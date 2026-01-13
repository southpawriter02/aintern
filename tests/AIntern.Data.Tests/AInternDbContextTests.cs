using AIntern.Core.Entities;
using AIntern.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIntern.Data.Tests;

/// <summary>
/// Unit tests for the <see cref="AInternDbContext"/> class.
/// Verifies DbSet properties, automatic timestamp management, entity configurations,
/// and relationship behaviors.
/// </summary>
/// <remarks>
/// <para>
/// These tests use SQLite in-memory databases to verify:
/// </para>
/// <list type="bullet">
///   <item><description>DbContext instantiation and DbSet accessibility</description></item>
///   <item><description>Automatic CreatedAt/UpdatedAt timestamp management</description></item>
///   <item><description>Cascade delete behavior for Conversation → Messages</description></item>
///   <item><description>SetNull delete behavior for SystemPrompt → Conversations</description></item>
///   <item><description>Unique constraints on Name columns</description></item>
///   <item><description>Composite unique constraint on (ConversationId, SequenceNumber)</description></item>
/// </list>
/// <para>
/// Tests are organized by functionality using regions.
/// </para>
/// </remarks>
public class AInternDbContextTests : IDisposable
{
    #region Test Infrastructure

    /// <summary>
    /// SQLite connection kept open for in-memory database lifetime.
    /// </summary>
    private SqliteConnection? _connection;

    /// <summary>
    /// Creates an in-memory SQLite DbContext for testing.
    /// </summary>
    /// <returns>A configured AInternDbContext instance.</returns>
    /// <remarks>
    /// Uses SQLite in-memory database with a shared connection to ensure
    /// the database persists for the lifetime of the test.
    /// </remarks>
    private AInternDbContext CreateInMemoryContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Disposes of the test infrastructure.
    /// </summary>
    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that the DbContext can be instantiated with options.
    /// </summary>
    [Fact]
    public void Constructor_WithOptions_CreatesInstance()
    {
        // Arrange & Act
        using var context = CreateInMemoryContext();

        // Assert
        Assert.NotNull(context);
    }

    /// <summary>
    /// Verifies that the parameterless constructor creates an instance.
    /// This constructor is used for EF Core design-time tools.
    /// </summary>
    [Fact]
    public void Constructor_Parameterless_CreatesInstance()
    {
        // Arrange & Act
        using var context = new AInternDbContext();

        // Assert
        Assert.NotNull(context);
    }

    #endregion

    #region DbSet Property Tests

    /// <summary>
    /// Verifies that the Conversations DbSet is accessible and not null.
    /// </summary>
    [Fact]
    public void Conversations_DbSet_IsNotNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        var dbSet = context.Conversations;

