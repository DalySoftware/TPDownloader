using YamlDotNet.RepresentationModel;

internal static class SessionInfoParser
{
    internal static List<int> ExtractDriverUserIds(string yaml)
    {
        var yamlStream = new YamlStream();
        using var reader = new StringReader(yaml);
        yamlStream.Load(reader);

        if (yamlStream.Documents[0].RootNode is not YamlMappingNode root)
            return [];

        if (!root.Children.TryGetValue("DriverInfo", out var driverInfoNode))
            return [];

        if (driverInfoNode is not YamlMappingNode driverInfo)
            return [];

        if (!driverInfo.Children.TryGetValue("Drivers", out var driversNode))
            return [];

        if (driversNode is not YamlSequenceNode drivers)
            return [];

        return drivers
            .OfType<YamlMappingNode>()
            .Select(driver =>
                driver.Children.TryGetValue("UserID", out var userIdNode)
                && int.TryParse(userIdNode.ToString(), out int userId)
                    ? userId
                    : (int?)null
            )
            .Select(id => id)
            .OfType<int>()
            .ToList();
    }
}
