using YamlDotNet.RepresentationModel;

internal static class SessionInfoParser
{
    internal static SessionDownload? GetRequiredDownloads(string yaml)
    {
        var yamlStream = new YamlStream();
        using var reader = new StringReader(yaml);
        yamlStream.Load(reader);

        var sessionId = GetSessionId(yamlStream);
        if (sessionId == null)
            return null;

        var paints = GetDrivers(yamlStream)
            .OfType<YamlMappingNode>()
            .Select(ToCarPaintDownload)
            .OfType<DownloadId>()
            .Where(download => download.UserId > 0)
            .ToHashSet();

        return new SessionDownload(sessionId, paints);
    }

    private static SessionId? GetSessionId(YamlStream yamlStream)
    {
        if (
            yamlStream.Documents[0].RootNode is not YamlMappingNode root
            || !root.Children.TryGetValue("WeekendInfo", out var weekendInfoNode)
            || weekendInfoNode is not YamlMappingNode weekendInfo
            || !weekendInfo.Children.TryGetValue("SessionID", out var sessionIdNode)
            || !int.TryParse(sessionIdNode.ToString(), out int mainSessionId)
        )
        {
            return null;
        }

        int? subSessionId =
            weekendInfo.Children.TryGetValue("SubSessionID", out var subSessionIdNode)
            && int.TryParse(subSessionIdNode.ToString(), out int parsedSubSessionId)
                ? parsedSubSessionId
                : null;

        return new SessionId(mainSessionId, subSessionId);
    }

    private static YamlSequenceNode GetDrivers(YamlStream yamlStream)
    {
        if (
            yamlStream.Documents[0].RootNode is not YamlMappingNode root
            || !root.Children.TryGetValue("DriverInfo", out var driverInfoNode)
            || driverInfoNode is not YamlMappingNode driverInfo
            || !driverInfo.Children.TryGetValue("Drivers", out var driversNode)
            || driversNode is not YamlSequenceNode drivers
        )
        {
            return [];
        }
        return drivers;
    }

    private static DownloadId? ToCarPaintDownload(YamlMappingNode driver) =>
        driver.Children.TryGetValue("UserID", out var userIdNode)
        && int.TryParse(userIdNode.ToString(), out int userId)
        && driver.Children.TryGetValue("CarPath", out var carPathNode)
            ? new DownloadId(userId, carPathNode.ToString(), PaintType.Car)
            : null;
}
