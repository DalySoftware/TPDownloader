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
        paint.Type switch
        {
            PaintType.Car => paint.CarFile($"car_{paint.UserId}.tga"),
            PaintType.CarDecal => paint.CarFile($"decal_{paint.UserId}.tga"),
            PaintType.CarNumber => paint.CarFile($"car_num_{paint.UserId}.tga"),
            PaintType.CarSpecMap => paint.CarFile($"car_spec_{paint.UserId}.mip"),
            PaintType.Helmet => Path.Combine(IRacingPaints, $"helmet_{paint.UserId}.tga"),
            PaintType.Suit => Path.Combine(IRacingPaints, $"suit_{paint.UserId}.tga"),
            _ => throw new ArgumentException($"Unknown paint type: {paint.Type}"),
        };

    private static string CarFile(this DownloadId paint, string filename) =>
        Path.Combine(IRacingPaints, paint.Directory, filename);
}
