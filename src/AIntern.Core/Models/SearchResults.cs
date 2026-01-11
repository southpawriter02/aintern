namespace AIntern.Core.Models;

/// <summary>Container for grouped search results.</summary>
public sealed class SearchResults
{
    /// <summary>Matching conversations.</summary>
    public required IReadOnlyList<SearchResult> Conversations { get; init; }

    /// <summary>Matching messages.</summary>
    public required IReadOnlyList<SearchResult> Messages { get; init; }

    /// <summary>Total count of all results.</summary>
    public required int TotalCount { get; init; }

    /// <summary>Time taken to execute the search.</summary>
    public required TimeSpan SearchDuration { get; init; }

    /// <summary>Returns true if no results were found.</summary>
    public bool IsEmpty => TotalCount == 0;

    /// <summary>Empty result set.</summary>
    public static SearchResults Empty => new()
    {
        Conversations = [],
        Messages = [],
        TotalCount = 0,
        SearchDuration = TimeSpan.Zero
    };
}
