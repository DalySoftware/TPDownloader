using Microsoft.Extensions.Logging;

namespace Downloading;

internal class SessionDownloader(
    ILogger<SessionDownloader> logger,
    HttpClient httpClient,
    TradingPaintsFetcherFactory fetcherFactory
)
{
    public async Task DownloadSession(SessionDownload sessionDownload)
    {
        if (sessionDownload.PaintIds.Count == 0)
        {
            logger.LogInformation(
                "No paints to download for session {SessionId}.",
                sessionDownload.SessionId
            );
            return;
        }

        logger.LogInformation(
            "Starting download for session {SessionId} with {PaintCount} paints.",
            sessionDownload.SessionId,
            sessionDownload.PaintIds.Count
        );

        await DownloadAndSavePaints(sessionDownload);
    }

    internal async Task DownloadAndSavePaints(SessionDownload sessionDownload)
    {
        if (sessionDownload.PaintIds.Count == 0)
        {
            logger.LogInformation(
                "No paints to download for session {SessionId}.",
                sessionDownload.SessionId
            );
            return;
        }

        if (Directory.Exists(sessionDownload.SessionId.SessionFolder()))
        {
            logger.LogInformation(
                "Session folder {SessionFolder} already exists. Removing existing files.",
                sessionDownload.SessionId.SessionFolder()
            );
            Directory.Delete(sessionDownload.SessionId.SessionFolder(), true);
        }

        foreach (var paint in sessionDownload.PaintIds)
        {
            // create paret directory of paint.DownloadPath() if it doesn't exist
            var downloadPath = paint.DownloadPath(sessionDownload.SessionId);
            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }

            // Simulate downloading the paint
            logger.LogInformation(
                "Downloading paint for user {UserId} to {DownloadPath}",
                paint.UserId,
                downloadPath
            );
            await DownloadPaintToTempDirectory(sessionDownload.SessionId, paint);
        }
    }

    private async Task DownloadPaintToTempDirectory(SessionId sessionId, DownloadId downloadId)
    {
        var urls = await GetDownloadUrls(downloadId);
        foreach (var url in urls)
        {
            var fileName = Path.GetFileName(url.LocalPath);
            var destinationPath = Path.Combine(downloadId.DownloadPath(sessionId), fileName);

            logger.LogInformation("Downloading {Url} to {DestinationPath}", url, destinationPath);
            var fileResponse = await httpClient.GetAsync(url);
            if (!fileResponse.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Failed to download {Url}. Status code: {StatusCode}",
                    url,
                    fileResponse.StatusCode
                );
                continue;
            }

            using var fileStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write
            );
            await fileResponse.Content.CopyToAsync(fileStream);
        }
    }

    private async Task<IEnumerable<Uri>> GetDownloadUrls(DownloadId paint)
    {
        var files = await fetcherFactory.Create(paint.UserId).FetchPaintFilesAsync();
        // Filter files by CarPath
        var urls = files
            .Where(f =>
                string.Equals(f.Id.Directory, paint.Directory, StringComparison.OrdinalIgnoreCase)
            )
            .Select(f => new Uri(f.Url));
        return urls;
    }
}
