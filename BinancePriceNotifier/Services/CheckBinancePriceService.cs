using BinancePriceNotifier.Helpers;
using BinancePriceNotifier.Model.MarkPrice;
using BinancePriceNotifier.Models.Options;
using Microsoft.Extensions.Options;
using static BinancePriceNotifier.Enums.ContractEnums;

namespace BinancePriceNotifier.Services
{
    internal class CheckBinancePriceService
    {
        private readonly BinanceHelper _binanceHelper;
        private readonly TelegramHelper _telegramHelper;
        private List<BlockChainContract> _contractList;

        public CheckBinancePriceService( BinanceHelper binanceHelper, TelegramHelper telegramHelper , IOptions<BlockContractOptions> options)
        {
            _binanceHelper = binanceHelper;
            _telegramHelper = telegramHelper;
            _contractList = options.Value.ContractList;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _binanceHelper.ConnectAsync();

                // Binance WebSocket 無限循環監聽 合約價格(BTC, ETH, SOL)，並更新至 MarkPriceModel(static)
                _ = Task.Run(() => _binanceHelper.ReceiveMessagesAsync(), cancellationToken);
            }
            catch (Exception ex)
            {
                Program.Logger.Error($"BinanceWebSocketService StartAsync Error : {ex.Message}");
            }

            await StartTrade();
        }

        private async Task StartTrade()
        {
            while (true)
            {
                try
                {
                    await CheckPrice();
                }
                catch (Exception ex)
                {
                    Program.Logger.Error($"GridRobotService Error : {ex.Message}");
                }
                finally
                {
                    await Task.Delay(1000);
                }
            }
        }

        private async Task CheckPrice()
        {
            foreach (var item in _contractList)
            {
                var currentPrice = GetCurrentPrice(item);
                try
                {
                    if (currentPrice == 0) continue;

                    var triggeredGridPrice = GetTriggeredGridPrice(item,currentPrice);

                    if (triggeredGridPrice.HasValue)
                    {
                        string isRaise = string.Empty;
                        if (item.LastMarkPrice > currentPrice)
                        {
                            isRaise = "下跌";
                        }
                        else
                        {
                            isRaise = "上漲";
                        }
                        string msg = $"幣別 : {item.TargetKey},\n價格變化 :{isRaise} ,\n目前價格 : {currentPrice}";
                        Console.WriteLine($"現在時間 {DateTime.Now}\n{msg}");
                        await _telegramHelper.SendOrderJsonData($"{msg}");
                        Program.Logger.Info($"\n{msg}");
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Error($"Error : {ex.Message}");
                }
                finally
                {
                    item.SetLastMarkPrice(currentPrice);
                }

            }
        }

        private decimal GetCurrentPrice(BlockChainContract model)
        {
            return model.ContractTargetKey switch
            {
                TargetKeyType.BTC => MarkPriceModel.BtcPrice,
                TargetKeyType.ETH => MarkPriceModel.EthPrice,
                TargetKeyType.SOL => MarkPriceModel.SolPrice,
                TargetKeyType.BNB => MarkPriceModel.BnbPrice,
                _ => 0
            }; ;
        }

        private static decimal? GetTriggeredGridPrice(BlockChainContract model ,decimal currentPrice)
        {
            if (model.LastMarkPrice == 0)
            {
                return null;
            }

            // 價格區間
            decimal lowerPrice = Math.Min(model.LastMarkPrice, currentPrice);
            decimal higherPrice = Math.Max(model.LastMarkPrice, currentPrice);

            // 區間內符合之價格
            var possibleTriggeredGridPrices = model.GridPriceList
                                         .Where(p => p >= lowerPrice && p <= higherPrice)
                                         .ToList();

            if (!possibleTriggeredGridPrices.Any())
            {
                return null;
            }

            var triggeredGridPrice = possibleTriggeredGridPrices.OrderBy(p => Math.Abs(currentPrice - p)).First();

            // 檢查是否觸發價格
            if (!model.TriggeredGridPrices.Contains(triggeredGridPrice))
            {
                // 重置觸發價錢
                model.ResetTriggeredGridPrices();
                // 標記新的觸發價格
                model.TriggeredGridPrices.Add(triggeredGridPrice);
            }
            else
            {
                return null;
            }

            model.CurrentTriggeredPrice = triggeredGridPrice;

            return triggeredGridPrice;
        }
    }
}
