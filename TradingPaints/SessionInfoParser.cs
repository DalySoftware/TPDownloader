using YamlDotNet.RepresentationModel;

namespace TPDownloader.TradingPaints;

internal class SessionInfoParser
{
    internal Session? GetSessionInfo(string yaml)
    {
        var yamlStream = new YamlStream();
        using var reader = new StringReader(yaml);
        yamlStream.Load(reader);
        
        var weekendInfo = GetWeekendInfo(yamlStream);
        if (weekendInfo == null)
            return null;

        var sessionId = GetSessionId(weekendInfo);
        if (sessionId == null)
            return null;

        var cars = GetDrivers(yamlStream)
            .OfType<YamlMappingNode>()
            .Select(ToCarInfo)
            .OfType<Session.User>()
            .Where(car => car.UserId > 0)
            .ToHashSet();

        var teamRacing = GetTeamRacing(weekendInfo);
        return new Session(sessionId, cars, teamRacing);
    }
    
    private static YamlMappingNode? GetWeekendInfo(YamlStream yamlStream)
    {
        var root = yamlStream.Documents.FirstOrDefault()?.RootNode as YamlMappingNode;
        if (root == null)
            return null;

        return root.Children.TryGetValue("WeekendInfo", out var node)
            ? node as YamlMappingNode
            : null;
    }

    private static bool GetTeamRacing(YamlMappingNode weekend)
    {
        return weekend.Children.TryGetValue("TeamRacing", out var node)
               && int.TryParse(node.ToString(), out var value)
               && value == 1;
    }

    private static Session.SessionId? GetSessionId(YamlMappingNode weekend)
    {
        if (
            !weekend.Children.TryGetValue("SessionID", out var sessionIdNode) ||
            !int.TryParse(sessionIdNode.ToString(), out var mainSessionId)
        )
            return null;

        int? subSessionId =
            weekend.Children.TryGetValue("SubSessionID", out var subSessionIdNode)
            && int.TryParse(subSessionIdNode.ToString(), out var parsedSubSessionId)
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
        && int.TryParse(userIdNode.ToString(), out var userId)
        && driver.Children.TryGetValue("CarPath", out var carPathNode)
            ? new Session.User(userId, GetTeamId(driver), carPathNode.ToString(), GetCarNumber(driver))
            : null;

    private static int GetTeamId(YamlMappingNode driver) =>
        driver.Children.TryGetValue("TeamID", out var teamIdNode)
        && int.TryParse(teamIdNode.ToString(), out var teamId)
            ? teamId : 0; // 0 is also used for safety car

    private static string GetCarNumber(YamlMappingNode driver) =>
        driver.Children.TryGetValue("CarNumber", out var node) ? node.ToString() : string.Empty;
}
