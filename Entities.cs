namespace TPDownloader;

internal record Session(Session.SessionId Id, HashSet<Session.User> Users)
{
    internal record SessionId(int MainSessionId, int? SubSessionId);

    internal record User(int UserId, string Directory);
}

internal record DownloadId(int UserId, string Directory, PaintType Type);

internal record DownloadFile(DownloadId Id, string Url);

internal enum PaintType
{
    Car,
    CarDecal,
    CarNumber,
    CarSpecMap,
    Helmet,
    Suit,
}

internal record DownloadedFile(Session.SessionId SessionId, DownloadId DownloadId, string FilePath);

internal record SavedFile(Session.SessionId SessionId, DownloadId DownloadId, string FilePath);
