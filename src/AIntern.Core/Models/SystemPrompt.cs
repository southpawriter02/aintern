using System.Text.Json;

namespace AIntern.Core.Models;

/// <summary>
/// Domain model representing a system prompt for AI conversations.
/// Provides validation, duplication, and computed properties for UI display.
/// </summary>
/// <remarks>
/// <para>
/// This domain model is separate from <see cref="Entities.SystemPromptEntity"/> to provide:
/// </para>
/// <list type="bullet">
///   <item><description><b>Validation:</b> Comprehensive field validation via <see cref="Validate"/></description></item>
///   <item><description><b>Computed properties:</b> <see cref="CharacterCount"/> and <see cref="EstimatedTokenCount"/></description></item>
///   <item><description><b>Duplication:</b> Deep copy for creating user copies of templates via <see cref="Duplicate"/></description></item>
///   <item><description><b>Tags as List:</b> Deserialized tags for easier manipulation</description></item>
/// </list>
/// <para>
/// <b>Mapping:</b> Convert to/from <see cref="Entities.SystemPromptEntity"/> using
/// <see cref="FromEntity"/> and <see cref="ToEntity"/> methods.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is not thread-safe. Create new instances for concurrent use.
/// </para>
/// </remarks>
/// <example>
/// Creating and validating a new system prompt:
/// <code>
/// var prompt = new SystemPrompt
/// {
///     Name = "My Custom Prompt",
///     Content = "You are a helpful assistant...",
///     Description = "A custom assistant for my needs",
///     Category = "General"
/// };
///
/// var result = prompt.Validate();
/// if (!result.IsValid)
/// {
///     Console.WriteLine(result.GetAllErrors());
/// }
/// </code>
/// </example>
public sealed class SystemPrompt
{
    #region Constants

    /// <summary>
    /// Maximum allowed length for the <see cref="Name"/> property.
    /// </summary>
    /// <remarks>
    /// Matches the database column constraint in <see cref="Data.Configurations.SystemPromptConfiguration"/>.
    /// </remarks>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Maximum allowed length for the <see cref="Description"/> property.
    /// </summary>
    /// <remarks>
    /// Matches the database column constraint in <see cref="Data.Configurations.SystemPromptConfiguration"/>.
    /// </remarks>
    public const int DescriptionMaxLength = 500;

    /// <summary>
    /// Maximum allowed length for the <see cref="Content"/> property.
    /// </summary>
    /// <remarks>
    /// Large enough to accommodate extensive system prompts while preventing
    /// accidental submission of extremely large content that could cause issues.
    /// </remarks>
    public const int ContentMaxLength = 50000;

    /// <summary>
    /// Maximum allowed length for the <see cref="Category"/> property.
    /// </summary>
    public const int CategoryMaxLength = 50;

    /// <summary>
    /// Approximate characters per token for estimation purposes.
    /// </summary>
    /// <remarks>
    /// This is a rough approximation. Actual token counts depend on the tokenizer
    /// used by the specific model. English text averages ~4 characters per token,
    /// but this varies significantly with code, special characters, and non-Latin scripts.
    /// </remarks>
    private const int CharactersPerToken = 4;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the unique identifier for the system prompt.
    /// </summary>
    /// <remarks>
    /// Generated as a new GUID when the prompt is created.
    /// Well-known GUIDs are used for built-in templates.
    /// </remarks>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display name for the prompt.
    /// </summary>
    /// <remarks>
    /// <para>Must be unique across all prompts.</para>
    /// <para>Maximum length: <see cref="NameMaxLength"/> characters.</para>
    /// <para>Examples: "Default Assistant", "Code Expert", "Creative Writer"</para>
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the actual system prompt text sent to the model.
    /// </summary>
    /// <remarks>
    /// <para>This text is prepended to every conversation using this prompt.</para>
    /// <para>May contain multi-line instructions, examples, and formatting.</para>
    /// <para>Maximum length: <see cref="ContentMaxLength"/> characters.</para>
    /// </remarks>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description of what this prompt does.
    /// </summary>
    /// <remarks>
    /// <para>Displayed in the prompt selection UI to help users choose.</para>
    /// <para>Maximum length: <see cref="DescriptionMaxLength"/> characters.</para>
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category for organizing prompts.
    /// </summary>
    /// <remarks>
    /// <para>Predefined categories for filtering in the UI.</para>
    /// <para>Maximum length: <see cref="CategoryMaxLength"/> characters.</para>
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

