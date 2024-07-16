using System.Net.WebSockets;

namespace BlockTradeStrategy.Helpers
{
    internal class BinanceHelper
    {
        private ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly TelegramHelper _telegramHelper;
        private string _webSocketEndpoint = string.Empty;
        private readonly string _binanceWsEndpoint = $"wss://fstream.binance.com/stream?streams=";
        private System.Timers.Timer _reconnectTimer;
        private int _isReceivingMessages = 0;
        private const int _maxRetryCount = 10;

        internal BinanceHelper(TelegramHelper telegramHelper)
        {
            _telegramHelper = telegramHelper;
            _reconnectTimer = new System.Timers.Timer(3600000);
            _reconnectTimer.Elapsed += async (sender, e) => await AutoReconnectAsync();
            _reconnectTimer.AutoReset = true;
        }

        internal async Task ConnectAsync()
        {
            _reconnectTimer.Start();

            LoggerHelper.LogInfo("開始連接WebStocket連線");
            string btc = "btcusdt@aggTrade";
            string eth = "ethusdt@aggTrade";
            string sol = "solusdt@aggTrade";

            _webSocketEndpoint = $"{_binanceWsEndpoint}{btc}/{eth}/{sol}";
            try
            {
                await _webSocket.ConnectAsync(new Uri(_webSocketEndpoint), CancellationToken.None);
                if (_webSocket.State == WebSocketState.Open)
                {
                    LoggerHelper.LogInfo("WebStocket連線成功");
                    await _telegramHelper.SendTelegramMsgAsync("WebStocket連線成功");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogInfo($"Error while connecting to websocket , Reason : {ex.Message}");
                await _telegramHelper.SendErrorMessage($"Error while connecting to websocket , Reason : {ex.Message}");
                await ReconnectAsync();
            }
        }

        internal async Task ReceiveMessagesAsync()
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
                        var responseDate = JsonConvert.DeserializeObject<ConvertMarkPriceWebSocketModel>(response);

                        if (responseDate == null) continue;

                        MarkPriceModel.UpdateMarkPrice(responseDate);
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        LoggerHelper.LogInfo("WebSocket closed by server. Reconnecting...");
                        await ReconnectAsync();
                    }

                    Array.Clear(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogInfo($"Error while receiving message , Reason : {ex.Message}");
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
                    LoggerHelper.LogInfo("每小時自動關閉WebStocket連線");
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
                    LoggerHelper.LogError(msg);
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
        internal async Task ReconnectAsync()
        {
            LoggerHelper.LogInfo("開始進行重新連接WebStocket連線流程");
            int retryCount = 0;

            while (retryCount < _maxRetryCount)
            {
                LoggerHelper.LogInfo($"開始第一次嘗試重建連線 : {retryCount + 1}");
                try
                {
                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();
                    LoggerHelper.LogInfo("釋放Stocket連線資源，重新建立執行個體");
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
                    LoggerHelper.LogError($"Reconnect attempt {retryCount} failed: {ex.Message}. Retrying in {delay}ms.");
                    await _telegramHelper.SendErrorMessage($"Reconnect attempt {retryCount} failed: {ex.Message}. Retrying in {delay}ms.");
                    await Task.Delay(delay);
                }
            }

            if (retryCount == _maxRetryCount)
            {
                LoggerHelper.LogError("Max reconnect attempts reached. Connection failed.");
                await _telegramHelper.SendErrorMessage("Max reconnect attempts reached. Connection failed.");
            }
        }
    }
}
