// See https://aka.ms/new-console-template for more information
using irsdkSharp;

var sdk = new IRacingSDK();
await Loop(sdk);

static async Task Loop(IRacingSDK sdk)
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
        var downloads = SessionInfoParser.GetRequiredDownloads(session);
        if (downloads == null)
        {
            Console.WriteLine("Nothing to download.");
            return;
        }

        Console.WriteLine(
            $"Session: {downloads.SessionId} needs downloads: {string.Join(',', downloads.Paints)}"
        );
        return;
    }
}
