namespace BinancePriceNotifier.Models.Options
{
    public class TelegramOptions
    {
        public string TelegramBotToken { get; set; } = string.Empty;
        public List<long> SubscriptionIDList { get; set; } = new List<long>();
    }
}
