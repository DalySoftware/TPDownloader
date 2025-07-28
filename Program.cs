// See https://aka.ms/new-console-template for more information
using irsdkSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradingPaints;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(
        (_, services) =>
        {
            services.AddHttpClient<TradingPaintsFetcherFactory>();
            services.AddHttpClient<SessionDownloader>();
            services
                .AddSingleton<IRacingSDK>()
                .AddTransient<PaintSaver>()
                .AddSingleton<UserInterface>();
        }
    )
    .Build();

var ui = host.Services.GetRequiredService<UserInterface>();
await ui.MainLoop();
