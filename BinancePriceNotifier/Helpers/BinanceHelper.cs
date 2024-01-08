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

        public BinanceHelper(TelegramHelper telegramHelper)
        {
            _telegramHelper = telegramHelper;
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
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        // 這裡應對 ping 幀進行處理
                        await HandlePingFrameAsync();
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

        /// <summary>
        /// Binance改為每10分鐘發送一次ping frame，如果10分鐘內沒有收到pong frame，則斷開連接
        /// </summary>
        /// <returns></returns>
        private async Task HandlePingFrameAsync()
        {
            // 回應 pong 幀
            await _webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Pong")), WebSocketMessageType.Binary, true, CancellationToken.None);
            Program.Logger.Info("Sent pong frame in response to ping frame");
        }


    }
}
