using YamlDotNet.RepresentationModel;

namespace TPDownloader.TradingPaints;

internal class SessionInfoParser
{
    internal Session? GetSessionInfo(string yaml)
    {
        var yamlStream = new YamlStream();
        using var reader = new StringReader(yaml);
        yamlStream.Load(reader);

        var sessionId = GetSessionId(yamlStream);
        if (sessionId == null)
            return null;

        var cars = GetDrivers(yamlStream)
            .OfType<YamlMappingNode>()
            .Select(ToCarInfo)
            .OfType<Session.User>()
            .Where(car => car.UserId > 0)
            .ToHashSet();

        return new Session(sessionId, cars);
    }

    private static Session.SessionId? GetSessionId(YamlStream yamlStream)
    {
        if (
            yamlStream.Documents.FirstOrDefault()?.RootNode is not YamlMappingNode root
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

        return new Session.SessionId(mainSessionId, subSessionId);
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

    private static Session.User? ToCarInfo(YamlMappingNode driver) =>
        driver.Children.TryGetValue("UserID", out var userIdNode)
        && int.TryParse(userIdNode.ToString(), out int userId)
        && driver.Children.TryGetValue("CarPath", out var carPathNode)
            ? new Session.User(userId, carPathNode.ToString())
            : null;
}
