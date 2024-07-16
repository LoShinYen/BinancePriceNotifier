using NLog;

namespace BinancePriceNotifier.Helpers
{
    internal static class LoggerHelper
    {
        internal static void LogInfo(string msg, string serviceName = "")
        {
            var logger = LogManager.GetLogger(serviceName);
            Console.WriteLine(msg);
            logger.Info(msg);
        }

        internal static void LogError(string msg, string serviceName = "")
        {
            var logger = LogManager.GetLogger(serviceName);
            Console.WriteLine(msg);
            logger.Error(msg);
        }
    }
}
