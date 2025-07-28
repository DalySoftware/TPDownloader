using irsdkSharp;
using Microsoft.Extensions.Logging;
using TPDownloader.IRacing;
using TPDownloader.TradingPaints;

namespace TPDownloader;

internal class UserInterface(
    ILogger<UserInterface> logger,
    IRacingSDK sdk,
    SessionDownloader downloader,
    PaintManager paintManager,
    SessionInfoParser sessionParser
)
{
    private (Session.SessionId Id, HashSet<SavedFile> Files) _lastSession = (
        new Session.SessionId(0, null),
        []
    );

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
            var session = sessionParser.GetSessionInfo(sessionYaml);
            if (session is { } && session.Id != _lastSession.Id)
            {
                await DownloadAndSavePaints(session);
            }
        }
    }

    private async Task DownloadAndSavePaints(Session session)
    {
        logger.LogInformation("Deleting paints for old session {SessionId}", _lastSession.Id);
        paintManager.DeleteLastSessionPaints(_lastSession.Files);

        if (session == null)
        {
            logger.LogInformation("No downloads needed for {SessionId}", session);
            return;
        }

        var downloaded = (await downloader.DownloadSession(session)).ToHashSet();

        logger.LogInformation(
            "Moving {Count} files for {SessionId}.",
            downloaded.Count,
            session.Id
        );

        var savedFiles = await paintManager.SaveSessionPaints(session.Id, downloaded);
        _lastSession = (session.Id, savedFiles.ToHashSet());

        logger.LogInformation("Processing complete for {SessionId}", session.Id);
    }
}
