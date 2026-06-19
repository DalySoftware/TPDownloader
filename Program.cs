using irsdkSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
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
                    (_, config) =>
                    {
                        config.SetBasePath(AppContext.BaseDirectory);
                        config.AddInMemoryCollection(defaultConfig);
                        config.AddJsonFile("appsettings.json", optional: true);
                        config.AddEnvironmentVariables();
                    }
                )
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(new UILoggerProvider(mainForm));
                })
                .ConfigureServices((context, services) =>
                {
                    services
                        .AddSingleton(mainForm)
                        .AddSingleton<TrayApplication>()
                        .AddSingleton<PaintService>()
                        .AddSingleton<SshTunnelService>()
                        .AddTransient<PaintManager>()
                        .AddTransient<SessionInfoParser>()
                        .AddTransient<IRacingSDK>();

                    AddTradingPaintsHttpClient<TradingPaintsUserFetcherFactory>(services, context.Configuration);
                    AddTradingPaintsHttpClient<TradingPaintsTeamFetcher>(services, context.Configuration);
                    AddTradingPaintsHttpClient<SessionDownloader>(services, context.Configuration);
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

    private static void AddTradingPaintsHttpClient<TClient>(
        IServiceCollection services,
        IConfiguration configuration
    ) where TClient : class
    {
        var builder = services
            .AddHttpClient<TClient>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(45);
            });

        var proxyUrl = GetProxyUrl(configuration);
        if (string.IsNullOrWhiteSpace(proxyUrl))
            return;

        builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var proxy = new WebProxy(proxyUrl)
            {
                BypassProxyOnLocal = GetBool(configuration, "Proxy:BypassOnLocal", true),
            };

            var username = configuration["Proxy:Username"];
            var password = configuration["Proxy:Password"];
            if (!string.IsNullOrWhiteSpace(username))
            {
                proxy.Credentials = new NetworkCredential(username, password ?? string.Empty);
            }

            return new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true,
            };
        });
    }

    internal static string? GetProxyUrl(IConfiguration configuration)
    {
        if (GetBool(configuration, "Proxy:SshTunnel:Enabled", false))
        {
            var localPort = GetInt(configuration, "Proxy:SshTunnel:LocalSocksPort", 7890);
            return $"socks5://127.0.0.1:{localPort}";
        }

        var proxyUrl = configuration["Proxy:Url"];
        if (!string.IsNullOrWhiteSpace(proxyUrl))
            return proxyUrl;

        return Environment.GetEnvironmentVariable("TPDOWNLOADER_PROXY");
    }

    internal static bool GetBool(IConfiguration configuration, string key, bool defaultValue)
    {
        var value = configuration[key];
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    internal static int GetInt(IConfiguration configuration, string key, int defaultValue)
    {
        var value = configuration[key];
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
