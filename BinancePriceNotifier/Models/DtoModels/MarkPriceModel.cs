using BinancePriceNotifier.Models.ViewModels;
using static BinancePriceNotifier.Enums.ContractEnums;

namespace BinancePriceNotifier.Model.MarkPrice
{
    public static class MarkPriceModel
    {
        private static readonly object _lock = new object();

        public static decimal BtcPrice { get; set; } 
        public static decimal EthPrice { get; set; }
        public static decimal SolPrice { get; set; }

        public static void UpdateMarkPrice(ConvertMarkPriceWebSocketResponse responseDate)
        {
            lock (_lock)
            {
                switch (responseDate.Data.s)
                {
                    case CryptoContractType.BTCUSDT:
                        BtcPrice = Decimal.Parse(responseDate.Data.p);
                        break;
                    case CryptoContractType.ETHUSDT:
                        EthPrice = Decimal.Parse(responseDate.Data.p);
                        break;
                    case CryptoContractType.SOLUSDT:
                        SolPrice = Decimal.Parse(responseDate.Data.p);
                        break;
                }
            }
        }
    }
}
