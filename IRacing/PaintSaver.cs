using Microsoft.Extensions.Logging;

internal class PaintSaver(ILogger<PaintSaver> logger)
{
    public async Task SaveSessionPaints(
        SessionId sessionId,
        IEnumerable<DownloadedFile> downloadedFiles
    )
    {
        foreach (var download in downloadedFiles)
        {
            await MovePaintToIRacing(download);
        }

        Directory.Delete(sessionId.SessionFolder(), true);
    }

    private async Task MovePaintToIRacing(DownloadedFile download)
    {
        if (Path.GetExtension(download.FilePath).Equals(".bz2", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug(
                "Extracting {FilePath} to {SavePath}",
                download.FilePath,
                download.DownloadId.SavePath()
            );
            await FileExtractor.ExtractBz2FileAsync(
                download.FilePath,
                download.DownloadId.SavePath()
            );
            File.Delete(download.FilePath);
            return;
        }

        logger.LogDebug(
            "Moving {FilePath} to {SavePath}",
            download.FilePath,
            download.DownloadId.SavePath()
        );
        File.Move(download.FilePath, download.DownloadId.SavePath(), true);
    }
}
