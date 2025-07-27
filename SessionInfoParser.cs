using YamlDotNet.RepresentationModel;

internal static class SessionInfoParser
{
    internal static IEnumerable<CarPaintId> GetRequiredDownloads(string yaml)
    {
        var yamlStream = new YamlStream();
        using var reader = new StringReader(yaml);
        yamlStream.Load(reader);

        var drivers = GetDrivers(yamlStream);
        return drivers
            .Select(d => d)
            .OfType<YamlMappingNode>()
            .Select(ToCarPaintDownload)
            .OfType<CarPaintId>()
            .Where(download => download.UserId > 0);
    }

    private static IEnumerable<YamlNode> GetDrivers(YamlStream yamlStream)
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

    private static CarPaintId? ToCarPaintDownload(YamlMappingNode driver) =>
        driver.Children.TryGetValue("UserID", out var userIdNode)
        && int.TryParse(userIdNode.ToString(), out int userId)
        && driver.Children.TryGetValue("CarPath", out var carPathNode)
            ? new CarPaintId(userId, carPathNode.ToString())
            : null;
}
