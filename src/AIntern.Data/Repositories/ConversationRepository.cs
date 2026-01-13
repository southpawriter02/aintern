using System.Diagnostics;
using AIntern.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository implementation for managing conversations and their messages.
/// </summary>
/// <remarks>
/// <para>
/// This repository provides a clean abstraction over Entity Framework Core operations
/// for conversations and messages, with comprehensive logging support.
/// </para>
/// <para>
/// <b>Key Implementation Details:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Uses <see cref="Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteUpdateAsync{TSource}"/> for efficient single-roundtrip bulk updates</description></item>
///   <item><description>Automatically manages message sequence numbers using MAX + 1 pattern</description></item>
///   <item><description>Atomically updates MessageCount and TotalTokenCount on add/delete operations</description></item>
///   <item><description>Uses AsNoTracking() for read-only queries to improve performance</description></item>
/// </list>
/// <para>
/// <b>Logging Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Debug:</b> Entry/exit for all operations with parameters and timing</description></item>
///   <item><description><b>Warning:</b> When bulk operations affect 0 rows (may indicate missing entity)</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is not thread-safe. Each request should use its own
/// instance via dependency injection with scoped lifetime.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typical usage via dependency injection
/// public class ConversationService
/// {
///     private readonly IConversationRepository _repository;
///     
///     public ConversationService(IConversationRepository repository)
///     {
///         _repository = repository;
///     }
///     
///     public async Task&lt;ConversationEntity&gt; StartNewConversationAsync(string title)
///     {
///         var conversation = new ConversationEntity { Title = title };
///         return await _repository.CreateAsync(conversation);
///     }
/// }
/// </code>
/// </example>
public sealed class ConversationRepository : IConversationRepository
{
    #region Fields

    /// <summary>
    /// The database context for Entity Framework operations.
    /// </summary>
    private readonly AInternDbContext _context;

    /// <summary>
    /// Logger instance for diagnostic output.
    /// </summary>
    private readonly ILogger<ConversationRepository> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="ConversationRepository"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// If no logger is provided, a <see cref="NullLogger{T}"/> is used.
    /// </remarks>
    public ConversationRepository(AInternDbContext context, ILogger<ConversationRepository>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? NullLogger<ConversationRepository>.Instance;
        _logger.LogDebug("ConversationRepository instance created");
    }

    #endregion

    #region Read Operations

