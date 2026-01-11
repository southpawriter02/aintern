using AIntern.Core.Entities;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Repository interface for conversation data access operations.
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Gets a conversation by ID without messages.
    /// </summary>
    Task<ConversationEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a conversation by ID with all messages loaded.
    /// </summary>
    Task<ConversationEntity?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent conversations, ordered by UpdatedAt descending.
    /// </summary>
    Task<IReadOnlyList<ConversationEntity>> GetRecentAsync(int count = 50, CancellationToken ct = default);

    /// <summary>
    /// Searches conversations by title.
    /// </summary>
    Task<IReadOnlyList<ConversationEntity>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    Task<ConversationEntity> CreateAsync(ConversationEntity conversation, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing conversation.
    /// </summary>
    Task UpdateAsync(ConversationEntity conversation, CancellationToken ct = default);

    /// <summary>
    /// Deletes a conversation and all its messages.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Archives a conversation (soft delete).
    /// </summary>
    Task ArchiveAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Adds a message to a conversation.
    /// </summary>
    Task AddMessageAsync(Guid conversationId, MessageEntity message, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing message.
    /// </summary>
    Task UpdateMessageAsync(MessageEntity message, CancellationToken ct = default);
}
