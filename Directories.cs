internal static class Directories
{
    internal static readonly string IRacingPaints = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "iRacing",
        "paint"
    );

    internal static readonly string Temp = Path.Combine(Path.GetTempPath(), "TPDownloader");

    internal static string SessionFolder(this SessionId sessionId) =>
        Path.Combine(Temp, $"{sessionId.MainSessionId}_{sessionId.SubSessionId}");

    internal static string DownloadPath(this DownloadId paint, SessionId sessionId) =>
        Path.Combine(sessionId.SessionFolder(), paint.Directory);

    internal static string SavePath(this DownloadId paint) =>
        Path.Combine(IRacingPaints, paint.UserId.ToString(), paint.Directory);
}
