using System.Text;
using System.Xml;

namespace TPDownloader.TradingPaints;

internal class TradingPaintsTeamFetcher(HttpClient httpClient)
{
    internal async Task<List<DownloadFile>> FetchSession(Session session)
    {
        var listParam = string.Join(
            ",",
            session.Users.Select(u =>
                $"{u.UserId}={u.Directory}={u.TeamId}={u.CarNumber}"
            )
        );

        var body =
            $"list={listParam}" +
            $"&series={session.SeriesId}" +
            $"&league={session.LeagueId}" +
            $"&team=1" +
            $"&user={session.DriverUserId}";

        var response = await httpClient.PostAsync(
            "https://fetch.tradingpaints.gg/fetch.php",
            new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
        );

        if (!response.IsSuccessStatusCode)
            return [];

        var xml = await response.Content.ReadAsStringAsync();

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var nodes = doc.SelectNodes("//Car");
        if (nodes == null)
            return [];

        var result = new List<DownloadFile>();

        foreach (XmlNode car in nodes)
        {
            var fileUrl = car.SelectSingleNode("file")?.InnerText;
            var directory = car.SelectSingleNode("directory")?.InnerText;
            var type = car.SelectSingleNode("type")?.InnerText;
            if (string.IsNullOrEmpty(fileUrl) ||
                string.IsNullOrEmpty(directory) ||
                string.IsNullOrEmpty(type))
                continue;
            
            var userIdRaw = car.SelectSingleNode("userid")?.InnerText;
            if (!int.TryParse(userIdRaw, out var userId))
                userId = 0;

            var teamIdRaw = car.SelectSingleNode("teamid")?.InnerText;
            if (!int.TryParse(teamIdRaw, out var teamId))
                teamId = 0;


            result.Add(new DownloadFile(
                new DownloadId(userId, teamId, directory, TradingPaintsUserFetcher.ToPaintType(type)),
                fileUrl
            ));
        }

        return result;
    }

}