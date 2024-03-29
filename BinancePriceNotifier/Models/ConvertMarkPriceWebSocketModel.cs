using static BinancePriceNotifier.Enums.ContractEnums;

namespace BinancePriceNotifier.Models
{
    public class ConvertMarkPriceWebSocketModel
    {
        public string Stream { get; set; } = string.Empty;
        public Data Data { get; set; } = new Data();
    }

    public class Data
    {
        /// <summary>
        /// WebStokcet 事件
        /// </summary>
        public string e { get; set; } = string.Empty;

        /// <summary>
        /// Unix 時間戳記 - 事件時間
        /// </summary>
        public string E { get; set; } = string.Empty;

        /// <summary>
        /// 合約幣別
        /// </summary>
        public CryptoContractType s { get; set; }

        /// <summary>
        /// 成交價格
        /// </summary>
        public string p { get; set; } = string.Empty;

        /// <summary>
        /// 成交時間
        /// </summary>
        public string T { get; set; } = string.Empty;


        /// <summary>
        /// 人眼可觀察事件時間
        /// </summary>
        public DateTime UnixConvertEventDateTime
        {
            get
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(E)).DateTime;
            }
        }

        /// <summary>
        /// 人眼可觀察成交時間
        /// </summary>
        public DateTime UnixConvertTradeDateTime
        {
            get
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(T)).DateTime;
            }
        }
    }
}
