using AIntern.Core.Models;

namespace AIntern.Core.Entities;

/// <summary>
/// Represents a single message within a conversation.
/// Messages are ordered by SequenceNumber within their parent conversation.
/// </summary>
/// <remarks>
/// <para>Messages can be from three roles: System, User, or Assistant.</para>
/// <para>Key features:</para>
/// <list type="bullet">
///   <item><description>Sequence-based ordering within conversation</description></item>
///   <item><description>Token counting for context management</description></item>
///   <item><description>Generation statistics for assistant messages</description></item>
///   <item><description>Edit tracking with timestamps</description></item>
///   <item><description>Incomplete message handling for cancelled generations</description></item>
/// </list>
/// <para>
/// This entity uses the <see cref="MessageRole"/> enum from <c>AIntern.Core.Models</c>
/// to maintain consistency with the existing ChatMessage model.
/// </para>
/// </remarks>
/// <example>
/// Creating a user message:
/// <code>
/// var message = new MessageEntity
/// {
///     Id = Guid.NewGuid(),
///     ConversationId = conversationId,
///     Role = MessageRole.User,
///     Content = "How do I implement dependency injection?",
///     SequenceNumber = 1,
///     Timestamp = DateTime.UtcNow
/// };
/// </code>
/// </example>
public sealed class MessageEntity
{
    #region Primary Key

    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    /// <remarks>
    /// Generated as a new GUID when the message is created.
    /// Used as the primary key in the database.
    /// </remarks>
    public Guid Id { get; set; }

    #endregion

    #region Foreign Key

    /// <summary>
    /// Gets or sets the conversation this message belongs to.
    /// </summary>
    /// <remarks>
    /// Required foreign key. Messages cannot exist without a parent conversation.
    /// Configured with cascade delete behavior in v0.2.1c.
    /// </remarks>
    public Guid ConversationId { get; set; }

    #endregion

    #region Message Content

    /// <summary>
    /// Gets or sets the role of the message sender.
    /// </summary>
    /// <remarks>
    /// Determines how the message is displayed and processed:
    /// <list type="bullet">
    ///   <item><description>System: Hidden from UI, prepended to context</description></item>
    ///   <item><description>User: Displayed on right side, user input</description></item>
    ///   <item><description>Assistant: Displayed on left side, AI response</description></item>
    /// </list>
    /// <para>Stored as integer in database (System=0, User=1, Assistant=2).</para>
    /// <para>Uses the existing <see cref="MessageRole"/> enum from <c>AIntern.Core.Models</c>.</para>
    /// </remarks>
    public MessageRole Role { get; set; }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    /// <remarks>
    /// <para>Can be any length (no database limit).</para>
    /// <para>May contain markdown formatting.</para>
    /// <para>For assistant messages, may be partial if generation was cancelled.</para>
    /// <para>Default: empty string</para>
    /// </remarks>
    public string Content { get; set; } = string.Empty;

    #endregion

    #region Ordering

    /// <summary>
    /// Gets or sets the order of this message within the conversation.
    /// </summary>
    /// <remarks>
    /// <para>Starts at 1 for the first message.</para>
    /// <para>Unique within each conversation (enforced by database index in v0.2.1c).</para>
    /// <para>Used to maintain chronological message order.</para>
    /// </remarks>
    public int SequenceNumber { get; set; }

    #endregion

    #region Timestamps

    /// <summary>
    /// Gets or sets when the message was created.
    /// </summary>
    /// <remarks>
    /// Set automatically when the message is first saved.
    /// For assistant messages, this is when generation started.
    /// Stored as UTC time.
    /// </remarks>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets when the message was last edited.
    /// </summary>
    /// <remarks>
    /// Null if the message has never been edited.
    /// Set when IsEdited becomes true.
    /// </remarks>
    public DateTime? EditedAt { get; set; }

    #endregion

    #region Token Statistics

    /// <summary>
    /// Gets or sets the number of tokens in this message.
    /// </summary>
    /// <remarks>
    /// <para>Null if not yet calculated.</para>
    /// <para>Calculated using the model's tokenizer.</para>
    /// <para>Used for context window management.</para>
    /// </remarks>
    public int? TokenCount { get; set; }

    #endregion

    #region Generation Statistics (Assistant Messages Only)

    /// <summary>
    /// Gets or sets the time taken to generate this message in milliseconds.
    /// </summary>
    /// <remarks>
    /// <para>Only applicable for Assistant messages.</para>
    /// <para>Null for User and System messages.</para>
    /// <para>Measures time from generation start to completion.</para>
    /// </remarks>
    public int? GenerationTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the tokens generated per second during message generation.
    /// </summary>
    /// <remarks>
    /// <para>Only applicable for Assistant messages.</para>
    /// <para>Calculated as TokenCount / (GenerationTimeMs / 1000).</para>
    /// <para>Used for performance monitoring and display.</para>
    /// </remarks>
    public float? TokensPerSecond { get; set; }

    #endregion

    #region Status Flags

    /// <summary>
    /// Gets or sets whether this message has been edited after creation.
    /// </summary>
    /// <remarks>
    /// <para>Default: false</para>
    /// <para>Set to true when user edits their message or regenerates an assistant response.</para>
    /// </remarks>
    public bool IsEdited { get; set; }

    /// <summary>
    /// Gets or sets whether the message generation was completed.
    /// </summary>
    /// <remarks>
    /// <para>Default: true</para>
    /// <para>Set to false for assistant messages where generation was cancelled mid-stream.</para>
    /// <para>Incomplete messages may be displayed with a visual indicator.</para>
    /// </remarks>
    public bool IsComplete { get; set; } = true;

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Gets or sets the conversation this message belongs to.
    /// </summary>
    /// <remarks>
    /// Navigation property for EF Core.
    /// Required relationship - message cannot exist without conversation.
    /// </remarks>
    public ConversationEntity Conversation { get; set; } = null!;

    #endregion
}