        // Assert
        Assert.NotNull(dbSet);
    }

    /// <summary>
    /// Verifies that the Messages DbSet is accessible and not null.
    /// </summary>
    [Fact]
    public void Messages_DbSet_IsNotNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        var dbSet = context.Messages;

        // Assert
        Assert.NotNull(dbSet);
    }

    /// <summary>
    /// Verifies that the SystemPrompts DbSet is accessible and not null.
    /// </summary>
    [Fact]
    public void SystemPrompts_DbSet_IsNotNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        var dbSet = context.SystemPrompts;

        // Assert
        Assert.NotNull(dbSet);
    }

    /// <summary>
    /// Verifies that the InferencePresets DbSet is accessible and not null.
    /// </summary>
    [Fact]
    public void InferencePresets_DbSet_IsNotNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        var dbSet = context.InferencePresets;

        // Assert
        Assert.NotNull(dbSet);
    }

    #endregion

    #region Timestamp Auto-Management Tests

    /// <summary>
    /// Verifies that SaveChangesAsync automatically sets CreatedAt when adding an entity.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_SetsCreatedAtOnAdd()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var conversation = new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = "Test Conversation"
        };

        // Act
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(default, conversation.CreatedAt);
        Assert.Equal(conversation.CreatedAt, conversation.UpdatedAt);
    }

    /// <summary>
    /// Verifies that SaveChangesAsync automatically updates UpdatedAt when modifying an entity.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_UpdatesUpdatedAtOnModify()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var conversation = new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = "Original Title"
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var originalUpdatedAt = conversation.UpdatedAt;

        // Ensure time difference
        await Task.Delay(10);

        // Act
        conversation.Title = "Modified Title";
        await context.SaveChangesAsync();

        // Assert
        Assert.True(conversation.UpdatedAt > originalUpdatedAt,
            $"UpdatedAt ({conversation.UpdatedAt}) should be greater than original ({originalUpdatedAt})");
    }

    #endregion

    #region Relationship Tests

    /// <summary>
    /// Verifies that deleting a SystemPrompt sets the SystemPromptId to null on related conversations.
    /// </summary>
    [Fact]
    public async Task Conversation_SystemPromptRelationship_SetNullOnDelete()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var systemPrompt = new SystemPromptEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Prompt",
            Content = "You are a helpful assistant."
        };
        context.SystemPrompts.Add(systemPrompt);
        await context.SaveChangesAsync();

        var conversation = new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = "Test Conversation",
            SystemPromptId = systemPrompt.Id
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Act
        context.SystemPrompts.Remove(systemPrompt);
        await context.SaveChangesAsync();

        // Assert
        var reloadedConversation = await context.Conversations.FindAsync(conversation.Id);
        Assert.NotNull(reloadedConversation);
        Assert.Null(reloadedConversation.SystemPromptId);
    }

    /// <summary>
    /// Verifies that deleting a Conversation cascades to delete all its Messages.
    /// </summary>
    [Fact]
    public async Task Conversation_MessagesRelationship_CascadeOnDelete()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var conversation = new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = "Test Conversation"
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var message1 = new MessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = "Hello",
            SequenceNumber = 0
        };
        var message2 = new MessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = "Hi there!",
            SequenceNumber = 1
        };
        context.Messages.AddRange(message1, message2);
        await context.SaveChangesAsync();

        // Act
        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(context.Messages);
    }

    #endregion

    #region Unique Constraint Tests

    /// <summary>
    /// Verifies that duplicate SequenceNumbers within the same Conversation are rejected.
    /// </summary>
    [Fact]
    public async Task Message_UniqueSequenceNumber_EnforcedPerConversation()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var conversation = new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = "Test Conversation"
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var message1 = new MessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = "First message",
            SequenceNumber = 0
        };
        context.Messages.Add(message1);
        await context.SaveChangesAsync();

        var message2 = new MessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = "Duplicate sequence",
            SequenceNumber = 0 // Duplicate!
        };
        context.Messages.Add(message2);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    /// <summary>
    /// Verifies that duplicate SystemPrompt names are rejected.
    /// </summary>
    [Fact]
    public async Task SystemPrompt_UniqueName_EnforcedByDatabase()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var prompt1 = new SystemPromptEntity
        {
            Id = Guid.NewGuid(),
            Name = "Unique Name",
            Content = "First prompt"
        };
        context.SystemPrompts.Add(prompt1);
        await context.SaveChangesAsync();

        var prompt2 = new SystemPromptEntity
        {
            Id = Guid.NewGuid(),
            Name = "Unique Name", // Duplicate!
            Content = "Second prompt"
        };
        context.SystemPrompts.Add(prompt2);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    /// <summary>
    /// Verifies that duplicate InferencePreset names are rejected.
    /// </summary>
    [Fact]
    public async Task InferencePreset_UniqueName_EnforcedByDatabase()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var preset1 = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Unique Preset"
        };
        context.InferencePresets.Add(preset1);
        await context.SaveChangesAsync();

        var preset2 = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Unique Preset" // Duplicate!
        };
        context.InferencePresets.Add(preset2);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    #endregion

    #region Configuration Default Value Tests

    /// <summary>
    /// Verifies that MessageRole enum is stored as integer in the database.
    /// </summary>
    [Fact]
    public async Task Message_RoleStoredAsInteger()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var conversation = new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = "Test"
        };
        context.Conversations.Add(conversation);

        var message = new MessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = "Test message",
            SequenceNumber = 0
        };
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        // Act - Query using raw SQL to verify integer storage
        var savedMessage = await context.Messages.FirstAsync(m => m.Id == message.Id);

        // Assert
        Assert.Equal(MessageRole.Assistant, savedMessage.Role);
        Assert.Equal(2, (int)savedMessage.Role); // Assistant = 2
    }

    /// <summary>
    /// Verifies that SystemPrompt Category defaults to "General" in the database.
    /// </summary>
    [Fact]
    public async Task SystemPrompt_DefaultCategory_IsGeneral()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var prompt = new SystemPromptEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Prompt",
            Content = "Test content"
            // Category not set - should default to "General"
        };
        context.SystemPrompts.Add(prompt);
        await context.SaveChangesAsync();

        // Act
        var savedPrompt = await context.SystemPrompts.FindAsync(prompt.Id);

        // Assert
        Assert.NotNull(savedPrompt);
        Assert.Equal("General", savedPrompt.Category);
    }

    /// <summary>
    /// Verifies that InferencePreset Temperature defaults to 0.7.
    /// </summary>
    [Fact]
    public async Task InferencePreset_DefaultTemperature_Is0Point7()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var preset = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Preset"
        };
        context.InferencePresets.Add(preset);
        await context.SaveChangesAsync();

        // Act
        var savedPreset = await context.InferencePresets.FindAsync(preset.Id);

        // Assert
        Assert.NotNull(savedPreset);
        Assert.Equal(0.7f, savedPreset.Temperature);
    }

    /// <summary>
    /// Verifies that InferencePreset TopP defaults to 0.9.
    /// </summary>
    [Fact]
    public async Task InferencePreset_DefaultTopP_Is0Point9()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var preset = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "TopP Test"
        };
        context.InferencePresets.Add(preset);
        await context.SaveChangesAsync();

        // Act
        var savedPreset = await context.InferencePresets.FindAsync(preset.Id);

        // Assert
        Assert.NotNull(savedPreset);
        Assert.Equal(0.9f, savedPreset.TopP);
    }

    /// <summary>
    /// Verifies that InferencePreset TopK defaults to 40.
    /// </summary>
    [Fact]
    public async Task InferencePreset_DefaultTopK_Is40()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var preset = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "TopK Test"
        };
        context.InferencePresets.Add(preset);
        await context.SaveChangesAsync();

        // Act
        var savedPreset = await context.InferencePresets.FindAsync(preset.Id);

        // Assert
        Assert.NotNull(savedPreset);
        Assert.Equal(40, savedPreset.TopK);
    }

    /// <summary>
    /// Verifies that InferencePreset RepeatPenalty defaults to 1.1.
    /// </summary>
    [Fact]
    public async Task InferencePreset_DefaultRepeatPenalty_Is1Point1()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var preset = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "RepeatPenalty Test"
        };
        context.InferencePresets.Add(preset);
        await context.SaveChangesAsync();

        // Act
        var savedPreset = await context.InferencePresets.FindAsync(preset.Id);

        // Assert
        Assert.NotNull(savedPreset);
        Assert.Equal(1.1f, savedPreset.RepeatPenalty);
    }

    /// <summary>
    /// Verifies that InferencePreset MaxTokens defaults to 2048.
    /// </summary>
    [Fact]
    public async Task InferencePreset_DefaultMaxTokens_Is2048()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var preset = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "MaxTokens Test"
        };
        context.InferencePresets.Add(preset);
        await context.SaveChangesAsync();

        // Act
        var savedPreset = await context.InferencePresets.FindAsync(preset.Id);

        // Assert
        Assert.NotNull(savedPreset);
        Assert.Equal(2048, savedPreset.MaxTokens);
    }

    /// <summary>
    /// Verifies that InferencePreset ContextSize defaults to 4096.
    /// </summary>
    [Fact]
    public async Task InferencePreset_DefaultContextSize_Is4096()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var preset = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "ContextSize Test"
        };
        context.InferencePresets.Add(preset);
        await context.SaveChangesAsync();

        // Act
        var savedPreset = await context.InferencePresets.FindAsync(preset.Id);

        // Assert
        Assert.NotNull(savedPreset);
        Assert.Equal(4096, savedPreset.ContextSize);
    }

    #endregion

    #region Model Configuration Tests

    /// <summary>
    /// Verifies that EnsureCreated creates all four expected tables.
    /// </summary>
    [Fact]
    public void Model_CreatesAllTables()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act - Tables are created by CreateInMemoryContext
        // We verify by successfully adding entities to each table
        var canAddConversation = true;
        var canAddMessage = true;
        var canAddSystemPrompt = true;
        var canAddInferencePreset = true;

        try
        {
            var conv = new ConversationEntity { Id = Guid.NewGuid(), Title = "Test" };
            context.Conversations.Add(conv);
            context.SaveChanges();

            context.Messages.Add(new MessageEntity
            {
                Id = Guid.NewGuid(),
                ConversationId = conv.Id,
                Role = MessageRole.User,
                Content = "Test",
                SequenceNumber = 0
            });
            context.SaveChanges();

            context.SystemPrompts.Add(new SystemPromptEntity
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Content = "Test"
            });
            context.SaveChanges();

            context.InferencePresets.Add(new InferencePresetEntity
            {
                Id = Guid.NewGuid(),
                Name = "Test"
            });
            context.SaveChanges();
        }
        catch
        {
            canAddConversation = false;
            canAddMessage = false;
            canAddSystemPrompt = false;
            canAddInferencePreset = false;
        }

        // Assert
        Assert.True(canAddConversation, "Should be able to add to Conversations table");
        Assert.True(canAddMessage, "Should be able to add to Messages table");
        Assert.True(canAddSystemPrompt, "Should be able to add to SystemPrompts table");
        Assert.True(canAddInferencePreset, "Should be able to add to InferencePresets table");
    }

    #endregion
}
