using ICSharpCode.SharpZipLib.BZip2;

namespace TPDownloader.IRacing;

internal static class FileExtractor
{
    internal static async Task ExtractBz2FileAsync(string sourcePath, string destinationPath)
    {
        if (Path.GetDirectoryName(destinationPath) is { } directory)
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        using var decompressionStream = new BZip2InputStream(fileStream);
        using var outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);

        await decompressionStream.CopyToAsync(outputStream);
    }
}
