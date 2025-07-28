using Microsoft.Extensions.Logging;

namespace TradingPaints;

internal class SessionDownloader(
    ILogger<SessionDownloader> logger,
    HttpClient httpClient,
    TradingPaintsFetcherFactory fetcherFactory
)
{
    internal async Task<IEnumerable<DownloadedFile>> DownloadSession(
        SessionDownload sessionDownload
    )
    {
        if (sessionDownload.PaintIds.Count == 0)
        {
            logger.LogInformation(
                "No paints to download for session {SessionId}.",
                sessionDownload.SessionId
            );
            return [];
        }

        logger.LogInformation(
            "Starting download for session {SessionId} with {PaintCount} paints.",
            sessionDownload.SessionId,
            sessionDownload.PaintIds.Count
        );

        return await DownloadPaints(sessionDownload);
    }

    private async Task<IEnumerable<DownloadedFile>> DownloadPaints(SessionDownload sessionDownload)
    {
        if (sessionDownload.PaintIds.Count == 0)
        {
            logger.LogInformation(
                "No paints to download for session {SessionId}.",
                sessionDownload.SessionId
            );
            return [];
        }

        if (Directory.Exists(sessionDownload.SessionId.SessionFolder()))
        {
            logger.LogInformation(
                "Session folder {SessionFolder} already exists. Removing existing files.",
                sessionDownload.SessionId.SessionFolder()
            );
            Directory.Delete(sessionDownload.SessionId.SessionFolder(), true);
        }

        var downloadedFiles = new List<DownloadedFile>();

        // Group paints by user to avoid multiple API calls for the same user
        var paintsByUser = sessionDownload.PaintIds.GroupBy(p => p.UserId);

        foreach (var userPaints in paintsByUser)
        {
            var userId = userPaints.Key;
            var userFiles = (await fetcherFactory.Create(userId).FetchPaintFilesAsync()).ToList();

            foreach (var paint in userPaints)
            {
                var downloadPath = paint.DownloadPath(sessionDownload.SessionId);
                Directory.CreateDirectory(downloadPath);

                // Filter files for this specific paint
                var paintUrls = userFiles
                    .Where(f =>
                        f.Id.Type is PaintType.Helmet or PaintType.Suit
                        || string.Equals(
                            f.Id.Directory,
                            paint.Directory,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Select(f => new Uri(f.Url));

                var fileTasks = new List<Task<DownloadedFile?>>();

                foreach (var url in paintUrls)
                {
                    var task = DownloadPaintToTempDirectory(sessionDownload.SessionId, paint, url);
                    fileTasks.Add(task);
                }
                var files = await Task.WhenAll(fileTasks);
                downloadedFiles.AddRange(files.OfType<DownloadedFile>());
            }
        }

        return downloadedFiles;
    }

    private async Task<DownloadedFile?> DownloadPaintToTempDirectory(
        SessionId sessionId,
        DownloadId downloadId,
        Uri url
    )
    {
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
