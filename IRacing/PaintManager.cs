namespace TPDownloader.IRacing;

internal class PaintManager(ILogger<PaintManager> logger)
{
    public async Task<IEnumerable<SavedFile>> SaveSessionPaints(
        Session.SessionId sessionId,
        IEnumerable<DownloadedFile> downloadedFiles
    )
    {
        var savedFiles = new List<SavedFile>();
        foreach (var download in downloadedFiles)
        {
            var file = await MovePaintToIRacing(download);
            savedFiles.Add(file);
        }

        Directory.Delete(sessionId.SessionFolder(), true);
        return savedFiles;
    }

    private async Task<SavedFile> MovePaintToIRacing(DownloadedFile download)
    {
        var downloadPath = download.FilePath;
        var savePath = download.DownloadId.SavePath();
        if (Path.GetExtension(downloadPath).Equals(".bz2", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Extracting {FilePath} to {SavePath}", downloadPath, savePath);
            await FileExtractor.ExtractBz2FileAsync(downloadPath, savePath);
            File.Delete(downloadPath);
            return new(download.SessionId, download.DownloadId, savePath);
        }

        logger.LogDebug("Moving {FilePath} to {SavePath}", downloadPath, savePath);
        File.Move(downloadPath, savePath, true);
        return new(download.SessionId, download.DownloadId, savePath);
    }

    internal void DeleteLastSessionPaints(IEnumerable<SavedFile> files)
    {
        foreach (var file in files)
        {
            logger.LogDebug("Deleting {FilePath}", file.FilePath);
            File.Delete(file.FilePath);
        }
    }
}