    /// <summary>
    /// Gets or sets the tags associated with this prompt.
    /// </summary>
    /// <remarks>
    /// <para>Tags provide searchable keywords for finding prompts.</para>
    /// <para>Stored as JSON in the database via <see cref="Entities.SystemPromptEntity.TagsJson"/>.</para>
    /// </remarks>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets when the prompt was created.
    /// </summary>
    /// <remarks>
    /// Stored as UTC time.
    /// </remarks>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the prompt was last modified.
    /// </summary>
    /// <remarks>
    /// Updated automatically when prompt content or settings change.
    /// Stored as UTC time.
    /// </remarks>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this is the default prompt for new conversations.
    /// </summary>
    /// <remarks>
    /// <para>Only one prompt should have this set to true.</para>
    /// <para>When setting a new default, clear the flag on the previous default.</para>
    /// </remarks>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether this is a built-in template.
    /// </summary>
    /// <remarks>
    /// <para>Built-in prompts are created during database seeding.</para>
    /// <para>They cannot be deleted (only hidden via IsActive).</para>
    /// <para>They can be duplicated to create user-editable copies.</para>
    /// </remarks>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Gets or sets whether this prompt is currently active/visible.
    /// </summary>
    /// <remarks>
    /// <para>Used for soft-delete functionality.</para>
    /// <para>Inactive prompts are hidden from selection UI.</para>
    /// </remarks>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of times this prompt has been used.
    /// </summary>
    /// <remarks>
    /// <para>Incremented when a new conversation is started with this prompt.</para>
    /// <para>Used for analytics and sorting by popularity.</para>
    /// </remarks>
    public int UsageCount { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the character count of the prompt content.
    /// </summary>
    /// <remarks>
    /// Returns 0 if <see cref="Content"/> is null.
    /// Displayed in the editor UI to help users gauge prompt length.
    /// </remarks>
    public int CharacterCount => Content?.Length ?? 0;

    /// <summary>
    /// Gets an estimated token count based on character length.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a rough approximation using ~4 characters per token.
    /// Actual token counts depend on the specific tokenizer and content.
    /// </para>
    /// <para>
    /// Use this for UI display and rough context window estimation only.
    /// For accurate token counts, use the model's actual tokenizer.
    /// </para>
    /// </remarks>
    public int EstimatedTokenCount => CharacterCount / CharactersPerToken;

    #endregion

    #region Methods

    /// <summary>
    /// Validates all fields of the system prompt.
    /// </summary>
    /// <returns>
    /// A <see cref="ValidationResult"/> indicating whether validation passed
    /// and containing any error messages.
    /// </returns>
    /// <remarks>
    /// <para>Validates the following constraints:</para>
    /// <list type="bullet">
    ///   <item><description>Name is required and ≤ <see cref="NameMaxLength"/> characters</description></item>
    ///   <item><description>Content is required and ≤ <see cref="ContentMaxLength"/> characters</description></item>
    ///   <item><description>Description (if provided) is ≤ <see cref="DescriptionMaxLength"/> characters</description></item>
    ///   <item><description>Category is required and ≤ <see cref="CategoryMaxLength"/> characters</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = prompt.Validate();
    /// if (!result.IsValid)
    /// {
    ///     foreach (var error in result.Errors)
    ///     {
    ///         Console.WriteLine(error);
    ///     }
    /// }
    /// </code>
    /// </example>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        // Validate Name: Required field, must not be empty or whitespace.
        // This is the primary identifier shown in the UI dropdown.
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Name is required.");
        }
        else if (Name.Length > NameMaxLength)
        {
            errors.Add($"Name must not exceed {NameMaxLength} characters.");
        }

        // Validate Content: Required field, contains the actual system prompt.
        // Empty content would result in no system instruction to the model.
        if (string.IsNullOrWhiteSpace(Content))
        {
            errors.Add("Content is required.");
        }
        else if (Content.Length > ContentMaxLength)
        {
            errors.Add($"Content must not exceed {ContentMaxLength:N0} characters.");
        }

        // Validate Description: Optional field, but enforce max length if provided.
        if (Description != null && Description.Length > DescriptionMaxLength)
        {
            errors.Add($"Description must not exceed {DescriptionMaxLength} characters.");
        }

