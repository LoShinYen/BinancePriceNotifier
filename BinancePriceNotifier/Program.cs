using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    static async Task Main(string[] args)
    {
        LoggerHelper.LogInfo("Application is running");
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
            var checkBinancePriceService = scope.ServiceProvider.GetRequiredService<CheckBinancePriceService>();

            await checkBinancePriceService.StartAsync();

        }
    }
}

