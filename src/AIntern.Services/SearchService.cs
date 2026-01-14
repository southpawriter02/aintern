namespace AIntern.Services;

using System.Diagnostics;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Data;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides full-text search functionality across conversations and messages.
/// </summary>
/// <remarks>
/// <para>
/// This service wraps the FTS5 search infrastructure in <see cref="AInternDbContext"/>
/// and provides:
/// </para>
/// <list type="bullet">
///   <item><description><b>Search:</b> Full-text search with BM25 ranking via DbContext</description></item>
///   <item><description><b>Suggestions:</b> In-memory recent search tracking for autocomplete</description></item>
///   <item><description><b>Maintenance:</b> Index rebuild capability via DbContext</description></item>
/// </list>
/// <para>
/// The service maintains a thread-safe in-memory cache of recent searches (up to 20)
/// for providing autocomplete suggestions. This cache is session-scoped and not persisted
/// across application restarts.
/// </para>
/// <para>Added in v0.2.5b.</para>
/// </remarks>
public sealed class SearchService : ISearchService
{
    #region Constants

    /// <summary>
    /// Maximum number of recent searches to track for suggestions.
    /// </summary>
    private const int MaxRecentSearches = 20;

    #endregion

    #region Fields

    private readonly AInternDbContext _dbContext;
    private readonly ILogger<SearchService> _logger;

    /// <summary>
    /// Thread-safe collection of recent search queries for autocomplete suggestions.
    /// Most recent searches are at the end of the list.
    /// </summary>
    private readonly List<string> _recentSearches = new(capacity: MaxRecentSearches);

    /// <summary>
    /// Lock object for thread-safe access to <see cref="_recentSearches"/>.
    /// </summary>
    private readonly object _recentSearchesLock = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchService"/> class.
    /// </summary>
    /// <param name="dbContext">Database context for FTS5 search operations.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dbContext"/> or <paramref name="logger"/> is null.
    /// </exception>
    public SearchService(AInternDbContext dbContext, ILogger<SearchService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("[INIT] SearchService created");
    }

    #endregion

    #region ISearchService Implementation

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This method delegates to <see cref="AInternDbContext.SearchAsync"/> for the actual
    /// FTS5 query execution. Valid searches (non-empty, normalized) are tracked in recent
    /// searches for autocomplete suggestions.
    /// </para>
    /// <para>
    /// Empty or whitespace-only queries return <see cref="SearchResults.Empty(SearchQuery)"/>
    /// without hitting the database or tracking in recent searches.
    /// </para>
    /// </remarks>
    public async Task<SearchResults> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SearchAsync - {LogSummary}", query.LogSummary);

        // Skip empty/invalid queries.
        if (!query.IsValid)
        {
            _logger.LogDebug("[SKIP] SearchAsync - Invalid query (empty or whitespace), returning empty results");
            return SearchResults.Empty(query);
        }

        // Skip if no content type filters are enabled.
        if (!query.HasContentTypeFilter)
        {
            _logger.LogDebug("[SKIP] SearchAsync - No content type filters enabled, returning empty results");
            return SearchResults.Empty(query);
        }

        try
        {
            // Track valid query in recent searches for autocomplete.
            TrackRecentSearch(query.NormalizedQueryText);

            // Delegate to DbContext for FTS5 search.
            var results = await _dbContext.SearchAsync(query, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] SearchAsync - {Summary}, Duration: {Ms}ms",
                results.Summary,
                stopwatch.ElapsedMilliseconds);

            return results;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] SearchAsync - Cancelled after {Ms}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[ERROR] SearchAsync - Failed after {Ms}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This method delegates to <see cref="AInternDbContext.RebuildFts5IndexesAsync"/>
    /// for the actual index rebuild. The operation clears and repopulates the FTS5
    /// virtual tables from the source Conversations and Messages tables.
    /// </para>
    /// <para>
    /// Note: This operation may take several seconds for large databases.
    /// Consider running it with progress indication on a background thread.
    /// </para>
    /// </remarks>
    public async Task RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] RebuildIndexAsync");

        try
        {
            await _dbContext.RebuildFts5IndexesAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] RebuildIndexAsync - Index rebuild completed in {Ms}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] RebuildIndexAsync - Cancelled after {Ms}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[ERROR] RebuildIndexAsync - Failed after {Ms}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Suggestions are drawn from an in-memory cache of recent searches
    /// (maximum 20 entries). The cache is session-scoped and not persisted
    /// across application restarts.
    /// </para>
    /// <para>
    /// Matching is case-insensitive and uses <see cref="string.StartsWith(string, StringComparison)"/>
    /// with <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </para>
    /// <para>
    /// If <paramref name="prefix"/> is empty or whitespace, an empty list is returned
    /// (we don't return all recent searches for empty input).
    /// </para>
    /// </remarks>
    public Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string prefix,
        int maxSuggestions = 5,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] GetSuggestionsAsync - Prefix: '{Prefix}', MaxSuggestions: {Max}",
            prefix,
            maxSuggestions);

        // Return empty list for empty/whitespace prefix.
        if (string.IsNullOrWhiteSpace(prefix))
        {
            _logger.LogDebug("[SKIP] GetSuggestionsAsync - Empty prefix, returning empty list");
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        // Normalize prefix for matching.
        var normalizedPrefix = prefix.Trim().ToLowerInvariant();

        // Filter recent searches by prefix (thread-safe).
        IReadOnlyList<string> suggestions;
        lock (_recentSearchesLock)
        {
            suggestions = _recentSearches
                .Where(s => s.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                .Reverse() // Most recent first
                .Take(maxSuggestions)
                .ToList();
        }

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] GetSuggestionsAsync - Found {Count} suggestions in {Ms}ms",
            suggestions.Count,
            stopwatch.ElapsedMilliseconds);

        return Task.FromResult(suggestions);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Tracks a search query in the recent searches list for autocomplete suggestions.
    /// </summary>
    /// <param name="query">The normalized search query to track.</param>
    /// <remarks>
    /// <para>
    /// This method is thread-safe and maintains a maximum of <see cref="MaxRecentSearches"/>
    /// entries in the cache. When the limit is reached, the oldest entry is removed.
    /// </para>
    /// <para>
    /// Duplicate queries are moved to the end of the list (most recent position).
    /// Queries are stored in lowercase for case-insensitive matching.
    /// </para>
    /// </remarks>
    private void TrackRecentSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        // Normalize to lowercase for consistent matching.
        var normalizedQuery = query.Trim().ToLowerInvariant();

        lock (_recentSearchesLock)
        {
            // Remove existing duplicate (will re-add at end for recency).
            _recentSearches.Remove(normalizedQuery);

            // Add to end (most recent).
            _recentSearches.Add(normalizedQuery);

            // Enforce max limit by removing oldest (first).
            while (_recentSearches.Count > MaxRecentSearches)
            {
                var removed = _recentSearches[0];
                _recentSearches.RemoveAt(0);
                _logger.LogDebug(
                    "[INFO] TrackRecentSearch - Removed oldest search: '{Query}', Count: {Count}",
                    removed,
                    _recentSearches.Count);
            }
        }

        _logger.LogDebug(
            "[INFO] TrackRecentSearch - Tracked: '{Query}', Total: {Count}",
            normalizedQuery,
            _recentSearches.Count);
    }

    #endregion
}