        // Validate Category: Required field for organization and filtering.
        if (string.IsNullOrWhiteSpace(Category))
        {
            errors.Add("Category is required.");
        }
        else if (Category.Length > CategoryMaxLength)
        {
            errors.Add($"Category must not exceed {CategoryMaxLength} characters.");
        }

        return errors.Count == 0
            ? ValidationResult.Success
            : ValidationResult.Failure(errors);
    }

    /// <summary>
    /// Creates a deep copy of this system prompt with a new ID.
    /// </summary>
    /// <param name="newName">
    /// Optional new name for the duplicate. If not provided, appends " (Copy)" to the original name.
    /// </param>
    /// <returns>
    /// A new <see cref="SystemPrompt"/> instance with copied values, a new ID,
    /// and reset flags (IsBuiltIn=false, IsDefault=false).
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is used to create user-editable copies of built-in templates.
    /// The duplicate:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Gets a new unique ID</description></item>
    ///   <item><description>Is marked as not built-in (user can edit/delete)</description></item>
    ///   <item><description>Is not marked as default</description></item>
    ///   <item><description>Has reset usage count and timestamps</description></item>
    ///   <item><description>Gets a copy of tags (not the same reference)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var template = await repository.GetByNameAsync("Code Expert");
    /// var copy = template.Duplicate("My Code Expert");
    /// await repository.CreateAsync(copy);
    /// </code>
    /// </example>
    public SystemPrompt Duplicate(string? newName = null)
    {
        var now = DateTime.UtcNow;

        return new SystemPrompt
        {
            // Generate new ID - this is now a separate entity
            Id = Guid.NewGuid(),

            // Use provided name or append " (Copy)" to original
            Name = newName ?? $"{Name} (Copy)",

            // Copy content fields exactly
            Content = Content,
            Description = Description,
            Category = Category,

            // Deep copy the tags list to avoid shared reference
            Tags = [.. Tags],

            // Reset timestamps to now
            CreatedAt = now,
            UpdatedAt = now,

            // Reset flags - duplicate is always user-created
            IsBuiltIn = false,
            IsDefault = false,
            IsActive = true,

            // Reset usage count - this is a new prompt
            UsageCount = 0
        };
    }

    /// <summary>
    /// Creates a domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity to convert.</param>
    /// <returns>A new <see cref="SystemPrompt"/> with values from the entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Deserializes <see cref="Entities.SystemPromptEntity.TagsJson"/> into the <see cref="Tags"/> list.
    /// If TagsJson is null or empty, an empty list is created.
    /// </para>
    /// </remarks>
    public static SystemPrompt FromEntity(Entities.SystemPromptEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Deserialize tags from JSON, defaulting to empty list if null/empty.
        // Using try-catch to handle malformed JSON gracefully.
        List<string> tags = [];
        if (!string.IsNullOrWhiteSpace(entity.TagsJson))
        {
            try
            {
                tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson) ?? [];
            }
            catch (JsonException)
            {
                // Log warning in production - for now, just use empty list
                tags = [];
            }
        }

        return new SystemPrompt
        {
            Id = entity.Id,
            Name = entity.Name,
            Content = entity.Content,
            Description = entity.Description,
            Category = entity.Category,
            Tags = tags,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDefault = entity.IsDefault,
            IsBuiltIn = entity.IsBuiltIn,
            IsActive = entity.IsActive,
            UsageCount = entity.UsageCount
        };
    }

    /// <summary>
    /// Converts this domain model to a database entity.
    /// </summary>
    /// <returns>A new <see cref="Entities.SystemPromptEntity"/> with values from this model.</returns>
    /// <remarks>
    /// <para>
    /// Serializes the <see cref="Tags"/> list to JSON for storage in
    /// <see cref="Entities.SystemPromptEntity.TagsJson"/>.
    /// </para>
    /// </remarks>
    public Entities.SystemPromptEntity ToEntity()
    {
        // Serialize tags to JSON, or null if empty.
        // This saves storage space for prompts without tags.
        string? tagsJson = Tags.Count > 0
            ? JsonSerializer.Serialize(Tags)
            : null;

        return new Entities.SystemPromptEntity
        {
            Id = Id,
            Name = Name,
            Content = Content,
            Description = Description,
            Category = Category,
            TagsJson = tagsJson,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            IsDefault = IsDefault,
            IsBuiltIn = IsBuiltIn,
            IsActive = IsActive,
            UsageCount = UsageCount
        };
    }

    #endregion
}
