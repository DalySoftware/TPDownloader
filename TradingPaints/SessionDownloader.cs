using Microsoft.Extensions.Logging;

namespace TPDownloader.TradingPaints
{
    internal class SessionDownloader(
        ILogger<SessionDownloader> logger,
        HttpClient httpClient,
        TradingPaintsFetcherFactory fetcherFactory
    )
    {
        internal async Task<IEnumerable<DownloadedFile>> DownloadSession(Session session)
        {
            logger.LogInformation("Starting download for {SessionId}", session.Id);

            return await DownloadPaints(session);
        }

        private async Task<IEnumerable<DownloadedFile>> DownloadPaints(Session session)
        {
            if (Directory.Exists(session.Id.SessionFolder()))
            {
                logger.LogInformation(
                    "Session folder {SessionFolder} already exists. Removing existing files",
                    session.Id.SessionFolder()
                );
                Directory.Delete(session.Id.SessionFolder(), true);
            }

            var allFiles = await FetchSessionFiles(session);
            var fileTasks = new List<Task<DownloadedFile?>>();
            foreach (var file in allFiles)
            {
                var task = DownloadPaintToTempDirectory(session.Id, file.Id, new Uri(file.Url));
                fileTasks.Add(task);
            }

            var results = await Task.WhenAll(fileTasks);
            return results.OfType<DownloadedFile>();
        }
        
        private async Task<List<DownloadFile>> FetchSessionFiles(Session session) => 
            session.TeamRacing ? FetchTeamSessionFiles(session) : await FetchIndividualSessionFiles(session);

        private async Task<List<DownloadFile>> FetchIndividualSessionFiles(Session session)
        {
            var files = new List<DownloadFile>();

            foreach (var user in session.Users)
            {
                var allUserFiles = await fetcherFactory.Create(user.UserId)
                    .FetchPaintFilesAsync();

                files.AddRange(allUserFiles.Where(f =>
                    f.Id.Type is PaintType.Helmet or PaintType.Suit
                    || string.Equals(f.Id.Directory, user.Directory, StringComparison.OrdinalIgnoreCase)
                ));
            }

            return files;
        }

        private List<DownloadFile> FetchTeamSessionFiles(Session session) => [];

        private async Task<DownloadedFile?> DownloadPaintToTempDirectory(
            Session.SessionId sessionId,
            DownloadId downloadId,
            Uri url
        )
        {
            var downloadPath = downloadId.DownloadPath(sessionId);
            Directory.CreateDirectory(downloadPath);

            var fileName = Path.GetFileName(url.LocalPath);
            var destinationPath = Path.Combine(downloadPath, fileName);

            logger.LogDebug("Downloading {Url} to {DestinationPath}", url, destinationPath);
            var fileResponse = await httpClient.GetAsync(url);
            if (!fileResponse.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Failed to download {Url}. Status code: {StatusCode}",
                    url,
                    fileResponse.StatusCode
                );
                return null;
            }

            using var fileStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write
            );
            await fileResponse.Content.CopyToAsync(fileStream);
            return new DownloadedFile(sessionId, downloadId, destinationPath);
        }
    }
}
