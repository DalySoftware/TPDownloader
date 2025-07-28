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
            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(new UILoggerProvider(mainForm));
                })
                .ConfigureServices(services =>
                    services
                        .AddSingleton(mainForm)
                        .AddSingleton<TrayApplication>()
                        .AddTransient<PaintManager>()
                        .AddTransient<SessionDownloader>()
                        .AddTransient<SessionInfoParser>()
                        .AddTransient<TradingPaintsFetcher>()
                )
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
