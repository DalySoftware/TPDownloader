using irsdkSharp;
using Microsoft.Extensions.Logging;
using TradingPaints;

internal class UserInterface(
    ILogger<UserInterface> logger,
    IRacingSDK sdk,
    SessionDownloader downloader
)
{
    internal async Task MainLoop()
    {
        sdk.OnConnected -= OnConnected;
        sdk.OnConnected += OnConnected;
        sdk.OnDisconnected -= OnDisconnected;
        sdk.OnConnected += OnDisconnected;

        logger.LogInformation("Waiting for iRacing SDK connection");
        while (true)
        {
            if (!sdk.IsConnected())
            {
                logger.LogDebug("Waiting to connect...");
                await Task.Delay(TimeSpan.FromSeconds(2));
                continue;
            }
        }
    }

    private async void OnConnected()
    {
        logger.LogInformation("Connected to iRacing SDK");
        var session = sdk.GetSessionInfo();
        var sessionToDownload = SessionInfoParser.GetRequiredDownloads(session);
        if (sessionToDownload == null)
        {
            logger.LogInformation("No downloads needed for session {SessionId}", session);
            return;
        }

        logger.LogInformation(
            "Session {SessionId} requires downloads for {PaintIds}",
            sessionToDownload.SessionId,
            string.Join(", ", sessionToDownload.PaintIds)
        );

        var downloaded = (await downloader.DownloadSession(sessionToDownload)).ToHashSet();

        logger.LogInformation(
            "Moving {Count} files for session {SessionId}.",
            downloaded.Count,
            sessionToDownload.SessionId
        );
        await PaintSaver.SaveSessionPaints(sessionToDownload.SessionId, downloaded);
        logger.LogInformation(
            "Finished moving files for session {SessionId}.",
            sessionToDownload.SessionId
        );
    }

    private void OnDisconnected()
    {
        logger.LogInformation("Waiting for iRacing SDK connection");
    }
}