    /// <inheritdoc />
    public async Task<ConversationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] GetByIdAsync - ConversationId: {ConversationId}", id);

        // Use AsNoTracking() since this is a read-only query - improves performance
        // by not adding the entity to the change tracker.
        var conversation = await _context.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] GetByIdAsync - ConversationId: {ConversationId}, Found: {Found}, Duration: {DurationMs}ms",
            id, conversation != null, stopwatch.ElapsedMilliseconds);

        return conversation;
    }

    /// <inheritdoc />
    public async Task<ConversationEntity?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting conversation with messages by ID: {ConversationId}", id);

        var conversation = await _context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.SequenceNumber))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (conversation == null)
        {
            _logger.LogDebug("Conversation not found: {ConversationId}", id);
        }
        else
        {
            _logger.LogDebug(
                "Retrieved conversation {ConversationId} with {MessageCount} messages",
                id,
                conversation.Messages.Count);
        }

        return conversation;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationEntity>> GetRecentAsync(
        int skip = 0,
        int take = 20,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Getting recent conversations (skip: {Skip}, take: {Take}, includeArchived: {IncludeArchived})",
            skip, take, includeArchived);

        var query = _context.Conversations.AsNoTracking();

        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        var conversations = await query
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} recent conversations", conversations.Count);

        return conversations;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationEntity>> SearchAsync(
        string searchTerm,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Searching conversations with term: '{SearchTerm}' (skip: {Skip}, take: {Take})",
            searchTerm, skip, take);

        var conversations = await _context.Conversations
            .AsNoTracking()
            .Where(c => c.Title.Contains(searchTerm))
            .OrderByDescending(c => c.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Search returned {Count} conversations", conversations.Count);

        return conversations;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if conversation exists: {ConversationId}", id);

        var exists = await _context.Conversations
            .AnyAsync(c => c.Id == id, cancellationToken);

        _logger.LogDebug("Conversation {ConversationId} exists: {Exists}", id, exists);

        return exists;
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public async Task<ConversationEntity> CreateAsync(ConversationEntity conversation, CancellationToken cancellationToken = default)
    {
        if (conversation.Id == Guid.Empty)
        {
            conversation.Id = Guid.NewGuid();
            _logger.LogDebug("Generated new ID for conversation: {ConversationId}", conversation.Id);
        }

        _logger.LogDebug("Creating conversation: {ConversationId} with title '{Title}'", conversation.Id, conversation.Title);

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created conversation: {ConversationId}", conversation.Id);

        return conversation;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ConversationEntity conversation, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating conversation: {ConversationId}", conversation.Id);

        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated conversation: {ConversationId}", conversation.Id);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] DeleteAsync - ConversationId: {ConversationId}", id);

        // ExecuteDeleteAsync performs a bulk delete directly in the database without loading
        // the entity into memory. Messages are cascade-deleted due to FK configuration.
        var affectedRows = await _context.Conversations
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        stopwatch.Stop();

        // Log warning if nothing was deleted - may indicate the conversation doesn't exist
        if (affectedRows == 0)
        {
            _logger.LogWarning(
                "DeleteAsync affected 0 rows - conversation may not exist: {ConversationId}",
                id);
        }
        else
        {
            _logger.LogDebug(
                "[EXIT] DeleteAsync - ConversationId: {ConversationId}, AffectedRows: {AffectedRows}, Duration: {DurationMs}ms",
                id, affectedRows, stopwatch.ElapsedMilliseconds);
        }
    }

    #endregion

    #region Archive and Pin Operations

    /// <inheritdoc />
    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ENTER] ArchiveAsync - ConversationId: {ConversationId}", id);

        // ExecuteUpdateAsync performs a bulk update directly in the database.
        // This is more efficient than loading the entity, modifying, and saving.
        var affectedRows = await _context.Conversations
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.IsArchived, true)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        // Log warning if no rows affected - conversation may not exist
        if (affectedRows == 0)
        {
            _logger.LogWarning(
                "ArchiveAsync affected 0 rows - conversation may not exist: {ConversationId}",
                id);
        }
        else
        {
            _logger.LogDebug(
                "[EXIT] ArchiveAsync - ConversationId: {ConversationId}, AffectedRows: {AffectedRows}",
                id, affectedRows);
        }
    }

    /// <inheritdoc />
    public async Task UnarchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Unarchiving conversation: {ConversationId}", id);

        var affectedRows = await _context.Conversations
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.IsArchived, false)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogDebug("Unarchived conversation {ConversationId}, affected rows: {AffectedRows}", id, affectedRows);
    }

    /// <inheritdoc />
    public async Task PinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pinning conversation: {ConversationId}", id);

        var affectedRows = await _context.Conversations
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.IsPinned, true)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogDebug("Pinned conversation {ConversationId}, affected rows: {AffectedRows}", id, affectedRows);
    }

    /// <inheritdoc />
    public async Task UnpinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Unpinning conversation: {ConversationId}", id);

        var affectedRows = await _context.Conversations
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.IsPinned, false)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogDebug("Unpinned conversation {ConversationId}, affected rows: {AffectedRows}", id, affectedRows);
    }

    #endregion

    #region Message Operations

    /// <inheritdoc />
    public async Task<MessageEntity> AddMessageAsync(Guid conversationId, MessageEntity message, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] AddMessageAsync - ConversationId: {ConversationId}, Role: {Role}",
            conversationId, message.Role);

        // Assign sequence number using MAX + 1 pattern.
        // This ensures messages are ordered chronologically within a conversation.
        // Note: This pattern is safe for single-user scenarios but not suitable for
        // high-concurrency scenarios where multiple messages could be added simultaneously.
        // In such cases, consider using a database sequence or optimistic concurrency.
        var maxSequence = await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .MaxAsync(m => (int?)m.SequenceNumber, cancellationToken) ?? 0;

        message.SequenceNumber = maxSequence + 1;
        message.ConversationId = conversationId;

        if (message.Id == Guid.Empty)
        {
            message.Id = Guid.NewGuid();
        }

        _logger.LogDebug(
            "Assigning sequence number {SequenceNumber} to message {MessageId}",
            message.SequenceNumber,
            message.Id);

        _context.Messages.Add(message);

        // Atomically update conversation metadata using ExecuteUpdateAsync.
        // This is done separately from SaveChangesAsync to ensure the counts are
        // updated even if the message entity was already tracked.
        var tokenIncrement = message.TokenCount ?? 0;

        await _context.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.MessageCount, c => c.MessageCount + 1)
                .SetProperty(c => c.TotalTokenCount, c => c.TotalTokenCount + tokenIncrement)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] AddMessageAsync - MessageId: {MessageId}, ConversationId: {ConversationId}, Sequence: {SequenceNumber}, Tokens: {TokenCount}, Duration: {DurationMs}ms",
            message.Id,
            conversationId,
            message.SequenceNumber,
            message.TokenCount ?? 0,
            stopwatch.ElapsedMilliseconds);

        return message;
    }

    /// <inheritdoc />
    public async Task UpdateMessageAsync(MessageEntity message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating message: {MessageId}", message.Id);

        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        _context.Messages.Update(message);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated message: {MessageId}", message.Id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MessageEntity>> GetMessagesAsync(
        Guid conversationId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Getting messages for conversation {ConversationId} (skip: {Skip}, take: {Take})",
            conversationId, skip, take);

        var messages = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SequenceNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} messages for conversation {ConversationId}", messages.Count, conversationId);

        return messages;
    }

    /// <inheritdoc />
    public async Task<int> GetMessageCountAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting message count for conversation: {ConversationId}", conversationId);

        var count = await _context.Messages
            .CountAsync(m => m.ConversationId == conversationId, cancellationToken);

        _logger.LogDebug("Conversation {ConversationId} has {Count} messages", conversationId, count);

        return count;
    }

    /// <inheritdoc />
    public async Task DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] DeleteMessageAsync - MessageId: {MessageId}", messageId);

        // First, we need to load the message to get its ConversationId and TokenCount
        // so we can update the conversation metadata after deletion.
        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "DeleteMessageAsync - Message not found: {MessageId}, Duration: {DurationMs}ms",
                messageId, stopwatch.ElapsedMilliseconds);
            return;
        }

        var tokenDecrement = message.TokenCount ?? 0;
        var conversationId = message.ConversationId;

        _context.Messages.Remove(message);

        // Update conversation counts
        await _context.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.MessageCount, c => c.MessageCount - 1)
                .SetProperty(c => c.TotalTokenCount, c => c.TotalTokenCount - tokenDecrement)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] DeleteMessageAsync - MessageId: {MessageId}, ConversationId: {ConversationId}, TokensRemoved: {TokensRemoved}, Duration: {DurationMs}ms",
            messageId, conversationId, tokenDecrement, stopwatch.ElapsedMilliseconds);
    }

    #endregion
}
