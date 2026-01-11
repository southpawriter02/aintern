using Microsoft.EntityFrameworkCore;
using AIntern.Core.Entities;
using AIntern.Core.Interfaces;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository implementation for conversation data access operations.
/// </summary>
public sealed class ConversationRepository : IConversationRepository
{
    private readonly IDbContextFactory<AInternDbContext> _contextFactory;

    public ConversationRepository(IDbContextFactory<AInternDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<ConversationEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<ConversationEntity?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Conversations
            .AsNoTracking()
            .Include(c => c.Messages.OrderBy(m => m.SequenceNumber))
            .Include(c => c.SystemPrompt)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<ConversationEntity>> GetRecentAsync(int count = 50, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Conversations
            .AsNoTracking()
            .Where(c => !c.IsArchived)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ConversationEntity>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetRecentAsync(ct: ct);

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var normalizedQuery = query.ToLowerInvariant();

        return await context.Conversations
            .AsNoTracking()
            .Where(c => !c.IsArchived && c.Title.ToLower().Contains(normalizedQuery))
            .OrderByDescending(c => c.UpdatedAt)
            .Take(50)
            .ToListAsync(ct);
    }

    public async Task<ConversationEntity> CreateAsync(ConversationEntity conversation, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task UpdateAsync(ConversationEntity conversation, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.Conversations.Update(conversation);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var conversation = await context.Conversations.FindAsync(new object[] { id }, ct);
        if (conversation is not null)
        {
            context.Conversations.Remove(conversation);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var conversation = await context.Conversations.FindAsync(new object[] { id }, ct);
        if (conversation is not null)
        {
            conversation.IsArchived = true;
            conversation.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task AddMessageAsync(Guid conversationId, MessageEntity message, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        message.ConversationId = conversationId;

        // Get the next sequence number
        var maxSequence = await context.Messages
            .Where(m => m.ConversationId == conversationId)
            .MaxAsync(m => (int?)m.SequenceNumber, ct) ?? -1;

        message.SequenceNumber = maxSequence + 1;

        context.Messages.Add(message);

        // Update conversation's message count and timestamp
        var conversation = await context.Conversations.FindAsync(new object[] { conversationId }, ct);
        if (conversation is not null)
        {
            conversation.MessageCount++;
            conversation.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateMessageAsync(MessageEntity message, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.Messages.Update(message);
        await context.SaveChangesAsync(ct);
    }
}
