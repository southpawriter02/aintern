using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Provides full-text search functionality across conversations and messages.
/// </summary>
/// <remarks>
/// <para>
/// This service wraps the FTS5 search infrastructure in <see cref="Data.AInternDbContext"/>
/// and provides:
/// </para>
/// <list type="bullet">
///   <item><description><b>Search:</b> Full-text search with BM25 ranking</description></item>
///   <item><description><b>Suggestions:</b> Recent search tracking for autocomplete</description></item>
///   <item><description><b>Maintenance:</b> Index rebuild capability</description></item>
/// </list>
/// <para>
/// The service maintains an in-memory cache of recent searches (up to 20) for
/// providing autocomplete suggestions. This cache is session-scoped and not persisted.
/// </para>
/// <para>Added in v0.2.5b.</para>
/// </remarks>
/// <example>
/// Basic search usage:
/// <code>
/// var searchService = serviceProvider.GetRequiredService&lt;ISearchService&gt;();
///
/// // Simple search
/// var query = SearchQuery.Simple("hello world");
/// var results = await searchService.SearchAsync(query);
///
/// // Get suggestions for autocomplete
/// var suggestions = await searchService.GetSuggestionsAsync("hel");
/// </code>
/// </example>
public interface ISearchService
{
    #region Search Operations

    /// <summary>
    /// Searches conversations and messages using FTS5 full-text search.
    /// </summary>
    /// <param name="query">
    /// The search query containing the search text and filters.
    /// Use <see cref="SearchQuery.Simple(string, int)"/> for basic searches.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="SearchResults"/> containing matching items sorted by relevance (BM25 rank).
    /// Returns <see cref="SearchResults.Empty(SearchQuery)"/> if the query is invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method delegates to <c>AInternDbContext.SearchAsync()</c> for the actual
    /// FTS5 query execution. Valid searches are tracked in recent searches for
    /// autocomplete suggestions.
    /// </para>
    /// <para>
    /// Results are ranked using SQLite's BM25 algorithm, with lower rank values
    /// indicating higher relevance.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Search all content types
    /// var results = await searchService.SearchAsync(SearchQuery.Simple("error handling"));
    ///
    /// // Search only conversations
    /// var convResults = await searchService.SearchAsync(SearchQuery.ConversationsOnly("project plan"));
    ///
    /// // Search only messages
    /// var msgResults = await searchService.SearchAsync(SearchQuery.MessagesOnly("bug fix"));
    /// </code>
    /// </example>
    Task<SearchResults> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default);

    #endregion

    #region Index Maintenance

    /// <summary>
    /// Rebuilds the FTS5 indexes from source data.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the rebuild is finished.</returns>
    /// <remarks>
    /// <para>
    /// This operation clears and repopulates the FTS5 virtual tables from the
    /// source <c>Conversations</c> and <c>Messages</c> tables. Use this to:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Repair corrupted indexes</description></item>
    ///   <item><description>Reindex after bulk imports</description></item>
    ///   <item><description>Ensure index consistency</description></item>
    /// </list>
    /// <para>
    /// Note: This operation may take several seconds for large databases.
    /// Consider running it on a background thread with progress indication.
    /// </para>
    /// </remarks>
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Suggestions

    /// <summary>
    /// Gets search suggestions based on recent searches.
    /// </summary>
    /// <param name="prefix">
    /// The prefix to match against recent searches. Case-insensitive.
    /// Pass an empty string to get all recent searches.
    /// </param>
    /// <param name="maxSuggestions">
    /// Maximum number of suggestions to return. Defaults to 5.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A list of recent search queries matching the prefix, ordered by recency
    /// (most recent first). Returns an empty list if no matches found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Suggestions are drawn from an in-memory cache of recent searches
    /// (maximum 20 entries). The cache is session-scoped and not persisted
    /// across application restarts.
    /// </para>
    /// <para>
    /// Matching is case-insensitive and uses <see cref="string.StartsWith(string)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get suggestions for partial input
    /// var suggestions = await searchService.GetSuggestionsAsync("err", maxSuggestions: 3);
    /// // Might return: ["error handling", "error messages", "error codes"]
    ///
    /// // Get all recent searches
    /// var allRecent = await searchService.GetSuggestionsAsync("", maxSuggestions: 10);
    /// </code>
    /// </example>
    Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string prefix,
        int maxSuggestions = 5,
        CancellationToken cancellationToken = default);

    #endregion
}
