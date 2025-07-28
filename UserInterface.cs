using irsdkSharp;
using Microsoft.Extensions.Logging;
using TradingPaints;

internal class UserInterface(
    ILogger<UserInterface> logger,
    IRacingSDK sdk,
    SessionDownloader downloader
)
{
    private SessionId _lastSession = new(0, null);

    internal async Task MainLoop()
    {
        while (true)
        {
            if (!sdk.IsConnected())
            {
                logger.LogDebug("Waiting to connect with an IRacing session...");
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
            logger.LogInformation("No downloads needed for session {SessionId}", session);
            return;
        }

        logger.LogInformation(
            "Session {SessionId} requires downloads for {PaintIds}",
            session.SessionId,
            string.Join(", ", session.PaintIds)
        );

        var downloaded = (await downloader.DownloadSession(session)).ToHashSet();

        logger.LogInformation(
            "Moving {Count} files for session {SessionId}.",
            downloaded.Count,
            session.SessionId
        );
        await PaintSaver.SaveSessionPaints(session.SessionId, downloaded);
        logger.LogInformation("Processing complete for session {SessionId}.", session.SessionId);

        _lastSession = session.SessionId;
    }
}
