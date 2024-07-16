using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace BinancePriceNotifier.Helpers
{
    public class TelegramHelper
    {
        private static TelegramBotClient? _telegramClient;
        private static List<long> _developer = new List<long>();

        public TelegramHelper(IOptions<TelegramOptions> options)
        {
            var token = options.Value.TelegramBotToken;
            if (string.IsNullOrEmpty(token))
            {
                LoggerHelper.LogError("Telegram bot token is not configured.");
            }
            else if (_telegramClient == null)
            {
                _telegramClient = new TelegramBotClient(token);
            }
            _developer = options.Value.SubscriptionIDList;
        }

        public async Task SendTelegramMsgAsync(string message)
        {
            await SendMessageToDevelopers($"{message}");
        }

        public async Task SendErrorMessage(string message)
        {
            await SendMessageToDevelopers($"錯誤訊息 : {message}");
        }

        private async Task SendMessageToDevelopers(string message)
        {
            try
            {
                if (_telegramClient != null)
                {
                    foreach (var id in _developer)
                    {
                        await _telegramClient.SendTextMessageAsync(id, message);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Error while sending message to telegram , {ex.Message}");
            }
        }
    }
}
