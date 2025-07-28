using irsdkSharp;
using Microsoft.Extensions.Logging;
using TradingPaints;

internal class UserInterface(
    ILogger<UserInterface> logger,
    IRacingSDK sdk,
    SessionDownloader downloader,
    PaintSaver saver
)
{
    private SessionId _lastSession = new(0, null);

    internal async Task MainLoop()
    {
        logger.LogInformation("Waiting to connect with an IRacing session...");
        while (true)
        {
            if (!sdk.IsConnected())
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Waiting for connection...");
                await Task.Delay(TimeSpan.FromSeconds(2));
                continue;
            }

            // The OnConnected event doesnt work for some reason so we do it manually
            var sessionYaml = sdk.GetSessionInfo();
            var session = SessionInfoParser.GetRequiredDownloads(sessionYaml);
            if (session is { } && session.SessionId != _lastSession)
            {
                await DownAndSavePaints(session);
            }
        }
    }

    private async Task DownAndSavePaints(SessionDownload session)
    {
        if (session == null)
        {
            logger.LogInformation("No downloads needed for {SessionId}", session);
            return;
        }

        logger.LogInformation(
            "{SessionId} requires {} downloads: {PaintIds}",
            session.SessionId,
            session.PaintIds.Count,
            string.Join(", ", session.PaintIds)
        );

        var downloaded = (await downloader.DownloadSession(session)).ToHashSet();

        logger.LogInformation(
            "Moving {Count} files for {SessionId}.",
            downloaded.Count,
            session.SessionId
        );
        await saver.SaveSessionPaints(session.SessionId, downloaded);
        logger.LogInformation("Processing complete for {SessionId}.", session.SessionId);

        _lastSession = session.SessionId;
    }
}
