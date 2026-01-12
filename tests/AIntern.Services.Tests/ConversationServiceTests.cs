using Xunit;
using AIntern.Core.Models;
using AIntern.Services;

namespace AIntern.Services.Tests;

/// <summary>
/// Unit tests for ConversationService (in-memory implementation).
/// </summary>
public class ConversationServiceTests
{
    private readonly ConversationService _service;

    public ConversationServiceTests()
    {
        _service = new ConversationService();
    }

    #region CurrentConversation Tests

    [Fact]
    public void CurrentConversation_ReturnsDefaultConversation_Initially()
    {
        Assert.NotNull(_service.CurrentConversation);
        Assert.Empty(_service.CurrentConversation.Messages);
    }

    #endregion

    #region HasUnsavedChanges Tests

    [Fact]
    public void HasUnsavedChanges_AlwaysFalse_ForInMemoryService()
    {
        Assert.False(_service.HasUnsavedChanges);

        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "Test" });

        Assert.False(_service.HasUnsavedChanges);
    }

    #endregion

    #region AddMessage Tests

    [Fact]
    public void AddMessage_AddsMessageToConversation()
    {
        var message = new ChatMessage
        {
            Role = MessageRole.User,
            Content = "Hello, world!"
        };

        _service.AddMessage(message);

        Assert.Single(_service.CurrentConversation.Messages);
        Assert.Equal("Hello, world!", _service.CurrentConversation.Messages[0].Content);
    }

    [Fact]
    public void AddMessage_UpdatesConversationTimestamp()
    {
        var originalTime = _service.CurrentConversation.UpdatedAt;

        Thread.Sleep(10);
        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "Test" });

        Assert.True(_service.CurrentConversation.UpdatedAt >= originalTime);
    }

    [Fact]
    public void AddMessage_RaisesConversationChangedEvent()
    {
        var eventRaised = false;
        _service.ConversationChanged += (_, _) => eventRaised = true;

        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "Test" });

        Assert.True(eventRaised);
    }

    [Fact]
    public void AddMessage_PreservesMessageOrder()
    {
        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "First" });
        _service.AddMessage(new ChatMessage { Role = MessageRole.Assistant, Content = "Second" });
        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "Third" });

        var messages = _service.GetMessages().ToList();
        Assert.Equal("First", messages[0].Content);
        Assert.Equal("Second", messages[1].Content);
        Assert.Equal("Third", messages[2].Content);
    }

    #endregion

    #region UpdateMessage Tests

    [Fact]
    public void UpdateMessage_UpdatesExistingMessage()
    {
        var message = new ChatMessage { Role = MessageRole.User, Content = "Original" };
        _service.AddMessage(message);

        _service.UpdateMessage(message.Id, m => m.Content = "Updated");

        Assert.Equal("Updated", _service.CurrentConversation.Messages[0].Content);
    }

    [Fact]
    public void UpdateMessage_RaisesConversationChangedEvent()
    {
        var message = new ChatMessage { Role = MessageRole.User, Content = "Original" };
        _service.AddMessage(message);

        var eventRaised = false;
        _service.ConversationChanged += (_, _) => eventRaised = true;

        _service.UpdateMessage(message.Id, m => m.Content = "Updated");

        Assert.True(eventRaised);
    }

    [Fact]
    public void UpdateMessage_DoesNothing_WhenMessageNotFound()
    {
        var eventRaised = false;
        _service.ConversationChanged += (_, _) => eventRaised = true;

        _service.UpdateMessage(Guid.NewGuid(), m => m.Content = "Updated");

        Assert.False(eventRaised);
    }

    #endregion

    #region ClearConversation Tests

    [Fact]
    public void ClearConversation_RemovesAllMessages()
    {
        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "Message 1" });
        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "Message 2" });

        _service.ClearConversation();

        Assert.Empty(_service.CurrentConversation.Messages);
    }

    [Fact]
    public void ClearConversation_CreatesNewConversation()
    {
        var originalId = _service.CurrentConversation.Id;
        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "Test" });

        _service.ClearConversation();

        Assert.NotEqual(originalId, _service.CurrentConversation.Id);
    }

    [Fact]
    public void ClearConversation_RaisesConversationChangedEvent()
    {
        var eventRaised = false;
        _service.ConversationChanged += (_, _) => eventRaised = true;

        _service.ClearConversation();

        Assert.True(eventRaised);
    }

    #endregion

    #region GetMessages Tests

    [Fact]
    public void GetMessages_ReturnsAllMessages()
    {
        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "1" });
        _service.AddMessage(new ChatMessage { Role = MessageRole.Assistant, Content = "2" });
        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "3" });

        var messages = _service.GetMessages().ToList();

        Assert.Equal(3, messages.Count);
    }

    [Fact]
    public void GetMessages_ReturnsEmpty_WhenNoMessages()
    {
        var messages = _service.GetMessages();

        Assert.Empty(messages);
    }

    #endregion

    #region CreateNewConversation Tests

    [Fact]
    public void CreateNewConversation_ReturnsNewConversation()
    {
        var originalId = _service.CurrentConversation.Id;

        var newConversation = _service.CreateNewConversation();

        Assert.NotEqual(originalId, newConversation.Id);
    }

    [Fact]
    public void CreateNewConversation_SetsCurrentConversation()
    {
        var newConversation = _service.CreateNewConversation();

        Assert.Equal(newConversation.Id, _service.CurrentConversation.Id);
    }

    [Fact]
    public void CreateNewConversation_HasEmptyMessages()
    {
        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "Test" });

        var newConversation = _service.CreateNewConversation();

        Assert.Empty(newConversation.Messages);
    }

    [Fact]
    public void CreateNewConversation_RaisesConversationChangedEvent()
    {
        var eventRaised = false;
        _service.ConversationChanged += (_, _) => eventRaised = true;

        _service.CreateNewConversation();

        Assert.True(eventRaised);
    }

    #endregion

    #region Async Persistence Methods (No-op for In-Memory)

    [Fact]
    public async Task GetRecentConversationsAsync_ReturnsEmpty()
    {
        var result = await _service.GetRecentConversationsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadConversationAsync_ThrowsNotSupported()
    {
        await Assert.ThrowsAsync<NotSupportedException>(
            () => _service.LoadConversationAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SaveCurrentConversationAsync_CompletesWithoutError()
    {
        _service.AddMessage(new ChatMessage { Role = MessageRole.User, Content = "Test" });

        // Should complete without throwing
        await _service.SaveCurrentConversationAsync();
    }

    [Fact]
    public async Task CreateNewConversationAsync_CreatesNewConversation()
    {
        var result = await _service.CreateNewConversationAsync("Test Title");

        Assert.NotNull(result);
        Assert.Equal("Test Title", result.Title);
    }

    [Fact]
    public async Task CreateNewConversationAsync_UsesDefaultTitle_WhenNotSpecified()
    {
        var result = await _service.CreateNewConversationAsync();

        Assert.Equal("New Conversation", result.Title);
    }

    [Fact]
    public async Task DeleteConversationAsync_CompletesWithoutError()
    {
        // Should complete without throwing (no-op for in-memory)
        await _service.DeleteConversationAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task RenameConversationAsync_RenamesCurrentConversation()
    {
        var conversationId = _service.CurrentConversation.Id;

        await _service.RenameConversationAsync(conversationId, "New Title");

        Assert.Equal("New Title", _service.CurrentConversation.Title);
    }

    [Fact]
    public async Task RenameConversationAsync_DoesNothing_ForOtherConversation()
    {
        var originalTitle = _service.CurrentConversation.Title;

        await _service.RenameConversationAsync(Guid.NewGuid(), "New Title");

        Assert.Equal(originalTitle, _service.CurrentConversation.Title);
    }

    [Fact]
    public async Task SearchConversationsAsync_ReturnsEmpty()
    {
        var result = await _service.SearchConversationsAsync("test");

        Assert.Empty(result);
    }

    #endregion
}
