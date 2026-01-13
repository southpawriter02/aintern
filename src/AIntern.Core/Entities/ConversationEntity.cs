namespace AIntern.Core.Entities;

/// <summary>
/// Represents a conversation (chat session) stored in the database.
/// A conversation contains an ordered collection of messages and metadata
/// about the chat session.
/// </summary>
/// <remarks>
/// <para>Conversations are the primary organizational unit for chat history.</para>
/// <para>Key features:</para>
/// <list type="bullet">
///   <item><description>Auto-generated titles from first user message</description></item>
///   <item><description>Support for pinning and archiving</description></item>
///   <item><description>Optional system prompt association</description></item>
///   <item><description>Denormalized counts for efficient list display</description></item>
/// </list>
/// <para>
/// This entity is configured for Entity Framework Core in v0.2.1c.
/// Navigation properties enable lazy loading and eager loading of related entities.
/// </para>
/// </remarks>
/// <example>
/// Creating a new conversation:
/// <code>
/// var conversation = new ConversationEntity
/// {
///     Id = Guid.NewGuid(),
///     Title = "How to implement DI in C#",
///     CreatedAt = DateTime.UtcNow,
///     UpdatedAt = DateTime.UtcNow
/// };
/// </code>
/// </example>
public sealed class ConversationEntity
{
    #region Primary Key

    /// <summary>
    /// Gets or sets the unique identifier for the conversation.
    /// </summary>
    /// <remarks>
    /// Generated as a new GUID when the conversation is created.
    /// Used as the primary key in the database.
    /// </remarks>
    public Guid Id { get; set; }

    #endregion

    #region Display Properties

    /// <summary>
    /// Gets or sets the display title for the conversation.
    /// </summary>
    /// <remarks>
    /// <para>Default value: "New Conversation"</para>
    /// <para>Can be auto-generated from the first user message content,
    /// typically truncated to 50-100 characters.</para>
    /// <para>Users can manually edit the title.</para>
    /// </remarks>
    /// <example>"How to implement dependency injection in C#"</example>
    public string Title { get; set; } = "New Conversation";

    #endregion

    #region Timestamps

    /// <summary>
    /// Gets or sets when the conversation was created.
    /// </summary>
    /// <remarks>
    /// Set automatically by the DbContext when the entity is added.
    /// Stored as UTC time for consistent ordering across time zones.
    /// </remarks>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the conversation was last modified.
    /// </summary>
    /// <remarks>
    /// Updated automatically when:
    /// <list type="bullet">
    ///   <item><description>A message is added</description></item>
    ///   <item><description>A message is edited</description></item>
    ///   <item><description>The title is changed</description></item>
    ///   <item><description>Settings are modified</description></item>
    /// </list>
    /// Used for sorting conversations by recency.
    /// </remarks>
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Model Information

    /// <summary>
    /// Gets or sets the path to the model file used for this conversation.
    /// </summary>
    /// <remarks>
    /// <para>Stored for reference and display purposes.</para>
    /// <para>May be null if no model has been loaded yet.</para>
    /// <para>Example: "/Users/john/models/llama-2-7b-chat.Q4_K_M.gguf"</para>
    /// </remarks>
    public string? ModelPath { get; set; }

    /// <summary>
    /// Gets or sets the human-readable model name.
    /// </summary>
    /// <remarks>
    /// <para>Can be extracted from the model path filename or user-defined.</para>
    /// <para>Displayed in the UI to identify which model was used.</para>
    /// <para>Example: "Llama 2 7B Chat"</para>
    /// </remarks>
    public string? ModelName { get; set; }

    #endregion

    #region System Prompt Reference

    /// <summary>
    /// Gets or sets the foreign key reference to the system prompt used for this conversation.
    /// </summary>
    /// <remarks>
    /// <para>Null indicates using the default system prompt or no system prompt.</para>
    /// <para>When the referenced SystemPrompt is deleted, this is set to null
    /// (SetNull delete behavior configured in v0.2.1c).</para>
    /// </remarks>
    public Guid? SystemPromptId { get; set; }

    #endregion

    #region Organization Flags

    /// <summary>
    /// Gets or sets whether the conversation is archived.
    /// </summary>
    /// <remarks>
    /// <para>Archived conversations are hidden from the main conversation list
    /// but can be viewed in an "Archived" section.</para>
    /// <para>Default: false</para>
    /// </remarks>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets whether the conversation is pinned to the top.
    /// </summary>
    /// <remarks>
    /// <para>Pinned conversations appear at the top of the conversation list,
    /// sorted by UpdatedAt among pinned items.</para>
    /// <para>Default: false</para>
    /// </remarks>
    public bool IsPinned { get; set; }

    #endregion

    #region Denormalized Statistics

    /// <summary>
    /// Gets or sets the total number of messages in this conversation.
    /// </summary>
    /// <remarks>
    /// <para>Denormalized for efficient display in conversation lists
    /// without needing to count related messages.</para>
    /// <para>Updated when messages are added or deleted.</para>
    /// <para>Default: 0</para>
    /// </remarks>
    public int MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the approximate total tokens used in this conversation.
    /// </summary>
    /// <remarks>
    /// <para>Sum of TokenCount from all messages.</para>
    /// <para>Used for context window management and statistics display.</para>
    /// <para>Default: 0</para>
    /// </remarks>
    public int TotalTokenCount { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Gets or sets the system prompt associated with this conversation.
    /// </summary>
    /// <remarks>
    /// Navigation property for EF Core.
    /// May be null if no specific system prompt is assigned.
    /// </remarks>
    public SystemPromptEntity? SystemPrompt { get; set; }

    /// <summary>
    /// Gets or sets all messages in this conversation.
    /// </summary>
    /// <remarks>
    /// <para>Navigation property for EF Core.</para>
    /// <para>Messages should be ordered by SequenceNumber when queried.</para>
    /// <para>Cascade delete: When conversation is deleted, all messages are deleted.</para>
    /// </remarks>
    public ICollection<MessageEntity> Messages { get; set; } = new List<MessageEntity>();

    #endregion
}
