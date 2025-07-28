using Microsoft.Extensions.Logging;

namespace TPDownloader.UI;

internal class UILogger(MainForm mainForm) : ILogger
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
        var message = $"[{ShortName(logLevel)}] {formatter(state, exception)}";
        var color = logLevel switch
        {
            LogLevel.Warning => Color.OrangeRed,
            LogLevel.Error => Color.Red,
            LogLevel.Critical => Color.DarkRed,
            _ => Color.Black,
        };
        mainForm.AppendLog(message, color);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }

    private string ShortName(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => "Trade",
            LogLevel.Debug => "Debug",
            LogLevel.Information => "Info",
            LogLevel.Warning => "Warn",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null),
        };
}
