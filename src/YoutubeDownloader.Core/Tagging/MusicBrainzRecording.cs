namespace YoutubeDownloader.Core.Tagging;

internal record MusicBrainzRecording
{
    public MusicBrainzRecording(string Artist, string? ArtistSort, string Title, string? Album)
    {
        this.Artist = Artist;
        this.ArtistSort = ArtistSort;
        this.Title = Title;
        this.Album = Album;
    }

    public string Artist { get; init; }
    public string? ArtistSort { get; init; }
    public string Title { get; init; }
    public string? Album { get; init; }

    public void Deconstruct(
        out string Artist,
        out string? ArtistSort,
        out string Title,
        out string? Album
    )
    {
        Artist = this.Artist;
        ArtistSort = this.ArtistSort;
        Title = this.Title;
        Album = this.Album;
    }
}
