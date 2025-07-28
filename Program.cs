using irsdkSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TPDownloader.IRacing;
using TPDownloader.TradingPaints;
using TPDownloader.UI;

namespace TPDownloader;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var mainForm = new MainForm();
            var defaultConfig = new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:Microsoft.*"] = "Warning",
                ["Logging:LogLevel:System.*"] = "Warning",
            };
            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(
                    (context, config) =>
                    {
                        config.AddInMemoryCollection(defaultConfig);
                        config.AddJsonFile("appsettings.json", optional: true);
                        config.AddEnvironmentVariables();
                    }
                )
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(new UILoggerProvider(mainForm));
                })
                .ConfigureServices(services =>
                {
                    services
                        .AddSingleton(mainForm)
                        .AddSingleton<TrayApplication>()
                        .AddSingleton<PaintService>()
                        .AddTransient<PaintManager>()
                        .AddTransient<SessionInfoParser>()
                        .AddTransient<IRacingSDK>();

                    services.AddHttpClient<TradingPaintsFetcherFactory>();
                    services.AddHttpClient<SessionDownloader>();
                })
                .Build();

            var trayApp = host.Services.GetRequiredService<TrayApplication>();
            trayApp.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error: {ex}");
        }
    }
}
