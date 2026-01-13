using AIntern.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIntern.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="MessageEntity"/> entity.
/// Defines table mapping, column constraints, indexes, and relationships.
/// </summary>
/// <remarks>
/// <para>
/// This configuration establishes the "Messages" table schema with the following key features:
/// </para>
/// <list type="bullet">
///   <item><description>Primary key on Id (GUID)</description></item>
///   <item><description>Foreign key to Conversations with Cascade delete behavior</description></item>
///   <item><description>Unique composite constraint on (ConversationId, SequenceNumber)</description></item>
///   <item><description>MessageRole enum stored as integer</description></item>
///   <item><description>Four indexes for query optimization</description></item>
/// </list>
/// <para>
/// The unique constraint ensures message ordering integrity within each conversation,
/// preventing duplicate sequence numbers.
/// </para>
/// </remarks>
/// <example>
/// The configuration is automatically discovered and applied by the DbContext:
/// <code>
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessageConfiguration).Assembly);
/// }
/// </code>
/// </example>
public sealed class MessageConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    #region Constants

    /// <summary>
    /// The database table name for messages.
    /// </summary>
    public const string TableName = "Messages";

    #endregion

    #region Index Names

    /// <summary>
    /// Index name for ConversationId foreign key.
    /// </summary>
    private const string IndexConversationId = "IX_Messages_ConversationId";

    /// <summary>
    /// Index name for Timestamp column.
    /// </summary>
    private const string IndexTimestamp = "IX_Messages_Timestamp";

    /// <summary>
    /// Index name for Role column.
    /// </summary>
    private const string IndexRole = "IX_Messages_Role";

    /// <summary>
    /// Index name for the unique composite constraint on (ConversationId, SequenceNumber).
    /// </summary>
    private const string IndexConversationSequence = "IX_Messages_ConversationId_SequenceNumber";

    #endregion

    #region Configure Method

    /// <summary>
    /// Configures the entity mapping for <see cref="MessageEntity"/>.
    /// </summary>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <remarks>
    /// <para>Configuration includes:</para>
    /// <list type="bullet">
    ///   <item><description>Table name and comment</description></item>
    ///   <item><description>Primary key configuration</description></item>
    ///   <item><description>Column constraints (required, optional statistics)</description></item>
    ///   <item><description>Unique composite index for message ordering</description></item>
    ///   <item><description>Foreign key relationship to ConversationEntity with cascade delete</description></item>
    /// </list>
    /// </remarks>
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        // Table configuration
        builder.ToTable(TableName, table =>
        {
            table.HasComment("Stores individual messages within conversations, including content and metadata.");
        });

        // Primary key
        builder.HasKey(m => m.Id);

        // Foreign key property
        builder.Property(m => m.ConversationId)
            .IsRequired()
            .HasComment("Reference to the parent conversation.");

        // Role column - stored as integer
        builder.Property(m => m.Role)
            .IsRequired()
            .HasComment("Message role: 0=System, 1=User, 2=Assistant.");

        // Content column - unlimited text
        builder.Property(m => m.Content)
            .IsRequired()
            .HasComment("The message text content.");

        // Sequence number for ordering
        builder.Property(m => m.SequenceNumber)
            .IsRequired()
            .HasComment("Order of this message within the conversation, starting at 0.");

        // Timestamp columns
        builder.Property(m => m.Timestamp)
            .IsRequired()
            .HasComment("UTC timestamp when the message was created.");

        builder.Property(m => m.EditedAt)
            .HasComment("UTC timestamp when the message was last edited, null if never edited.");

        // Statistics columns (optional)
        builder.Property(m => m.TokenCount)
            .HasComment("Approximate token count for this message.");

        builder.Property(m => m.GenerationTimeMs)
            .HasComment("Time in milliseconds to generate this response (assistant messages only).");

        builder.Property(m => m.TokensPerSecond)
            .HasComment("Generation speed in tokens per second (assistant messages only).");

        // Status flags
        builder.Property(m => m.IsEdited)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether this message has been edited after creation.");

        builder.Property(m => m.IsComplete)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Whether generation is complete (false during streaming).");

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
    private static void ConfigureIndexes(EntityTypeBuilder<MessageEntity> builder)
    {
        // Index for foreign key lookups
        builder.HasIndex(m => m.ConversationId)
            .HasDatabaseName(IndexConversationId);

        // Index for timestamp queries
        builder.HasIndex(m => m.Timestamp)
            .HasDatabaseName(IndexTimestamp);

        // Index for role filtering
        builder.HasIndex(m => m.Role)
            .HasDatabaseName(IndexRole);

        // Unique composite index for message ordering within conversation.
        // This constraint is CRITICAL for maintaining conversation integrity:
        // 1. Prevents duplicate sequence numbers (database-enforced ordering)
        // 2. Ensures messages can be reliably ordered without gaps or overlaps
        // 3. Catches application bugs that might assign duplicate sequences
        // Note: Repository layer assigns sequences using MAX+1 pattern.
        builder.HasIndex(m => new { m.ConversationId, m.SequenceNumber })
            .HasDatabaseName(IndexConversationSequence)
            .IsUnique();
    }

    /// <summary>
    /// Configures relationships with other entities.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    private static void ConfigureRelationships(EntityTypeBuilder<MessageEntity> builder)
    {
        // Cascade delete: When a conversation is deleted, all its messages must go with it.
        // This is the only sensible behavior because:
        // 1. Messages have no meaning without their parent conversation
        // 2. Orphaned messages would waste storage and cause confusion
        // 3. The FK constraint would prevent deletion anyway if not cascaded
        // Alternative (Restrict) would require manual message deletion first.
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    #endregion
}
