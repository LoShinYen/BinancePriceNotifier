using BinancePriceNotifier.Model.MarkPrice;
using BinancePriceNotifier.Models.ViewModels;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace BinancePriceNotifier.Helpers
{
    internal class BinanceHelper
    {
        private ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly TelegramHelper _telegramHelper;
        private System.Timers.Timer _reconnectTimer;
        private int _isReceivingMessages = 0;

        public BinanceHelper(TelegramHelper telegramHelper)
        {
            _telegramHelper = telegramHelper;
            _reconnectTimer = new System.Timers.Timer(3600000);
            _reconnectTimer.Elapsed += async (sender, e) => await AutoReconnectAsync();
            _reconnectTimer.AutoReset = true;
        }

        public async Task ConnectAsync()
        {
            string btc = "btcusdt@aggTrade";
            string eth = "ethusdt@aggTrade";
            string sol = "solusdt@aggTrade";
            string bnbusdt = "bnbusdt@aggTrade";
            string _binanceWsEndpoint = $"wss://fstream.binance.com/stream?streams={btc}/{eth}/{sol}/{bnbusdt}";

            try
            { 
                await _webSocket.ConnectAsync(new Uri(_binanceWsEndpoint), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Program.Logger.Error(ex, "Error while connecting to websocket");
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
                Program.Logger.Error(ex, "Error while receiving message");
                if (_webSocket.State == WebSocketState.Closed) { }
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
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
                Program.Logger.Info("每小時自動關閉WebStocket連線");
                await _telegramHelper.SendTelegramMsgAsync("每小時自動關閉WebStocket連線");
                await ReconnectAsync();
            }
        }

        /// <summary>
        /// 重新啟動 WebSocket 連接
        /// </summary>
        /// <returns></returns>
        public async Task ReconnectAsync()
        {
            int retryCount = 0;
            const int maxRetryCount = 5;

            while (retryCount < maxRetryCount)
            {
                try
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();
                    Program.Logger.Info("釋放Stocket連線資源，重新建立執行個體");
                    await ConnectAsync();

                    if (_webSocket.State == WebSocketState.Open)
                    {
                        Program.Logger.Info("Stocket連線建立成功");
                        _ = Task.Run(() => ReceiveMessagesAsync());
                        break;
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    int delay = (int)Math.Pow(2, retryCount) * 1000;
                    Program.Logger.Error($"Reconnect attempt {retryCount} failed: {ex.Message}. Retrying in {delay}ms.");
                    await Task.Delay(delay);
                }
            }

            if (retryCount == maxRetryCount)
            {
                Program.Logger.Error("Max reconnect attempts reached. Connection failed.");
                await _telegramHelper.SendErrorMessage("Max reconnect attempts reached. Connection failed.");
            }
        }
    }
}
