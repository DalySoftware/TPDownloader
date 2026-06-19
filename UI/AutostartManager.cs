using System.Text;

namespace TPDownloader.UI;

internal static class AutostartManager
{
    private const string StartupFileName = "TPDownloader.cmd";

    private static string StartupFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            StartupFileName
        );

    public static bool IsEnabled()
    {
        if (!File.Exists(StartupFilePath))
            return false;

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
            return true;

        return File.ReadAllText(StartupFilePath).Contains(exePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
                throw new InvalidOperationException("Could not determine TPDownloader executable path.");

            var content = new StringBuilder()
                .AppendLine("@echo off")
                .Append("start \"\" \"")
                .Append(exePath)
                .AppendLine("\"")
                .ToString();

            Directory.CreateDirectory(Path.GetDirectoryName(StartupFilePath)!);
            File.WriteAllText(StartupFilePath, content, Encoding.ASCII);
            return;
        }

        if (File.Exists(StartupFilePath))
            File.Delete(StartupFilePath);
    }
}
