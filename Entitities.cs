internal record SessionId(int MainSessionId, int? SubSessionId);

internal record CarPaintId(int UserId, string CarPath);

internal record SessionDownload(SessionId SessionId, HashSet<CarPaintId> Paints);
