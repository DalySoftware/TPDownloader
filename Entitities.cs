internal record SessionId(int MainSessionId, int? SubSessionId);

internal record DownloadId(int UserId, string Directory, PaintType Type);

internal enum PaintType
{
    Car,
    CarDecal,
    CarNumber,
    CarSpecMap,
    Helmet,
    Suit,
}

internal record SessionDownload(SessionId SessionId, HashSet<DownloadId> PaintIds);

internal record DownloadedFile(SessionId SessionId, DownloadId DownloadId, string FilePath);

internal record SavedFile(SessionId SessionId, DownloadId DownloadId, string FilePath);
