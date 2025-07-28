internal class PaintSaver
{
    public static async Task SaveSessionPaints(
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

    private static async Task MovePaintToIRacing(DownloadedFile download)
    {
        if (Path.GetExtension(download.FilePath).Equals(".bz2", StringComparison.OrdinalIgnoreCase))
        {
            await FileExtractor.ExtractBz2FileAsync(
                download.FilePath,
                download.DownloadId.SavePath()
            );
            File.Delete(download.FilePath);
            return;
        }

        File.Move(download.FilePath, download.DownloadId.SavePath(), true);
    }
}
