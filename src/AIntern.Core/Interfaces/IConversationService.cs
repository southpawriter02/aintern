using AIntern.Core.Events;
using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

public interface IConversationService
{
    /// <summary>
    /// Gets the current active conversation.
    /// </summary>
    Conversation CurrentConversation { get; }

    /// <summary>
    /// Gets whether the current conversation has unsaved changes.
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    /// Adds a message to the current conversation.
    /// </summary>
    void AddMessage(ChatMessage message);

    /// <summary>
    /// Updates an existing message in the current conversation.
    /// </summary>
    void UpdateMessage(Guid messageId, Action<ChatMessage> updateAction);

    /// <summary>
    /// Clears all messages from the current conversation.
    /// </summary>
    void ClearConversation();

    /// <summary>
    /// Gets all messages in the current conversation.
    /// </summary>
    IEnumerable<ChatMessage> GetMessages();

    /// <summary>
    /// Creates a new conversation and sets it as current.
    /// </summary>
    Conversation CreateNewConversation();

    /// <summary>
    /// Raised when the conversation changes.
    /// </summary>
    event EventHandler? ConversationChanged;

    // ============ Persistence Methods ============

    /// <summary>
    /// Gets recent conversations for display in the conversation list.
    /// </summary>
    /// <param name="count">Maximum number of conversations to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of conversation summaries ordered by last update time.</returns>
    Task<IReadOnlyList<ConversationSummary>> GetRecentConversationsAsync(int count = 50, CancellationToken ct = default);

    /// <summary>
    /// Loads a conversation by ID and sets it as current.
    /// </summary>
    /// <param name="conversationId">The conversation ID to load.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The loaded conversation.</returns>
    Task<Conversation> LoadConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Saves the current conversation to persistent storage.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task SaveCurrentConversationAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new conversation with optional parameters and sets it as current.
    /// </summary>
    /// <param name="title">Optional title for the conversation.</param>
    /// <param name="systemPromptId">Optional system prompt ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created conversation.</returns>
    Task<Conversation> CreateNewConversationAsync(string? title = null, Guid? systemPromptId = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes a conversation from persistent storage.
    /// </summary>
    /// <param name="conversationId">The conversation ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Renames a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to rename.</param>
    /// <param name="newTitle">The new title.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RenameConversationAsync(Guid conversationId, string newTitle, CancellationToken ct = default);

    /// <summary>
    /// Searches conversations by title or content.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching conversation summaries.</returns>
    Task<IReadOnlyList<ConversationSummary>> SearchConversationsAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Raised when the conversation list changes (add, remove, update).
    /// </summary>
    event EventHandler<ConversationListChangedEventArgs>? ConversationListChanged;
}
