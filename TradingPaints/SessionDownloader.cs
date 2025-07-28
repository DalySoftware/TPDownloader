using Microsoft.Extensions.Logging;

namespace TradingPaints;

internal class SessionDownloader(
    ILogger<SessionDownloader> logger,
    HttpClient httpClient,
    TradingPaintsFetcherFactory fetcherFactory
)
{
    internal async Task<IEnumerable<DownloadedFile>> DownloadSession(Session session)
    {
        logger.LogInformation("Starting download for {SessionId}", session.Id);

        return await DownloadPaints(session);
    }

    private async Task<IEnumerable<DownloadedFile>> DownloadPaints(Session session)
    {
        if (Directory.Exists(session.Id.SessionFolder()))
        {
            logger.LogInformation(
                "Session folder {SessionFolder} already exists. Removing existing files.",
                session.Id.SessionFolder()
            );
            Directory.Delete(session.Id.SessionFolder(), true);
        }

        var downloadedFiles = new List<DownloadedFile>();

        foreach (var user in session.Users)
        {
            var userId = user.UserId;
            var allUserFiles = await fetcherFactory.Create(userId).FetchPaintFilesAsync();

            var userFiles = allUserFiles
                .Where(f =>
                    f.Id.Type is PaintType.Helmet or PaintType.Suit
                    || string.Equals(
                        f.Id.Directory,
                        user.Directory,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToList();

            logger.LogInformation(
                "Downloading {Count} files for User {UserId}",
                userFiles.Count,
                userId
            );

            var fileTasks = new List<Task<DownloadedFile?>>();
            foreach (var file in userFiles)
            {
                var task = DownloadPaintToTempDirectory(session.Id, file.Id, new Uri(file.Url));
                fileTasks.Add(task);
            }
            var files = await Task.WhenAll(fileTasks);
            downloadedFiles.AddRange(files.OfType<DownloadedFile>());
        }

        return downloadedFiles;
    }

    private async Task<DownloadedFile?> DownloadPaintToTempDirectory(
        Session.SessionId sessionId,
        DownloadId downloadId,
        Uri url
    )
    {
        var downloadPath = downloadId.DownloadPath(sessionId);
        Directory.CreateDirectory(downloadPath);

        var fileName = Path.GetFileName(url.LocalPath);
        var destinationPath = Path.Combine(downloadId.DownloadPath(sessionId), fileName);

        logger.LogDebug("Downloading {Url} to {DestinationPath}", url, destinationPath);
        var fileResponse = await httpClient.GetAsync(url);
        if (!fileResponse.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Failed to download {Url}. Status code: {StatusCode}",
                url,
                fileResponse.StatusCode
            );
            return null;
        }

        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
        await fileResponse.Content.CopyToAsync(fileStream);
        return new DownloadedFile(sessionId, downloadId, destinationPath);
    }
}
