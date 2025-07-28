using Microsoft.Extensions.Logging;

namespace TPDownloader.UI;

internal class UILogger(MainForm mainForm, string category) : ILogger
{
    IDisposable? ILogger.BeginScope<TState>(TState state) => new NoopDisposable();

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        var message = $"[{logLevel}] {category}: {formatter(state, exception)}";
        mainForm.AppendLog(message);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
