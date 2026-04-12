namespace TPDownloader;

internal static class Directories
{
    internal static readonly string IRacingPaints = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "iRacing",
        "paint"
    );

    internal static readonly string Temp = Path.Combine(Path.GetTempPath(), "TPDownloader");

    internal static string SessionFolder(this Session.SessionId sessionId) =>
        Path.Combine(Temp, $"{sessionId.MainSessionId}_{sessionId.SubSessionId}");

    internal static string DownloadPath(this DownloadId paint, Session.SessionId sessionId) =>
        Path.Combine(sessionId.SessionFolder(), paint.Directory);

    internal static string SavePath(this DownloadId paint) =>
        paint.Type switch
        {
            PaintType.Car => paint.CarFile(GetName(paint, "car") + ".tga"),
            PaintType.CarDecal => paint.CarFile(GetName(paint, "decal") + ".tga"),
            PaintType.CarNumber => paint.CarFile(GetName(paint, "car_num") + ".tga"),
            PaintType.CarSpecMap => paint.CarFile(GetName(paint, "car_spec") + ".mip"),
            PaintType.Suit => Path.Combine(IRacingPaints, GetName(paint, "suit")+ ".tga"),
            PaintType.Helmet => Path.Combine(IRacingPaints, $"helmet_{paint.UserId}.tga"),
            _ => throw new ArgumentException($"Unknown paint type: {paint.Type}"),
        };
    
    private static string GetName(DownloadId paint, string prefix) => 
        paint.TeamId > 0
        ? $"{prefix}_team_{paint.TeamId}"
        : $"{prefix}_{paint.UserId}";

    private static string CarFile(this DownloadId paint, string filename) =>
        Path.Combine(IRacingPaints, paint.Directory, filename);
}
