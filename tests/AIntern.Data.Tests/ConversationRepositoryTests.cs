using AIntern.Core.Entities;
using AIntern.Core.Models;
using AIntern.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIntern.Data.Tests;

/// <summary>
/// Unit tests for the <see cref="ConversationRepository"/> class.
/// Verifies CRUD operations, archive/pin functionality, and message management.
/// </summary>
/// <remarks>
/// <para>
/// These tests use SQLite in-memory databases to verify:
/// </para>
/// <list type="bullet">
///   <item><description>Repository instantiation and constructor validation</description></item>
///   <item><description>Conversation CRUD operations</description></item>
///   <item><description>Archive and pin flag management</description></item>
///   <item><description>Message operations with automatic sequence numbering</description></item>
///   <item><description>Search functionality</description></item>
/// </list>
/// </remarks>
public class ConversationRepositoryTests : IDisposable
{
    #region Test Infrastructure

    /// <summary>
    /// SQLite connection kept open for in-memory database lifetime.
    /// </summary>
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Database context for repository operations.
    /// </summary>
    private readonly AInternDbContext _context;

    /// <summary>
    /// Repository under test.
    /// </summary>
    private readonly ConversationRepository _repository;

    /// <summary>
    /// Initializes test infrastructure with an in-memory SQLite database.
    /// </summary>
    public ConversationRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);
        _context.Database.EnsureCreated();

        _repository = new ConversationRepository(_context);
    }

    /// <summary>
    /// Disposes of the test infrastructure.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConversationRepository(null!));
    }

    /// <summary>
    /// Verifies that the constructor accepts null logger and uses NullLogger.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_UsesNullLogger()
    {
        // Arrange & Act
        var repository = new ConversationRepository(_context, null);

        // Assert - no exception means NullLogger was used
        Assert.NotNull(repository);
    }

    #endregion

    #region CRUD Tests

    /// <summary>
    /// Verifies that CreateAsync generates a new ID when the entity has an empty GUID.
    /// </summary>
    [Fact]
    public async Task CreateAsync_GeneratesId_WhenIdIsEmpty()
    {
        // Arrange
        var conversation = new ConversationEntity
        {
            Title = "Test Conversation"
        };

        // Act
        var result = await _repository.CreateAsync(conversation);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    /// <summary>
    /// Verifies that GetByIdAsync returns null when the conversation is not found.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetByIdWithMessagesAsync includes messages ordered by sequence number.
    /// </summary>
    [Fact]
    public async Task GetByIdWithMessagesAsync_IncludesMessages()
    {
        // Arrange
        var conversation = await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Test Conversation"
        });

        await _repository.AddMessageAsync(conversation.Id, new MessageEntity
        {
            Role = MessageRole.User,
            Content = "Hello"
        });

        await _repository.AddMessageAsync(conversation.Id, new MessageEntity
        {
            Role = MessageRole.Assistant,
            Content = "Hi there!"
        });

        // Act
        var result = await _repository.GetByIdWithMessagesAsync(conversation.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(1, result.Messages.First().SequenceNumber);
        Assert.Equal(2, result.Messages.Last().SequenceNumber);
    }

    /// <summary>
    /// Verifies that UpdateAsync modifies the entity in the database.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ModifiesEntity()
    {
        // Arrange
        var conversation = await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Original Title"
        });

        // Act
        var retrieved = await _context.Conversations.FindAsync(conversation.Id);
        retrieved!.Title = "Updated Title";
        await _repository.UpdateAsync(retrieved);

        // Assert
        var updated = await _repository.GetByIdAsync(conversation.Id);
        Assert.Equal("Updated Title", updated!.Title);
    }

    /// <summary>
    /// Verifies that DeleteAsync removes the conversation and cascades to messages.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_RemovesConversationAndMessages()
    {
        // Arrange
        var conversation = await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Test Conversation"
        });

        await _repository.AddMessageAsync(conversation.Id, new MessageEntity
        {
            Role = MessageRole.User,
            Content = "Test message"
        });

        // Act
        await _repository.DeleteAsync(conversation.Id);

        // Assert
        var deletedConversation = await _repository.GetByIdAsync(conversation.Id);
        var remainingMessages = await _context.Messages
            .Where(m => m.ConversationId == conversation.Id)
            .CountAsync();

        Assert.Null(deletedConversation);
        Assert.Equal(0, remainingMessages);
    }

    #endregion

    #region Archive/Pin Tests

    /// <summary>
    /// Verifies that GetRecentAsync excludes archived conversations by default.
    /// </summary>
    [Fact]
    public async Task GetRecentAsync_ExcludesArchived_ByDefault()
    {
        // Arrange
        var active = await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Active Conversation"
        });

        var archived = await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Archived Conversation"
        });

        await _repository.ArchiveAsync(archived.Id);

        // Act
        var results = await _repository.GetRecentAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("Active Conversation", results[0].Title);
    }

    /// <summary>
    /// Verifies that GetRecentAsync sorts pinned conversations first.
    /// </summary>
    [Fact]
    public async Task GetRecentAsync_SortsPinnedFirst()
    {
        // Arrange
        var unpinned = await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Unpinned Conversation"
        });

        // Wait a moment to ensure different UpdatedAt
        await Task.Delay(10);

        var pinned = await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Pinned Conversation"
        });

        await _repository.PinAsync(pinned.Id);

        // Act
        var results = await _repository.GetRecentAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Pinned Conversation", results[0].Title);
    }

    #endregion

    #region Message Tests

    /// <summary>
    /// Verifies that AddMessageAsync assigns sequential sequence numbers.
    /// </summary>
    [Fact]
    public async Task AddMessageAsync_AssignsSequenceNumber()
    {
        // Arrange
        var conversation = await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Test Conversation"
        });

        // Act
        var message1 = await _repository.AddMessageAsync(conversation.Id, new MessageEntity
        {
            Role = MessageRole.User,
            Content = "First"
        });

        var message2 = await _repository.AddMessageAsync(conversation.Id, new MessageEntity
        {
            Role = MessageRole.Assistant,
            Content = "Second"
        });

        // Assert
        Assert.Equal(1, message1.SequenceNumber);
        Assert.Equal(2, message2.SequenceNumber);
    }

    /// <summary>
    /// Verifies that AddMessageAsync increments the conversation's MessageCount.
    /// </summary>
    [Fact]
    public async Task AddMessageAsync_IncrementsMessageCount()
    {
        // Arrange
        var conversation = await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Test Conversation"
        });

        // Act
        await _repository.AddMessageAsync(conversation.Id, new MessageEntity
        {
            Role = MessageRole.User,
            Content = "Hello",
            TokenCount = 10
        });

        // Assert - need to refresh from database
        var updated = await _context.Conversations
            .AsNoTracking()
            .FirstAsync(c => c.Id == conversation.Id);

        Assert.Equal(1, updated.MessageCount);
        Assert.Equal(10, updated.TotalTokenCount);
    }

    #endregion

    #region Search Tests

    /// <summary>
    /// Verifies that SearchAsync finds conversations by title substring.
    /// </summary>
    [Fact]
    public async Task SearchAsync_FindsByTitleSubstring()
    {
        // Arrange
        await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Alpha Test Conversation"
        });

        await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Beta Discussion"
        });

        await _repository.CreateAsync(new ConversationEntity
        {
            Title = "Another Test Topic"
        });

        // Act
        var results = await _repository.SearchAsync("Test");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("Test", r.Title));
    }

    #endregion
}
