using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

/// <summary>
/// In-memory conversation service implementation (no persistence).
/// Used as a fallback when database is not available.
/// </summary>
public sealed class ConversationService : IConversationService
{
    private Conversation _currentConversation = new();

    public Conversation CurrentConversation => _currentConversation;

    public bool HasUnsavedChanges => false; // In-memory, nothing to save

    public event EventHandler? ConversationChanged;
    public event EventHandler<ConversationListChangedEventArgs>? ConversationListChanged;

    public void AddMessage(ChatMessage message)
    {
        _currentConversation.Messages.Add(message);
        _currentConversation.UpdatedAt = DateTime.UtcNow;
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateMessage(Guid messageId, Action<ChatMessage> updateAction)
    {
        var message = _currentConversation.Messages.FirstOrDefault(m => m.Id == messageId);
        if (message is not null)
        {
            updateAction(message);
            _currentConversation.UpdatedAt = DateTime.UtcNow;
            ConversationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ClearConversation()
    {
        _currentConversation = new Conversation();
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<ChatMessage> GetMessages() => _currentConversation.Messages.AsReadOnly();

    public Conversation CreateNewConversation()
    {
        _currentConversation = new Conversation();
        ConversationChanged?.Invoke(this, EventArgs.Empty);
        return _currentConversation;
    }

    // ============ Persistence Methods (no-op for in-memory service) ============

    public Task<IReadOnlyList<ConversationSummary>> GetRecentConversationsAsync(int count = 50, CancellationToken ct = default)
    {
        // In-memory service has no history
        return Task.FromResult<IReadOnlyList<ConversationSummary>>(Array.Empty<ConversationSummary>());
    }

    public Task<Conversation> LoadConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        throw new NotSupportedException("In-memory conversation service does not support loading conversations.");
    }

    public Task SaveCurrentConversationAsync(CancellationToken ct = default)
    {
        // No-op for in-memory service
        return Task.CompletedTask;
    }

    public Task<Conversation> CreateNewConversationAsync(string? title = null, Guid? systemPromptId = null, CancellationToken ct = default)
    {
        var conversation = CreateNewConversation();
        if (title is not null)
        {
            conversation.Title = title;
        }
        return Task.FromResult(conversation);
    }

    public Task DeleteConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        // No-op for in-memory service
        return Task.CompletedTask;
    }

    public Task RenameConversationAsync(Guid conversationId, string newTitle, CancellationToken ct = default)
    {
        if (_currentConversation.Id == conversationId)
        {
            _currentConversation.Title = newTitle;
            ConversationChanged?.Invoke(this, EventArgs.Empty);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ConversationSummary>> SearchConversationsAsync(string query, CancellationToken ct = default)
    {
        // In-memory service has no history to search
        return Task.FromResult<IReadOnlyList<ConversationSummary>>(Array.Empty<ConversationSummary>());
    }
}
