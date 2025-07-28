using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace TPDownloader
{
    public sealed class SimpleConsoleFormatter : ConsoleFormatter
    {
        private const string Green = "\u001b[32m";
        private const string Yellow = "\u001b[33m";
        private const string Red = "\u001b[31m";
        private const string BrightRed = "\u001b[31;1m";
        private const string Reset = "\u001b[0m";

        public SimpleConsoleFormatter()
            : base("SimpleConsoleFormatter") { }

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider? scopeProvider,
            TextWriter textWriter
        )
        {
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            string color = GetLogLevelColor(logEntry.LogLevel);

            if (logEntry.Exception == null)
            {
                textWriter.Write(
                    $"{color}{GetLogLevelString(logEntry.LogLevel)}{Reset}: {message}{Environment.NewLine}"
                );
                return;
            }

            textWriter.Write(
                $"{color}{GetLogLevelString(logEntry.LogLevel)}{Reset}: {message} {logEntry.Exception}{Environment.NewLine}"
            );
        }

        private static string GetLogLevelString(LogLevel logLevel) =>
            logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel)),
            };

        private static string GetLogLevelColor(LogLevel logLevel) =>
            logLevel switch
            {
                LogLevel.Information => Green,
                LogLevel.Warning => Yellow,
                LogLevel.Error => Red,
                LogLevel.Critical => BrightRed,
                _ => string.Empty,
            };
    }
}
