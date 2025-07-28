// See https://aka.ms/new-console-template for more information
using irsdkSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPaints;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(
        (_, services) =>
        {
            services.AddHttpClient<TradingPaintsFetcherFactory>();
            services.AddHttpClient<SessionDownloader>();
            services
                .AddTransient<IRacingSDK>()
                .AddTransient<PaintSaver>()
                .AddTransient<UserInterface>();
        }
    )
    .Build();

var ui = host.Services.GetRequiredService<UserInterface>();
await ui.MainLoop();
