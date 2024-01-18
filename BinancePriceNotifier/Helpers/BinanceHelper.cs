using BinancePriceNotifier.Helpers;
using BinancePriceNotifier.Model.MarkPrice;
using BinancePriceNotifier.Models.ViewModels;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace BlockTradeStrategy.Helpers
{
    public class BinanceHelper
    {
        private ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly TelegramHelper _telegramHelper;
        private string _webSocketEndpoint = string.Empty;
        private readonly string _binanceWsEndpoint = $"wss://fstream.binance.com/stream?streams=";
        private System.Timers.Timer _reconnectTimer;
        private int _isReceivingMessages = 0;
        private const int _maxRetryCount = 10;

        public BinanceHelper(TelegramHelper telegramHelper)
        {
            _telegramHelper = telegramHelper;
            _reconnectTimer = new System.Timers.Timer(3600000);
            _reconnectTimer.Elapsed += async (sender, e) => await AutoReconnectAsync();
            _reconnectTimer.AutoReset = true;
        }

        public async Task ConnectAsync()
        {
            _reconnectTimer.Start();

            Program.Logger.Info("開始連接WebStocket連線");
            string btc = "btcusdt@aggTrade";
            string eth = "ethusdt@aggTrade";
            string sol = "solusdt@aggTrade";

            _webSocketEndpoint = $"{_binanceWsEndpoint}{btc}/{eth}/{sol}";
            try
            {
                await _webSocket.ConnectAsync(new Uri(_webSocketEndpoint), CancellationToken.None);
                if (_webSocket.State == WebSocketState.Open)
                {
                    Program.Logger.Info("WebStocket連線成功");
                    await _telegramHelper.SendTelegramMsgAsync("WebStocket連線成功");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Error($"Error while connecting to websocket , Reason : {ex.Message}");
                await _telegramHelper.SendErrorMessage($"Error while connecting to websocket , Reason : {ex.Message}");
                await ReconnectAsync();
            }
        }

        public async Task ReceiveMessagesAsync()
        {
            if (Interlocked.CompareExchange(ref _isReceivingMessages, 1, 0) != 0) return;

            var buffer = new byte[1024 * 4];

            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var responseDate = JsonConvert.DeserializeObject<ConvertMarkPriceWebSocketResponse>(response);

                        if (responseDate == null) continue;

                        MarkPriceModel.UpdateMarkPrice(responseDate);
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Program.Logger.Info("WebSocket closed by server. Reconnecting...");
                        await ReconnectAsync();
                    }

                    Array.Clear(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Error($"Error while receiving message , Reason : {ex.Message}");
                await _telegramHelper.SendErrorMessage($"Error while receiving message , Reason : {ex.Message}");

                if (_webSocket.State == WebSocketState.Aborted)
                {
                    await ReconnectAsync();
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isReceivingMessages, 0);
            }
        }

        private async Task AutoReconnectAsync()
        {
            int autoReconnectCount = 0;

            while (autoReconnectCount < _maxRetryCount)
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
                    Program.Logger.Info("每小時自動關閉WebStocket連線");
                    await _telegramHelper.SendTelegramMsgAsync("每小時自動關閉WebStocket連線");
                }

                if (_webSocket.State == WebSocketState.Closed)
                {
                    await ReconnectAsync();
                    if (_webSocket.State == WebSocketState.Open) break;
                }
                else
                {
                    autoReconnectCount++;
                    var msg = $"第{autoReconnectCount} 次 WebSocket 關閉失敗，無法重新連線";
                    Program.Logger.Error(msg);
                    await _telegramHelper.SendErrorMessage(msg);
                    int delay = (int)Math.Pow(2, autoReconnectCount) * 1000;
                    await Task.Delay(delay);
                }
            }
        }

        /// <summary>
        /// 重新啟動 WebSocket 連接
        /// </summary>
        /// <returns></returns>
        public async Task ReconnectAsync()
        {
            Program.Logger.Info("開始進行重新連接WebStocket連線流程");
            int retryCount = 0;

            while (retryCount < _maxRetryCount)
            {
                Program.Logger.Info($"開始第一次嘗試重建連線 : {retryCount + 1}");
                try
                {
                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();
                    Program.Logger.Info("釋放Stocket連線資源，重新建立執行個體");
                    await ConnectAsync();

                    if (_webSocket.State == WebSocketState.Open)
                    {
                        _ = Task.Run(() => ReceiveMessagesAsync());
                        break;
                    }
                    else
                    {
                        await _telegramHelper.SendErrorMessage("Stocket連線建立失敗");
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    int delay = (int)Math.Pow(2, retryCount) * 1000;
                    Program.Logger.Error($"Reconnect attempt {retryCount} failed: {ex.Message}. Retrying in {delay}ms.");
                    await _telegramHelper.SendErrorMessage($"Reconnect attempt {retryCount} failed: {ex.Message}. Retrying in {delay}ms.");
                    await Task.Delay(delay);
                }
            }

            if (retryCount == _maxRetryCount)
            {
                Program.Logger.Error("Max reconnect attempts reached. Connection failed.");
                await _telegramHelper.SendErrorMessage("Max reconnect attempts reached. Connection failed.");
            }
        }
    }
}
