using System.Xml;
using Microsoft.Extensions.Logging;

namespace TradingPaints;

internal class TradingPaintsFetcherFactory(
    HttpClient httpClient,
    ILogger<TradingPaintsFetcher> logger
)
{
    internal TradingPaintsFetcher Create(int userId) => new(httpClient, logger, userId);
}

internal class TradingPaintsFetcher(
    HttpClient httpClient,
    ILogger<TradingPaintsFetcher> logger,
    int userId
)
{
    internal async Task<IEnumerable<DownloadFile>> FetchPaintFilesAsync()
    {
        var response = await httpClient.GetAsync(
            $"https://fetch.tradingpaints.gg/fetch_user.php?user={userId}"
        );
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Failed to fetch paint files for user {userId}. Status: {StatusCode}",
                userId,
                response.StatusCode
            );
            return [];
        }

        var content = await response.Content.ReadAsStringAsync();

        var doc = new XmlDocument();
        doc.LoadXml(content);

        var cars = doc.SelectNodes("//Car");
        if (cars == null)
        {
            return [];
        }

        return [.. cars.OfType<XmlNode>().Select(Parse).OfType<DownloadFile>()];
    }

    private DownloadFile? Parse(XmlNode car)
    {
        // TradingPaints calls these all cars even thought they can be helmets, suits, etc.
        var fileUrl = car.SelectSingleNode("file")?.InnerText;
        var directory = car.SelectSingleNode("directory")?.InnerText;
        var type = car.SelectSingleNode("type")?.InnerText;

        if (
            string.IsNullOrEmpty(fileUrl)
            || string.IsNullOrEmpty(directory)
            || string.IsNullOrEmpty(type)
        )
        {
            logger.LogWarning("Skipping car with missing data: {CarNode}", car.OuterXml);
            return null;
        }

        var downloadId = new DownloadId(userId, directory, ToPaintType(type));
        return new DownloadFile(downloadId, fileUrl);
    }

    private static PaintType ToPaintType(string type) =>
        type.ToLowerInvariant() switch
        {
            "car" => PaintType.Car,
            "car_decal" => PaintType.CarDecal,
            "car_spec" => PaintType.CarSpecMap,
            "car_num" => PaintType.CarNumber,
            "helmet" => PaintType.Helmet,
            "suit" => PaintType.Suit,
            _ => throw new ArgumentException($"Unknown paint type: {type}"),
        };
}
