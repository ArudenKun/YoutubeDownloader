using System.Text.RegularExpressions;
using YoutubeDownloader.Core.Utilities.Extensions;

namespace YoutubeDownloader.Core.Utilities;

public static class Url
{
    public static string? TryExtractFileName(string url) =>
        Regex.Match(url, @".+/([^?]*)").Groups[1].Value.NullIfEmptyOrWhiteSpace();
}
