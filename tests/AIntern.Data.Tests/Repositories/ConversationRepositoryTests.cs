using Xunit;
using AIntern.Core.Entities;
using AIntern.Core.Models;
using AIntern.Data.Repositories;

namespace AIntern.Data.Tests.Repositories;

/// <summary>
/// Unit tests for ConversationRepository (v0.2.1).
/// </summary>
public class ConversationRepositoryTests : IDisposable
{
    private readonly TestDbContextFactoryWrapper _contextFactory;
    private readonly ConversationRepository _repository;

    public ConversationRepositoryTests()
    {
        _contextFactory = new TestDbContextFactoryWrapper();
        _repository = new ConversationRepository(_contextFactory);
    }

    public void Dispose()
    {
        _contextFactory.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_CreatesNewConversation()
    {
        var conversation = new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _repository.CreateAsync(conversation);

        Assert.NotNull(result);
        Assert.Equal(conversation.Id, result.Id);
        Assert.Equal("Test Conversation", result.Title);
    }

    [Fact]
    public async Task CreateAsync_PersistsToDatabase()
    {
        var conversation = new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = "Persisted Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(conversation);

        var retrieved = await _repository.GetByIdAsync(conversation.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Persisted Conversation", retrieved.Title);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ReturnsConversation_WhenExists()
    {
        var conversation = await CreateTestConversation("Test");

        var result = await _repository.GetByIdAsync(conversation.Id);

        Assert.NotNull(result);
        Assert.Equal(conversation.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    #endregion

    #region GetByIdWithMessagesAsync Tests

    [Fact]
    public async Task GetByIdWithMessagesAsync_IncludesMessages()
    {
        var conversation = await CreateTestConversation("Test");
        await AddTestMessage(conversation.Id, "Message 1");
        await AddTestMessage(conversation.Id, "Message 2");

        var result = await _repository.GetByIdWithMessagesAsync(conversation.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result.Messages.Count);
    }

    [Fact]
    public async Task GetByIdWithMessagesAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByIdWithMessagesAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    #endregion

    #region GetRecentAsync Tests

    [Fact]
    public async Task GetRecentAsync_ReturnsOrderedByUpdatedAt()
    {
        var old = await CreateTestConversation("Old", DateTime.UtcNow.AddDays(-2));
        var recent = await CreateTestConversation("Recent", DateTime.UtcNow);
        var middle = await CreateTestConversation("Middle", DateTime.UtcNow.AddDays(-1));

        var result = await _repository.GetRecentAsync(10);

        Assert.Equal(3, result.Count);
        Assert.Equal("Recent", result[0].Title);
        Assert.Equal("Middle", result[1].Title);
        Assert.Equal("Old", result[2].Title);
    }

    [Fact]
    public async Task GetRecentAsync_RespectsCount()
    {
        await CreateTestConversation("1");
        await CreateTestConversation("2");
        await CreateTestConversation("3");

        var result = await _repository.GetRecentAsync(2);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRecentAsync_ExcludesArchived()
    {
        await CreateTestConversation("Active");
        var archived = await CreateTestConversation("Archived");
        await _repository.ArchiveAsync(archived.Id);

        var result = await _repository.GetRecentAsync(10);

        Assert.Single(result);
        Assert.Equal("Active", result[0].Title);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesProperties()
    {
        var conversation = await CreateTestConversation("Original");

        conversation.Title = "Updated";
        conversation.IsArchived = true;
        await _repository.UpdateAsync(conversation);

        var retrieved = await _repository.GetByIdAsync(conversation.Id);
        Assert.Equal("Updated", retrieved!.Title);
        Assert.True(retrieved.IsArchived);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RemovesConversation()
    {
        var conversation = await CreateTestConversation("To Delete");

        await _repository.DeleteAsync(conversation.Id);

        var retrieved = await _repository.GetByIdAsync(conversation.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_CascadeDeletesMessages()
    {
        var conversation = await CreateTestConversation("With Messages");
        var message = await AddTestMessage(conversation.Id, "Test Message");

        await _repository.DeleteAsync(conversation.Id);

        // Verify messages are also deleted
        var retrievedConversation = await _repository.GetByIdWithMessagesAsync(conversation.Id);
        Assert.Null(retrievedConversation);
    }

    #endregion

    #region ArchiveAsync Tests

    [Fact]
    public async Task ArchiveAsync_SetsIsArchivedTrue()
    {
        var conversation = await CreateTestConversation("To Archive");

        await _repository.ArchiveAsync(conversation.Id);

        var retrieved = await _repository.GetByIdAsync(conversation.Id);
        Assert.True(retrieved!.IsArchived);
    }

    #endregion

    #region AddMessageAsync Tests

    [Fact]
    public async Task AddMessageAsync_AddsMessage()
    {
        var conversation = await CreateTestConversation("Test");
        var message = new MessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = "Test message",
            Timestamp = DateTime.UtcNow,
            SequenceNumber = 1
        };

        await _repository.AddMessageAsync(conversation.Id, message);

        var result = await _repository.GetByIdWithMessagesAsync(conversation.Id);
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.Equal("Test message", result.Messages.First().Content);
    }

    [Fact]
    public async Task AddMessageAsync_IncrementsMessageCount()
    {
        var conversation = await CreateTestConversation("Test");

        await AddTestMessage(conversation.Id, "Message 1");
        await AddTestMessage(conversation.Id, "Message 2");

        var retrieved = await _repository.GetByIdAsync(conversation.Id);
        Assert.Equal(2, retrieved!.MessageCount);
    }

    [Fact]
    public async Task AddMessageAsync_SetsSequenceNumberCorrectly()
    {
        var conversation = await CreateTestConversation("Test");

        await AddTestMessage(conversation.Id, "Message 1");
        await AddTestMessage(conversation.Id, "Message 2");
        await AddTestMessage(conversation.Id, "Message 3");

        var result = await _repository.GetByIdWithMessagesAsync(conversation.Id);
        Assert.NotNull(result);
        Assert.Equal(3, result.Messages.Count);
        Assert.Equal(0, result.Messages.First().SequenceNumber);
        Assert.Equal(2, result.Messages.Last().SequenceNumber);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_FindsByTitle()
    {
        await CreateTestConversation("API Design Discussion");
        await CreateTestConversation("Bug Fix Help");
        await CreateTestConversation("Another API Topic");

        var result = await _repository.SearchAsync("API");

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Contains("API", c.Title));
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        await CreateTestConversation("Important Meeting");

        var result = await _repository.SearchAsync("important");

        Assert.Single(result);
    }

    [Fact]
    public async Task SearchAsync_ReturnsRecent_WhenQueryEmpty()
    {
        await CreateTestConversation("Test1");
        await CreateTestConversation("Test2");

        var result = await _repository.SearchAsync("");

        Assert.Equal(2, result.Count);
    }

    #endregion

    #region Helper Methods

    private async Task<ConversationEntity> CreateTestConversation(string title, DateTime? updatedAt = null)
    {
        var conversation = new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = updatedAt ?? DateTime.UtcNow
        };

        await _repository.CreateAsync(conversation);
        return conversation;
    }

    private async Task<MessageEntity> AddTestMessage(Guid conversationId, string content)
    {
        var message = new MessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = content,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = 0 // Will be set by repository
        };

        await _repository.AddMessageAsync(conversationId, message);
        return message;
    }

    #endregion
}
