using AIntern.Core.Entities;
using AIntern.Core.Models;
using AIntern.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AIntern.Services.Tests;

/// <summary>
/// Unit and integration tests for <see cref="SearchService"/> (v0.2.5b).
/// Tests search delegation, recent search tracking, and suggestions.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor validation for null dependencies</description></item>
///   <item><description>SearchAsync delegates to DbContext and tracks searches</description></item>
///   <item><description>Empty/invalid queries return empty results without DB calls</description></item>
///   <item><description>GetSuggestionsAsync filters recent searches by prefix</description></item>
///   <item><description>Recent search tracking with max limit enforcement</description></item>
///   <item><description>RebuildIndexAsync delegates to DbContext</description></item>
/// </list>
/// <para>
/// Integration tests use SQLite in-memory databases to test full search flow.
/// Unit tests verify the suggestion logic in isolation.
/// </para>
/// <para>Added in v0.2.5b.</para>
/// </remarks>
public class SearchServiceTests : IDisposable
{
    #region Test Infrastructure

    /// <summary>
    /// SQLite connection kept open for in-memory database lifetime.
    /// </summary>
    private SqliteConnection? _connection;

    /// <summary>
    /// Mock logger for the search service.
    /// </summary>
    private readonly Mock<ILogger<SearchService>> _mockLogger;

    public SearchServiceTests()
    {
        _mockLogger = new Mock<ILogger<SearchService>>();
    }

