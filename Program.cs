using irsdkSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using TPDownloader;
using TPDownloader.IRacing;
using TPDownloader.TradingPaints;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "TPDownloader";
    })
    .ConfigureLogging(
        (context, logging) =>
        {
            logging.ClearProviders();
            logging
                .AddConsole(options =>
                {
                    options.FormatterName = "SimpleConsoleFormatter";
                })
                .AddConsoleFormatter<SimpleConsoleFormatter, ConsoleFormatterOptions>();

            if (OperatingSystem.IsWindows())
            {
                logging.AddEventLog();
            }
        }
    )
    .ConfigureServices(
        (_, services) =>
        {
            services.AddHttpClient<TradingPaintsFetcherFactory>();
            services.AddHttpClient<SessionDownloader>();
            services
                .AddTransient<IRacingSDK>()
                .AddTransient<PaintManager>()
                .AddTransient<SessionInfoParser>()
                .AddHostedService<UserInterface>();
        }
    );

await builder.Build().RunAsync();
