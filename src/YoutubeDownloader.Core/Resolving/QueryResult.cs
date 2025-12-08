using YoutubeExplode.Videos;

namespace YoutubeDownloader.Core.Resolving;

public record QueryResult
{
    public QueryResult(QueryResultKind Kind, string Title, IReadOnlyList<IVideo> Videos)
    {
        this.Kind = Kind;
        this.Title = Title;
        this.Videos = Videos;
    }

    public static QueryResult Aggregate(IReadOnlyList<QueryResult> results)
    {
        if (!results.Any())
            throw new ArgumentException("Cannot aggregate empty results.", nameof(results));

        return new QueryResult(
            // Single query -> inherit kind, multiple queries -> aggregate
            results.Count == 1
                ? results.Single().Kind
                : QueryResultKind.Aggregate,
            // Single query -> inherit title, multiple queries -> aggregate
            results.Count == 1
                ? results.Single().Title
                : $"{results.Count} queries",
            // Combine all videos, deduplicate by ID
            results.SelectMany(q => q.Videos).DistinctBy(v => v.Id).ToArray()
        );
    }

    public QueryResultKind Kind { get; init; }
    public string Title { get; init; }
    public IReadOnlyList<IVideo> Videos { get; init; }

    public void Deconstruct(
        out QueryResultKind Kind,
        out string Title,
        out IReadOnlyList<IVideo> Videos
    )
    {
        Kind = this.Kind;
        Title = this.Title;
        Videos = this.Videos;
    }
}
