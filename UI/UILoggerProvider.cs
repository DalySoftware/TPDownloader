using Microsoft.Extensions.Logging;

namespace TPDownloader.UI;

internal class UILoggerProvider(MainForm mainForm) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new UILogger(mainForm, categoryName);

    public void Dispose() { }
}
