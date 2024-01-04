using BinancePriceNotifier.Model.MarkPrice;
using static BinancePriceNotifier.Enums.ContractEnums;

namespace BinancePriceNotifier.Models.Options
{
    internal class BlockContractOptions
    {
        public List<BlockChainContract> ContractList { get; set; } = new List<BlockChainContract>();
    }

    public class BlockChainContract
    {
        public string TargetKey { get; set; } = null!;

        public TargetKeyType ContractTargetKey
        {
            get
            {
                switch (this.TargetKey)
                {
                    case "BTC": return TargetKeyType.BTC;
                    case "ETH": return TargetKeyType.ETH;
                    case "SOL": return TargetKeyType.SOL;
                    case "BNB": return TargetKeyType.BNB;
                    default: throw new Exception("TargetKey is not valid");
                }
            }
        }


        public decimal LastMarkPrice { get; set; }

        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public int GridCount { get; set; }

        public List<decimal> GridPriceList
        {
            get
            {
                var gridPriceList = new List<decimal>();
                decimal gridDifference = (EntryPrice - ExitPrice) / GridCount;

                for (int i = 1; i <= GridCount; i++)
                {
                    gridPriceList.Add(ExitPrice + gridDifference * i);
                }

                return gridPriceList;
            }
        }

        public decimal CurrentTriggeredPrice { get; set; }

        public HashSet<decimal> TriggeredGridPrices { get; set; } = new HashSet<decimal>();

        public void ResetTriggeredGridPrices()
        {
            TriggeredGridPrices.Clear();
        }
    
        public void SetLastMarkPrice(decimal currentPrice)
        {
            LastMarkPrice = currentPrice;
        }

    }
}
