using irsdkSharp;
using Microsoft.Extensions.Logging;
using TPDownloader.IRacing;
using TPDownloader.TradingPaints;

namespace TPDownloader;

internal class PaintService(
    ILogger<PaintService> logger,
    IRacingSDK sdk,
    SessionDownloader downloader,
    PaintManager paintManager,
    SessionInfoParser sessionParser
) : IDisposable
{
    private (Session.SessionId Id, HashSet<SavedFile> Files) _lastSession = NullSession;
    private readonly static (Session.SessionId Id, HashSet<SavedFile> Files) NullSession = new(new Session.SessionId(0, null), []);

    private bool _disposed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return MainLoop(cancellationToken);
    }

    private bool _wasConnected;
    private async Task MainLoop(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Waiting to connect with an IRacing session...");
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!sdk.IsConnected())
                {
                    if (_wasConnected)
                    {
                        logger.LogInformation("Session exited");
                        Cleanup();
                    }

                    _wasConnected = false;
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug("Waiting for connection...");
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    continue;
                }

                _wasConnected = true;

                // The OnConnected event doesnt work for some reason so we do it manually
                var sessionYaml = sdk.GetSessionInfo();
                var session = sessionParser.GetSessionInfo(sessionYaml);
                if (session is { } && session.Id != _lastSession.Id)
                {
                    await DownloadAndSavePaints(session);

                    logger.LogInformation("Requesting IRacing texture reload");
                    sdk.BroadcastMessage(
                        irsdkSharp.Enums.BroadcastMessageTypes.ReloadTextures,
                        0,
                        0
                    );
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Application shutdown requested");
        }
    }

    private async Task DownloadAndSavePaints(Session session)
    {
        logger.LogInformation("Deleting paints for old session {SessionId}", _lastSession.Id);
        paintManager.DeleteLastSessionPaints(_lastSession.Files);

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

    private void Cleanup()
    {
        if (_lastSession.Files.Count > 0)
        {
            logger.LogInformation("Cleaning up last session {SessionId}", _lastSession.Id);
            paintManager.DeleteLastSessionPaints(_lastSession.Files);
        }

        _lastSession = NullSession;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Cleanup();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