    /// <summary>
    /// Creates an in-memory SQLite DbContext with FTS5 infrastructure initialized.
    /// </summary>
    private async Task<AInternDbContext> CreateInMemoryContextAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);
        await context.Database.EnsureCreatedAsync();
        await context.EnsureFts5TablesAsync();

        return context;
    }

    /// <summary>
    /// Creates a SearchService with the given DbContext.
    /// </summary>
    private SearchService CreateService(AInternDbContext context)
    {
        return new SearchService(context, _mockLogger.Object);
    }

    /// <summary>
    /// Creates a conversation entity with the specified title.
    /// </summary>
    private static ConversationEntity CreateConversation(string title)
    {
        return new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a message entity with the specified content.
    /// </summary>
    private static MessageEntity CreateMessage(Guid conversationId, string content, int sequenceNumber = 0)
    {
        return new MessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = content,
            SequenceNumber = sequenceNumber,
            Timestamp = DateTime.UtcNow,
            IsComplete = true
        };
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies constructor throws when DbContext is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SearchService(null!, _mockLogger.Object));
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public async Task Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SearchService(context, null!));
    }

    /// <summary>
    /// Verifies constructor succeeds with valid dependencies.
    /// </summary>
    [Fact]
    public async Task Constructor_ValidDependencies_Succeeds()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        // Act
        var service = CreateService(context);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region SearchAsync Tests

    /// <summary>
    /// Verifies SearchAsync returns empty for null query text.
    /// </summary>
    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmptyResults()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        var query = SearchQuery.Simple("");

        // Act
        var results = await service.SearchAsync(query);

        // Assert
        Assert.NotNull(results);
        Assert.False(results.HasResults);
        Assert.Equal(0, results.TotalCount);
    }

    /// <summary>
    /// Verifies SearchAsync returns empty for whitespace-only query.
    /// </summary>
    [Fact]
    public async Task SearchAsync_WhitespaceQuery_ReturnsEmptyResults()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        var query = SearchQuery.Simple("   ");

        // Act
        var results = await service.SearchAsync(query);

        // Assert
        Assert.False(results.HasResults);
    }

    /// <summary>
    /// Verifies SearchAsync returns empty when no content type filters are enabled.
    /// </summary>
    [Fact]
    public async Task SearchAsync_NoContentTypeFilters_ReturnsEmptyResults()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        var query = new SearchQuery(
            QueryText: "test",
            MaxResults: 20,
            IncludeConversations: false,
            IncludeMessages: false);

        // Act
        var results = await service.SearchAsync(query);

        // Assert
        Assert.False(results.HasResults);
    }

    /// <summary>
    /// Verifies SearchAsync finds matching conversations.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ValidQuery_FindsMatchingConversations()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        // Create test conversations
        var conv1 = CreateConversation("Machine Learning Tutorial");
        var conv2 = CreateConversation("Web Development Basics");
        context.Conversations.AddRange(conv1, conv2);
        await context.SaveChangesAsync();
        await context.RebuildFts5IndexesAsync();

        var service = CreateService(context);
        var query = SearchQuery.ConversationsOnly("machine");

        // Act
        var results = await service.SearchAsync(query);

        // Assert
        Assert.True(results.HasResults);
        Assert.Equal(1, results.TotalCount);
        Assert.Equal("Machine Learning Tutorial", results.Results[0].Title);
    }

    /// <summary>
    /// Verifies SearchAsync finds matching messages.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ValidQuery_FindsMatchingMessages()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        // Create test data
        var conv = CreateConversation("Test Conversation");
        context.Conversations.Add(conv);
        await context.SaveChangesAsync();

        var msg1 = CreateMessage(conv.Id, "How do I fix the authentication bug?", 1);
        var msg2 = CreateMessage(conv.Id, "Please review my code", 2);
        context.Messages.AddRange(msg1, msg2);
        await context.SaveChangesAsync();
        await context.RebuildFts5IndexesAsync();

        var service = CreateService(context);
        var query = SearchQuery.MessagesOnly("authentication");

        // Act
        var results = await service.SearchAsync(query);

        // Assert
        Assert.True(results.HasResults);
        Assert.Equal(1, results.TotalCount);
        Assert.Contains("authentication", results.Results[0].Preview, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies SearchAsync tracks valid queries in recent searches.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ValidQuery_TracksInRecentSearches()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        var query = SearchQuery.Simple("unique search term");

        // Act
        await service.SearchAsync(query);
        var suggestions = await service.GetSuggestionsAsync("unique");

        // Assert
        Assert.Single(suggestions);
        Assert.Equal("unique search term", suggestions[0]);
    }

    /// <summary>
    /// Verifies SearchAsync does not track empty queries.
    /// </summary>
    [Fact]
    public async Task SearchAsync_EmptyQuery_DoesNotTrackInRecentSearches()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        var emptyQuery = SearchQuery.Simple("");

        // Act
        await service.SearchAsync(emptyQuery);
        var suggestions = await service.GetSuggestionsAsync("");

        // Assert
        Assert.Empty(suggestions);
    }

    /// <summary>
    /// Verifies SearchAsync respects cancellation.
    /// </summary>
    [Fact]
    public async Task SearchAsync_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        var query = SearchQuery.Simple("test");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - use ThrowsAnyAsync since TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SearchAsync(query, cts.Token));
    }

    #endregion

    #region GetSuggestionsAsync Tests

    /// <summary>
    /// Verifies GetSuggestionsAsync returns empty for empty prefix.
    /// </summary>
    [Fact]
    public async Task GetSuggestionsAsync_EmptyPrefix_ReturnsEmptyList()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Add some recent searches
        await service.SearchAsync(SearchQuery.Simple("test search"));

        // Act
        var suggestions = await service.GetSuggestionsAsync("");

        // Assert
        Assert.Empty(suggestions);
    }

    /// <summary>
    /// Verifies GetSuggestionsAsync returns empty for whitespace prefix.
    /// </summary>
    [Fact]
    public async Task GetSuggestionsAsync_WhitespacePrefix_ReturnsEmptyList()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        await service.SearchAsync(SearchQuery.Simple("test search"));

        // Act
        var suggestions = await service.GetSuggestionsAsync("   ");

        // Assert
        Assert.Empty(suggestions);
    }

    /// <summary>
    /// Verifies GetSuggestionsAsync filters by prefix case-insensitively.
    /// </summary>
    [Fact]
    public async Task GetSuggestionsAsync_MatchingPrefix_ReturnsFilteredResults()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Add varied searches
        await service.SearchAsync(SearchQuery.Simple("error handling"));
        await service.SearchAsync(SearchQuery.Simple("error messages"));
        await service.SearchAsync(SearchQuery.Simple("debugging tips"));

        // Act - case-insensitive matching
        var suggestions = await service.GetSuggestionsAsync("ERR");

        // Assert
        Assert.Equal(2, suggestions.Count);
        Assert.All(suggestions, s => Assert.StartsWith("error", s));
    }

    /// <summary>
    /// Verifies GetSuggestionsAsync respects maxSuggestions limit.
    /// </summary>
    [Fact]
    public async Task GetSuggestionsAsync_RespectsMaxLimit()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Add many searches with same prefix
        for (int i = 1; i <= 10; i++)
        {
            await service.SearchAsync(SearchQuery.Simple($"test query {i}"));
        }

        // Act
        var suggestions = await service.GetSuggestionsAsync("test", maxSuggestions: 3);

        // Assert
        Assert.Equal(3, suggestions.Count);
    }

    /// <summary>
    /// Verifies GetSuggestionsAsync returns most recent first.
    /// </summary>
    [Fact]
    public async Task GetSuggestionsAsync_ReturnsMostRecentFirst()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Add searches in order
        await service.SearchAsync(SearchQuery.Simple("test first"));
        await service.SearchAsync(SearchQuery.Simple("test second"));
        await service.SearchAsync(SearchQuery.Simple("test third"));

        // Act
        var suggestions = await service.GetSuggestionsAsync("test");

        // Assert - most recent should be first
        Assert.Equal("test third", suggestions[0]);
        Assert.Equal("test second", suggestions[1]);
        Assert.Equal("test first", suggestions[2]);
    }

    /// <summary>
    /// Verifies GetSuggestionsAsync returns empty when no matches.
    /// </summary>
    [Fact]
    public async Task GetSuggestionsAsync_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        await service.SearchAsync(SearchQuery.Simple("error handling"));

        // Act
        var suggestions = await service.GetSuggestionsAsync("xyz");

        // Assert
        Assert.Empty(suggestions);
    }

    #endregion

    #region Recent Search Tracking Tests

    /// <summary>
    /// Verifies recent searches are capped at 20 items.
    /// </summary>
    [Fact]
    public async Task RecentSearchTracking_ExceedsMax_RemovesOldest()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Add 25 unique searches
        for (int i = 1; i <= 25; i++)
        {
            await service.SearchAsync(SearchQuery.Simple($"search {i}"));
        }

        // Act - get all recent searches
        var suggestions = await service.GetSuggestionsAsync("search", maxSuggestions: 100);

        // Assert - should only have 20
        Assert.Equal(20, suggestions.Count);

        // First 5 should have been removed
        Assert.DoesNotContain("search 1", suggestions);
        Assert.DoesNotContain("search 5", suggestions);

        // Most recent should be present
        Assert.Contains("search 25", suggestions);
        Assert.Contains("search 6", suggestions);
    }

    /// <summary>
    /// Verifies duplicate searches are moved to end (most recent).
    /// </summary>
    [Fact]
    public async Task RecentSearchTracking_Duplicate_MovesToEnd()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Add searches
        await service.SearchAsync(SearchQuery.Simple("test first"));
        await service.SearchAsync(SearchQuery.Simple("test second"));
        await service.SearchAsync(SearchQuery.Simple("test first")); // Duplicate

        // Act
        var suggestions = await service.GetSuggestionsAsync("test");

        // Assert - "test first" should now be most recent
        Assert.Equal(2, suggestions.Count);
        Assert.Equal("test first", suggestions[0]);
        Assert.Equal("test second", suggestions[1]);
    }

    /// <summary>
    /// Verifies searches are normalized to lowercase.
    /// </summary>
    [Fact]
    public async Task RecentSearchTracking_NormalizesToLowercase()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Add search with mixed case
        await service.SearchAsync(SearchQuery.Simple("Error Handling"));

        // Act - search with lowercase prefix
        var suggestions = await service.GetSuggestionsAsync("err");

        // Assert
        Assert.Single(suggestions);
        Assert.Equal("error handling", suggestions[0]); // Should be lowercase
    }

    #endregion

    #region RebuildIndexAsync Tests

    /// <summary>
    /// Verifies RebuildIndexAsync completes without error.
    /// </summary>
    [Fact]
    public async Task RebuildIndexAsync_Succeeds()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        // Add some data
        var conv = CreateConversation("Test Conversation");
        context.Conversations.Add(conv);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert - should not throw
        await service.RebuildIndexAsync();
    }

    /// <summary>
    /// Verifies RebuildIndexAsync makes data searchable.
    /// </summary>
    [Fact]
    public async Task RebuildIndexAsync_MakesDataSearchable()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        // Add data without triggering index update (simulating corruption)
        var conv = CreateConversation("Rebuild Test Conversation");
        context.Conversations.Add(conv);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.RebuildIndexAsync();
        var results = await service.SearchAsync(SearchQuery.ConversationsOnly("rebuild"));

        // Assert
        Assert.True(results.HasResults);
        Assert.Equal("Rebuild Test Conversation", results.Results[0].Title);
    }

    /// <summary>
    /// Verifies RebuildIndexAsync respects cancellation.
    /// </summary>
    [Fact]
    public async Task RebuildIndexAsync_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - use ThrowsAnyAsync since TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.RebuildIndexAsync(cts.Token));
    }

    #endregion

    #region Concurrency Tests

    /// <summary>
    /// Verifies recent search tracking is thread-safe.
    /// </summary>
    [Fact]
    public async Task RecentSearchTracking_ConcurrentSearches_ThreadSafe()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Act - execute many searches concurrently
        var tasks = Enumerable.Range(1, 50)
            .Select(i => service.SearchAsync(SearchQuery.Simple($"concurrent {i}")));

        await Task.WhenAll(tasks);

        // Assert - should have at most 20 recent searches
        var suggestions = await service.GetSuggestionsAsync("concurrent", maxSuggestions: 100);
        Assert.True(suggestions.Count <= 20);
        Assert.True(suggestions.Count > 0);
    }

    #endregion
}
