using AIntern.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIntern.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="ConversationEntity"/> entity.
/// Defines table mapping, column constraints, indexes, and relationships.
/// </summary>
/// <remarks>
/// <para>
/// This configuration establishes the "Conversations" table schema with the following key features:
/// </para>
/// <list type="bullet">
///   <item><description>Primary key on Id (GUID)</description></item>
///   <item><description>Foreign key to SystemPrompts with SetNull delete behavior</description></item>
///   <item><description>Six indexes optimized for list queries and filtering</description></item>
///   <item><description>Column constraints including max lengths and defaults</description></item>
/// </list>
/// <para>
/// The composite index <c>IX_Conversations_List</c> is specifically designed to support
/// the main conversation list query pattern: non-archived conversations, pinned first,
/// then sorted by most recent update.
/// </para>
/// </remarks>
/// <example>
/// The configuration is automatically discovered and applied by the DbContext:
/// <code>
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConversationConfiguration).Assembly);
/// }
/// </code>
/// </example>
public sealed class ConversationConfiguration : IEntityTypeConfiguration<ConversationEntity>
{
    #region Constants

    /// <summary>
    /// The database table name for conversations.
    /// </summary>
    public const string TableName = "Conversations";

    /// <summary>
    /// Maximum length for the Title column.
    /// </summary>
    public const int TitleMaxLength = 200;

    /// <summary>
    /// Maximum length for the ModelPath column.
    /// </summary>
    public const int ModelPathMaxLength = 500;

    /// <summary>
    /// Maximum length for the ModelName column.
    /// </summary>
    public const int ModelNameMaxLength = 100;

    #endregion

    #region Index Names

    /// <summary>
    /// Index name for UpdatedAt column (descending for recent-first queries).
    /// </summary>
    private const string IndexUpdatedAt = "IX_Conversations_UpdatedAt";

    /// <summary>
    /// Index name for IsArchived column.
    /// </summary>
    private const string IndexIsArchived = "IX_Conversations_IsArchived";

    /// <summary>
    /// Index name for IsPinned column.
    /// </summary>
    private const string IndexIsPinned = "IX_Conversations_IsPinned";

    /// <summary>
    /// Index name for SystemPromptId foreign key.
    /// </summary>
    private const string IndexSystemPromptId = "IX_Conversations_SystemPromptId";

    /// <summary>
    /// Index name for CreatedAt column.
    /// </summary>
    private const string IndexCreatedAt = "IX_Conversations_CreatedAt";

    /// <summary>
    /// Index name for the composite list query index.
    /// </summary>
    private const string IndexList = "IX_Conversations_List";

    #endregion

    #region Configure Method

    /// <summary>
    /// Configures the entity mapping for <see cref="ConversationEntity"/>.
    /// </summary>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <remarks>
    /// <para>Configuration includes:</para>
    /// <list type="bullet">
    ///   <item><description>Table name and comment</description></item>
    ///   <item><description>Primary key configuration</description></item>
    ///   <item><description>Column constraints (required, max length, defaults)</description></item>
    ///   <item><description>Six indexes for query optimization</description></item>
    ///   <item><description>Foreign key relationship to SystemPromptEntity</description></item>
    /// </list>
    /// </remarks>
    public void Configure(EntityTypeBuilder<ConversationEntity> builder)
    {
        // Table configuration
        builder.ToTable(TableName, table =>
        {
            table.HasComment("Stores chat conversations with metadata, timestamps, and organization flags.");
        });

        // Primary key
        builder.HasKey(c => c.Id);

        // Title column
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(TitleMaxLength)
            .HasDefaultValue("New Conversation")
            .HasComment("Display title for the conversation, auto-generated or user-defined.");

        // Model information columns
        builder.Property(c => c.ModelPath)
            .HasMaxLength(ModelPathMaxLength)
            .HasComment("Full path to the model file used for this conversation.");

        builder.Property(c => c.ModelName)
            .HasMaxLength(ModelNameMaxLength)
            .HasComment("Human-readable model name for display purposes.");

        // Timestamp columns
        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasComment("UTC timestamp when the conversation was created.");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasComment("UTC timestamp when the conversation was last modified.");

        // Organization flags
        builder.Property(c => c.IsArchived)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether the conversation is archived and hidden from main list.");

        builder.Property(c => c.IsPinned)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether the conversation is pinned to the top of the list.");

        // Denormalized statistics
        builder.Property(c => c.MessageCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Total number of messages in this conversation.");

        builder.Property(c => c.TotalTokenCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Approximate total tokens used across all messages.");

        // Foreign key property
        builder.Property(c => c.SystemPromptId)
            .HasComment("Optional reference to a system prompt template.");

        // Indexes
        ConfigureIndexes(builder);

        // Relationships
        ConfigureRelationships(builder);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Configures indexes for optimized query performance.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    private static void ConfigureIndexes(EntityTypeBuilder<ConversationEntity> builder)
    {
        // Index for sorting by most recent
        builder.HasIndex(c => c.UpdatedAt)
            .HasDatabaseName(IndexUpdatedAt)
            .IsDescending();

        // Index for filtering archived conversations
        builder.HasIndex(c => c.IsArchived)
            .HasDatabaseName(IndexIsArchived);

        // Index for filtering pinned conversations
        builder.HasIndex(c => c.IsPinned)
            .HasDatabaseName(IndexIsPinned);

        // Index for foreign key lookups
        builder.HasIndex(c => c.SystemPromptId)
            .HasDatabaseName(IndexSystemPromptId);

        // Index for creation date sorting
        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName(IndexCreatedAt);

        // Composite index for main list query:
        // WHERE IsArchived = 0 ORDER BY IsPinned DESC, UpdatedAt DESC
        builder.HasIndex(c => new { c.IsArchived, c.IsPinned, c.UpdatedAt })
            .HasDatabaseName(IndexList)
            .IsDescending(false, true, true);
    }

    /// <summary>
    /// Configures relationships with other entities.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    private static void ConfigureRelationships(EntityTypeBuilder<ConversationEntity> builder)
    {
        // SetNull behavior: When a SystemPrompt is deleted, conversations remain intact.
        // This is the appropriate choice because:
        // 1. Conversations have value independent of their prompt
        // 2. Users shouldn't lose conversation history when cleaning up prompts
        // 3. A null SystemPromptId simply means "no prompt assigned"
        // Alternative (Cascade) would delete all conversations using a prompt - too destructive.
        // Alternative (Restrict) would prevent prompt deletion if any conversation uses it.
        builder.HasOne(c => c.SystemPrompt)
            .WithMany(sp => sp.Conversations)
            .HasForeignKey(c => c.SystemPromptId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relationship to MessageEntity is configured in MessageConfiguration
        // (Conversation has navigation property Messages, configured from Message side)
    }

    #endregion
}
