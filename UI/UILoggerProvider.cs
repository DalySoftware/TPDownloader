using Microsoft.Extensions.Logging;

namespace TPDownloader.UI;

internal class UILoggerProvider(MainForm mainForm) : ILoggerProvider
{
    public ILogger CreateLogger(string _) => new UILogger(mainForm);

    public void Dispose() { }
}
