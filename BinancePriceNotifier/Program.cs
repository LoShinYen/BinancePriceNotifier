using BinancePriceNotifier.Helpers;
using BinancePriceNotifier.Models.Options;
using BinancePriceNotifier.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;

public class Program
{
    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    static async Task Main(string[] args)
    {
        Logger.Info("Application is running");
        var configuration = BuildConfiguration();
        var services = ConfigureServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        await RunApplication(serviceProvider);
    }

    static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        return builder.Build();
    }

    static ServiceCollection ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        //// Options Pattern
        services.Configure<TelegramOptions>(configuration.GetSection("TelegramOptions"));
        services.Configure<BlockContractOptions>(configuration.GetSection("BlockContractOptions"));

        // Singleton services
        services.AddSingleton<TelegramHelper>();
        services.AddSingleton<BinanceHelper>();
        services.AddSingleton<CheckBinancePriceService>();

        return services;
    }
    static async Task RunApplication(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var gridRobotService = scope.ServiceProvider.GetRequiredService<CheckBinancePriceService>();

            await gridRobotService.StartAsync();

        }
    }
}

