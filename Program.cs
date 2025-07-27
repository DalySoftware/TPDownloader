// See https://aka.ms/new-console-template for more information
using irsdkSharp;

Console.WriteLine("Hello, World!");

var sdk = new IRacingSDK();

await Loop();

async Task Loop()
{
    while (true)
    {
        var currentlyConnected = sdk.IsConnected();

        // Check if we can find the sim
        if (currentlyConnected)
        {
            Console.WriteLine("Connected!");
            var session = sdk.GetSessionInfo();
            var downloads = SessionInfoParser.GetRequiredDownloads(session);
            Console.WriteLine($"To download: {string.Join(", ", downloads)}");
            return;
        }

        Console.WriteLine("...waiting to connect");
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}
