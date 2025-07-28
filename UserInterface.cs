using irsdkSharp;
using TradingPaints;

internal class UserInterface(IRacingSDK sdk, SessionDownloader downloader)
{
    internal async Task MainLoop()
    {
        while (true)
        {
            if (!sdk.IsConnected())
            {
                Console.WriteLine("...waiting to connect");
                await Task.Delay(TimeSpan.FromSeconds(2));
                continue;
            }

            Console.WriteLine("Connected!");
            var session = sdk.GetSessionInfo();
            var sessionToDownload = SessionInfoParser.GetRequiredDownloads(session);
            if (sessionToDownload == null)
            {
                Console.WriteLine("Nothing to download.");
                return;
            }

            Console.WriteLine(
                $"Session: {sessionToDownload.SessionId} needs downloads: {string.Join(',', sessionToDownload.PaintIds)}"
            );
            var downloaded = (await downloader.DownloadSession(sessionToDownload)).ToHashSet();

            Console.WriteLine(
                $"Moving {downloaded.Count} files for session {sessionToDownload.SessionId}."
            );
            await PaintSaver.SaveSessionPaints(downloaded);
            return;
        }
    }
}
