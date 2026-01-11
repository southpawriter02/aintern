namespace AIntern.Core.Models;

/// <summary>Represents a single search result with relevance ranking.</summary>
public sealed class SearchResult
{
    /// <summary>Unique identifier of the matched item.</summary>
    public required Guid Id { get; init; }

    /// <summary>Type of result (Conversation or Message).</summary>
    public required SearchResultType Type { get; init; }

    /// <summary>Title or summary of the result.</summary>
    public required string Title { get; init; }

    /// <summary>Plain text snippet around the match.</summary>
    public required string Snippet { get; init; }

    /// <summary>HTML snippet with &lt;mark&gt; tags highlighting matches.</summary>
    public required string HighlightedSnippet { get; init; }

    /// <summary>BM25 relevance score (lower = more relevant, as bm25 returns negative values).</summary>
    public required double Rank { get; init; }

    /// <summary>Timestamp of the matched item.</summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>For message results, the parent conversation ID.</summary>
    public Guid? ConversationId { get; init; }

    /// <summary>For message results, the parent conversation title.</summary>
    public string? ConversationTitle { get; init; }
}

/// <summary>Type of search result.</summary>
public enum SearchResultType
{
    /// <summary>Result from conversation titles.</summary>
    Conversation,

    /// <summary>Result from message content.</summary>
    Message
}
