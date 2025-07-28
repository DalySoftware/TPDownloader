using irsdkSharp;
using Microsoft.Extensions.Hosting;
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
) : IHostedService
{
    private (Session.SessionId Id, HashSet<SavedFile> Files) _lastSession = (
        new Session.SessionId(0, null),
        []
    );

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return MainLoop(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Cleanup();
        return Task.CompletedTask;
    }

    private async Task MainLoop(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Waiting to connect with an IRacing session...");
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!sdk.IsConnected())
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug("Waiting for connection...");
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    continue;
                }

                // The OnConnected event doesnt work for some reason so we do it manually
                var sessionYaml = sdk.GetSessionInfo();
                var session = sessionParser.GetSessionInfo(sessionYaml);
                if (session is { } && session.Id != _lastSession.Id)
                {
                    await DownloadAndSavePaints(session);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Application shutdown requested");
        }
    }

    private void Cleanup()
    {
        if (_lastSession.Files.Count > 0)
        {
            logger.LogInformation("Cleaning up last session {SessionId} on exit", _lastSession.Id);
            paintManager.DeleteLastSessionPaints(_lastSession.Files);
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
