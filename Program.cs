using irsdkSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using TPDownloader;
using TPDownloader.IRacing;
using TPDownloader.TradingPaints;

var host = Host.CreateDefaultBuilder()
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
                .AddTransient<UserInterface>()
                .AddTransient<SessionInfoParser>();
        }
    )
    .Build();

var ui = host.Services.GetRequiredService<UserInterface>();
await ui.MainLoop();
