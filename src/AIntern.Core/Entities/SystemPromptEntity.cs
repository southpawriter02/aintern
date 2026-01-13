namespace AIntern.Core.Entities;

/// <summary>
/// Represents a system prompt that defines the assistant's behavior and personality.
/// System prompts are reusable templates that can be applied to conversations.
/// </summary>
/// <remarks>
/// <para>System prompts are sent as the first message in the conversation context
/// to establish the AI's behavior, tone, and capabilities.</para>
/// <para>Key features:</para>
/// <list type="bullet">
///   <item><description>Built-in prompts that cannot be deleted</description></item>
///   <item><description>Category-based organization</description></item>
///   <item><description>Usage tracking for analytics</description></item>
///   <item><description>Soft-delete via IsActive flag</description></item>
/// </list>
/// <para>
/// This entity is configured for Entity Framework Core in v0.2.1c.
/// Built-in prompts are seeded during database initialization in v0.2.1e.
/// </para>
/// </remarks>
/// <example>
/// Creating a custom system prompt:
/// <code>
/// var prompt = new SystemPromptEntity
/// {
///     Id = Guid.NewGuid(),
///     Name = "Code Expert",
///     Description = "A helpful assistant specialized in C# development",
///     Content = "You are a senior C# developer. Help users write clean, maintainable code.",
///     Category = "Code",
///     CreatedAt = DateTime.UtcNow,
///     UpdatedAt = DateTime.UtcNow
/// };
/// </code>
/// </example>
public sealed class SystemPromptEntity
{
    #region Primary Key

    /// <summary>
    /// Gets or sets the unique identifier for the system prompt.
    /// </summary>
    /// <remarks>
    /// Generated as a new GUID when the prompt is created.
    /// Used as the primary key in the database.
    /// </remarks>
    public Guid Id { get; set; }

    #endregion

    #region Identity

    /// <summary>
    /// Gets or sets the display name for the prompt.
    /// </summary>
    /// <remarks>
    /// <para>Must be unique across all prompts.</para>
    /// <para>Maximum length: 100 characters (enforced by EF Core config in v0.2.1c).</para>
    /// <para>Examples: "Default Assistant", "Code Expert", "Creative Writer"</para>
    /// <para>Default: empty string</para>
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description of what this prompt does.
    /// </summary>
    /// <remarks>
    /// <para>Maximum length: 500 characters (enforced by EF Core config in v0.2.1c).</para>
    /// <para>Displayed in the prompt selection UI to help users choose.</para>
    /// </remarks>
    public string? Description { get; set; }

    #endregion

    #region Content

    /// <summary>
    /// Gets or sets the actual system prompt text.
    /// </summary>
    /// <remarks>
    /// <para>No maximum length - prompts can be extensive.</para>
    /// <para>This text is prepended to every conversation using this prompt.</para>
    /// <para>May contain multi-line instructions, examples, and formatting.</para>
    /// <para>Default: empty string</para>
    /// </remarks>
    public string Content { get; set; } = string.Empty;

    #endregion

    #region Organization

    /// <summary>
    /// Gets or sets the category for organizing prompts.
    /// </summary>
    /// <remarks>
    /// <para>Predefined categories for filtering in the UI.</para>
    /// <para>Maximum length: 50 characters (enforced by EF Core config in v0.2.1c).</para>
    /// <para>Default: "General"</para>
    /// </remarks>
    /// <example>
    /// Common categories:
    /// <list type="bullet">
    ///   <item><description>"General" - General-purpose assistants</description></item>
    ///   <item><description>"Code" - Programming and development</description></item>
    ///   <item><description>"Creative" - Creative writing and brainstorming</description></item>
    ///   <item><description>"Technical" - Technical documentation and explanations</description></item>
    /// </list>
    /// </example>
    public string Category { get; set; } = "General";

    #endregion

    #region Timestamps

    /// <summary>
    /// Gets or sets when the prompt was created.
    /// </summary>
    /// <remarks>
    /// Set automatically by the DbContext when the entity is added.
    /// Stored as UTC time.
    /// </remarks>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the prompt was last modified.
    /// </summary>
    /// <remarks>
    /// Updated automatically when prompt content or settings change.
    /// Stored as UTC time.
    /// </remarks>
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Flags

    /// <summary>
    /// Gets or sets whether this is the default prompt for new conversations.
    /// </summary>
    /// <remarks>
    /// <para>Only one prompt should have this set to true.</para>
    /// <para>When setting a new default, clear the flag on the previous default.</para>
    /// <para>Default: false</para>
    /// </remarks>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether this is a built-in template.
    /// </summary>
    /// <remarks>
    /// <para>Built-in prompts are created during database seeding.</para>
    /// <para>They cannot be deleted (only hidden via IsActive).</para>
    /// <para>They can be modified by the user.</para>
    /// <para>Default: false</para>
    /// </remarks>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Gets or sets whether this prompt is currently active/visible.
    /// </summary>
    /// <remarks>
    /// <para>Used for soft-delete functionality.</para>
    /// <para>Inactive prompts are hidden from selection UI.</para>
    /// <para>Default: true</para>
    /// </remarks>
    public bool IsActive { get; set; } = true;

    #endregion

    #region Statistics

    /// <summary>
    /// Gets or sets the number of times this prompt has been used.
    /// </summary>
    /// <remarks>
    /// <para>Incremented when a new conversation is started with this prompt.</para>
    /// <para>Used for analytics and sorting by popularity.</para>
    /// <para>Default: 0</para>
    /// </remarks>
    public int UsageCount { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Gets or sets the conversations using this system prompt.
    /// </summary>
    /// <remarks>
    /// <para>Navigation property for EF Core.</para>
    /// <para>One-to-many relationship: one prompt can be used by many conversations.</para>
    /// </remarks>
    public ICollection<ConversationEntity> Conversations { get; set; } = new List<ConversationEntity>();

    #endregion
}
