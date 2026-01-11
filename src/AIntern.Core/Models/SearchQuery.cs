namespace AIntern.Core.Models;

/// <summary>Represents a search query with options.</summary>
public sealed class SearchQuery
{
    /// <summary>The search text (required).</summary>
    public required string Text { get; init; }

    /// <summary>Optional filter to search only conversations or messages.</summary>
    public SearchResultType? FilterType { get; init; }

    /// <summary>Maximum number of results to return (default: 50).</summary>
    public int MaxResults { get; init; } = 50;

    /// <summary>Approximate length of snippets in characters (default: 150).</summary>
    public int SnippetLength { get; init; } = 150;

    /// <summary>Creates a query for searching all types.</summary>
    public static SearchQuery All(string text) => new() { Text = text };

    /// <summary>Creates a query for searching conversations only.</summary>
    public static SearchQuery Conversations(string text) => new()
    {
        Text = text,
        FilterType = SearchResultType.Conversation
    };

    /// <summary>Creates a query for searching messages only.</summary>
    public static SearchQuery Messages(string text) => new()
    {
        Text = text,
        FilterType = SearchResultType.Message
    };
}
