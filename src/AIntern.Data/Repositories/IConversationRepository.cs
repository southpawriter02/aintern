using AIntern.Core.Entities;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository interface for managing conversations and their messages.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a complete abstraction over Entity Framework Core operations
/// for the <see cref="ConversationEntity"/> and <see cref="MessageEntity"/> types.
/// </para>
/// <para>
/// Key features include:
/// </para>
/// <list type="bullet">
///   <item><description>CRUD operations for conversations</description></item>
///   <item><description>Archive and pin flag management</description></item>
///   <item><description>Message management within conversations</description></item>
///   <item><description>Search and filtering capabilities</description></item>
/// </list>
/// </remarks>
/// <example>
/// Basic usage with dependency injection:
/// <code>
/// public class ConversationService
/// {
///     private readonly IConversationRepository _repository;
///
///     public ConversationService(IConversationRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;ConversationEntity?&gt; GetConversationAsync(Guid id)
///     {
///         return await _repository.GetByIdAsync(id);
///     }
/// }
/// </code>
/// </example>
public interface IConversationRepository
{
    #region Read Operations

    /// <summary>
    /// Retrieves a conversation by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The conversation entity if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method returns the conversation without its messages.
    /// Use <see cref="GetByIdWithMessagesAsync"/> to include messages.
    /// </remarks>
    Task<ConversationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a conversation by its unique identifier, including all messages.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The conversation entity with messages loaded if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Messages are ordered by <see cref="MessageEntity.SequenceNumber"/> ascending.
    /// </remarks>
    Task<ConversationEntity?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves recent conversations with pagination support.
    /// </summary>
    /// <param name="skip">The number of conversations to skip (default: 0).</param>
    /// <param name="take">The maximum number of conversations to return (default: 20).</param>
    /// <param name="includeArchived">Whether to include archived conversations (default: false).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of conversations ordered by pinned status (descending) then by
    /// <see cref="ConversationEntity.UpdatedAt"/> (descending).
    /// </returns>
    /// <remarks>
    /// <para>
    /// The default behavior excludes archived conversations. Set <paramref name="includeArchived"/>
    /// to <c>true</c> to include them.
    /// </para>
    /// <para>
    /// Pinned conversations always appear before unpinned conversations.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<ConversationEntity>> GetRecentAsync(
        int skip = 0,
        int take = 20,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches conversations by title.
    /// </summary>
    /// <param name="searchTerm">The search term to match against conversation titles.</param>
    /// <param name="skip">The number of results to skip (default: 0).</param>
    /// <param name="take">The maximum number of results to return (default: 20).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of conversations whose titles contain the search term (case-insensitive).
    /// </returns>
    /// <remarks>
    /// The search is case-insensitive and matches any substring of the title.
    /// </remarks>
    Task<IReadOnlyList<ConversationEntity>> SearchAsync(
        string searchTerm,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a conversation exists.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the conversation exists; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    /// <param name="conversation">The conversation entity to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created conversation with any generated values populated.</returns>
    /// <remarks>
    /// <para>
    /// If <see cref="ConversationEntity.Id"/> is <see cref="Guid.Empty"/>,
    /// a new GUID will be generated.
    /// </para>
    /// <para>
    /// <see cref="ConversationEntity.CreatedAt"/> and <see cref="ConversationEntity.UpdatedAt"/>
    /// are automatically set by the DbContext.
    /// </para>
    /// </remarks>
    Task<ConversationEntity> CreateAsync(ConversationEntity conversation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing conversation.
    /// </summary>
    /// <param name="conversation">The conversation entity with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <see cref="ConversationEntity.UpdatedAt"/> is automatically updated by the DbContext.
    /// </remarks>
    Task UpdateAsync(ConversationEntity conversation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation and all its messages.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This operation cascades to delete all messages associated with the conversation.
    /// </remarks>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Archive and Pin Operations

    /// <summary>
    /// Archives a conversation.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation to archive.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Archived conversations are excluded from <see cref="GetRecentAsync"/> by default.
    /// </remarks>
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unarchives a conversation.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation to unarchive.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnarchiveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pins a conversation.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation to pin.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Pinned conversations appear at the top of the list in <see cref="GetRecentAsync"/>.
    /// </remarks>
    Task PinAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpins a conversation.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation to unpin.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnpinAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Message Operations

    /// <summary>
    /// Adds a message to a conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="message">The message entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created message with any generated values populated.</returns>
    /// <remarks>
    /// <para>
    /// The <see cref="MessageEntity.SequenceNumber"/> is automatically assigned based on
    /// the current maximum sequence number in the conversation plus one.
    /// </para>
    /// <para>
    /// The conversation's <see cref="ConversationEntity.MessageCount"/> is automatically
    /// incremented, and <see cref="ConversationEntity.TotalTokenCount"/> is updated if
    /// <see cref="MessageEntity.TokenCount"/> is provided.
    /// </para>
    /// </remarks>
    Task<MessageEntity> AddMessageAsync(Guid conversationId, MessageEntity message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing message.
    /// </summary>
    /// <param name="message">The message entity with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method sets <see cref="MessageEntity.IsEdited"/> to <c>true</c> and
    /// <see cref="MessageEntity.EditedAt"/> to the current UTC time.
    /// </remarks>
    Task UpdateMessageAsync(MessageEntity message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves messages for a conversation with pagination support.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="skip">The number of messages to skip (default: 0).</param>
    /// <param name="take">The maximum number of messages to return (default: 50).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of messages ordered by <see cref="MessageEntity.SequenceNumber"/> ascending.
    /// </returns>
    Task<IReadOnlyList<MessageEntity>> GetMessagesAsync(
        Guid conversationId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of messages in a conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The total number of messages in the conversation.</returns>
    Task<int> GetMessageCountAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves messages for a conversation using 1-indexed page-based pagination.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="pageNumber">The 1-indexed page number to retrieve.</param>
    /// <param name="pageSize">The number of messages per page (default: 50).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of messages for the specified page, ordered by
    /// <see cref="MessageEntity.SequenceNumber"/> ascending.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses 1-indexed pages (page 1 = first page, page 2 = second page, etc.)
    /// as opposed to <see cref="GetMessagesAsync"/> which uses 0-based skip/take.
    /// </para>
    /// <para>
    /// Messages are retrieved in reverse order from the database (newest first) then
    /// sorted for display (oldest first). For example, with 100 messages and pageSize=50:
    /// <list type="bullet">
    ///   <item>Page 1: Messages 51-100 (most recent)</item>
    ///   <item>Page 2: Messages 1-50 (older)</item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<MessageEntity>> GetMessagesPagedAsync(
        Guid conversationId,
        int pageNumber,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a message from a conversation.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// The conversation's <see cref="ConversationEntity.MessageCount"/> is automatically
    /// decremented, and <see cref="ConversationEntity.TotalTokenCount"/> is updated if
    /// the message had a token count.
    /// </remarks>
    Task DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

    #endregion
}
