// See https://aka.ms/new-console-template for more information
using Downloading;
using irsdkSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(
        (_, services) =>
        {
            services.AddHttpClient<TradingPaintsFetcherFactory>();
            services.AddHttpClient<SessionDownloader>();
            services.AddSingleton<IRacingSDK>();
        }
    )
    .Build();

var sdk = host.Services.GetRequiredService<IRacingSDK>();
var downloader = host.Services.GetRequiredService<SessionDownloader>();
await Loop(sdk, downloader);

static async Task Loop(IRacingSDK sdk, SessionDownloader downloader)
{
    while (true)
    {
        if (!sdk.IsConnected())
        {
            Console.WriteLine("...waiting to connect");
            await Task.Delay(TimeSpan.FromSeconds(2));
            continue;
        }

        Console.WriteLine("Connected!");
        var session = sdk.GetSessionInfo();
        var downloads = SessionInfoParser.GetRequiredDownloads(session);
        if (downloads == null)
        {
            Console.WriteLine("Nothing to download.");
            return;
        }

        Console.WriteLine(
            $"Session: {downloads.SessionId} needs downloads: {string.Join(',', downloads.PaintIds)}"
        );
        await downloader.DownloadAndSavePaints(downloads);
        return;
    }
}
