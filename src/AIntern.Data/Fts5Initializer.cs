using Microsoft.EntityFrameworkCore;

namespace AIntern.Data;

/// <summary>
/// Handles FTS5 virtual table creation and trigger setup.
/// Adapted for Guid primary keys - stores EntityId as TEXT for joining.
/// </summary>
public static class Fts5Initializer
{
    /// <summary>
    /// Initializes FTS5 virtual tables and triggers for full-text search.
    /// Safe to call multiple times - uses IF NOT EXISTS.
    /// </summary>
    public static async Task InitializeAsync(AInternDbContext context, CancellationToken ct = default)
    {
        await CreateFts5TablesAsync(context, ct);
        await CreateTriggersAsync(context, ct);
        await PopulateExistingDataAsync(context, ct);
    }

    private static async Task CreateFts5TablesAsync(AInternDbContext context, CancellationToken ct)
    {
        // ConversationsFts: stores EntityId (Guid as TEXT) + Title
        await context.Database.ExecuteSqlRawAsync("""
            CREATE VIRTUAL TABLE IF NOT EXISTS ConversationsFts USING fts5(
                EntityId UNINDEXED,
                Title,
                tokenize='porter unicode61'
            );
            """, ct);

        // MessagesFts: stores EntityId (Guid as TEXT) + Content
        await context.Database.ExecuteSqlRawAsync("""
            CREATE VIRTUAL TABLE IF NOT EXISTS MessagesFts USING fts5(
                EntityId UNINDEXED,
                Content,
                tokenize='porter unicode61'
            );
            """, ct);
    }

    private static async Task CreateTriggersAsync(AInternDbContext context, CancellationToken ct)
    {
        // Conversations INSERT trigger
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER IF NOT EXISTS Conversations_ai AFTER INSERT ON Conversations BEGIN
                INSERT INTO ConversationsFts(EntityId, Title) VALUES (new.Id, new.Title);
            END;
            """, ct);

        // Conversations DELETE trigger
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER IF NOT EXISTS Conversations_ad AFTER DELETE ON Conversations BEGIN
                DELETE FROM ConversationsFts WHERE EntityId = old.Id;
            END;
            """, ct);

        // Conversations UPDATE trigger
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER IF NOT EXISTS Conversations_au AFTER UPDATE ON Conversations BEGIN
                DELETE FROM ConversationsFts WHERE EntityId = old.Id;
                INSERT INTO ConversationsFts(EntityId, Title) VALUES (new.Id, new.Title);
            END;
            """, ct);

        // Messages INSERT trigger
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER IF NOT EXISTS Messages_ai AFTER INSERT ON Messages BEGIN
                INSERT INTO MessagesFts(EntityId, Content) VALUES (new.Id, new.Content);
            END;
            """, ct);

        // Messages DELETE trigger
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER IF NOT EXISTS Messages_ad AFTER DELETE ON Messages BEGIN
                DELETE FROM MessagesFts WHERE EntityId = old.Id;
            END;
            """, ct);

        // Messages UPDATE trigger
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER IF NOT EXISTS Messages_au AFTER UPDATE ON Messages BEGIN
                DELETE FROM MessagesFts WHERE EntityId = old.Id;
                INSERT INTO MessagesFts(EntityId, Content) VALUES (new.Id, new.Content);
            END;
            """, ct);
    }

    private static async Task PopulateExistingDataAsync(AInternDbContext context, CancellationToken ct)
    {
        // Populate ConversationsFts with existing data (only if empty)
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO ConversationsFts(EntityId, Title)
            SELECT Id, Title FROM Conversations
            WHERE NOT EXISTS (SELECT 1 FROM ConversationsFts LIMIT 1);
            """, ct);

        // Populate MessagesFts with existing data (only if empty)
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO MessagesFts(EntityId, Content)
            SELECT Id, Content FROM Messages
            WHERE NOT EXISTS (SELECT 1 FROM MessagesFts LIMIT 1);
            """, ct);
    }
}
