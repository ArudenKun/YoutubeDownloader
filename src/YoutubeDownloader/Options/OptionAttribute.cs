namespace YoutubeDownloader.Options;

[AttributeUsage(AttributeTargets.Class)]
public class OptionAttribute(string? section = null) : Attribute
{
    public string? Section { get; } = section;
}
