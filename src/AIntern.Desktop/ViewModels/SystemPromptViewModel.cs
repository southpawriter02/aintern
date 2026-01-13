using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel representing a system prompt in UI lists (editor sidebar, selector dropdown).
/// Provides display properties and computed values for UI binding.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel is used in both <see cref="SystemPromptEditorViewModel"/> (UserPrompts/Templates lists)
/// and <see cref="SystemPromptSelectorViewModel"/> (AvailablePrompts list). It wraps the
/// <see cref="SystemPrompt"/> domain model with UI-specific computed properties.
/// </para>
/// <para>
/// <b>Computed Properties:</b>
/// <list type="bullet">
///   <item><see cref="CharacterCount"/> - Content length for display</item>
///   <item><see cref="EstimatedTokenCount"/> - Rough token estimate (~4 chars/token)</item>
///   <item><see cref="ContentPreview"/> - First 100 characters for list display</item>
///   <item><see cref="TypeLabel"/> - "Template" or "Custom" for badges</item>
///   <item><see cref="CategoryIcon"/> - Icon key based on category</item>
/// </list>
/// </para>
/// <para>Added in v0.2.4c.</para>
/// </remarks>
public sealed partial class SystemPromptViewModel : ViewModelBase
{
    #region Constants

    /// <summary>
    /// Maximum length for content preview display.
    /// </summary>
    private const int ContentPreviewMaxLength = 100;

    /// <summary>
    /// Approximate characters per token for estimation.
    /// </summary>
    /// <remarks>
    /// This is a rough approximation. Actual token counts depend on the tokenizer
    /// used by the specific model. English text averages ~4 characters per token.
    /// </remarks>
    private const int CharactersPerToken = 4;

    #endregion

    #region Identity Properties

    /// <summary>
    /// Gets the unique identifier for the system prompt.
    /// </summary>
    /// <remarks>
    /// This is init-only and set from the domain model or defaults to <see cref="Guid.Empty"/>
    /// for special items like the "No prompt" option in selectors.
    /// </remarks>
    public Guid Id { get; init; }

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets or sets the display name for the prompt.
    /// </summary>
    /// <remarks>
    /// Shown in list items and as the primary identifier in the UI.
    /// </remarks>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the actual system prompt text.
    /// </summary>
    /// <remarks>
    /// The full content is stored here but <see cref="ContentPreview"/> should be
    /// used for list display to avoid performance issues with large prompts.
    /// </remarks>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the prompt.
    /// </summary>
    /// <remarks>
    /// Displayed as secondary text in list items to help users understand
    /// the prompt's purpose.
    /// </remarks>
    [ObservableProperty]
    private string? _description;

    /// <summary>
    /// Gets or sets the category for organizing prompts.
    /// </summary>
    /// <remarks>
    /// Used for filtering and for determining <see cref="CategoryIcon"/>.
    /// Common values: "General", "Code", "Creative", "Technical".
    /// </remarks>
    [ObservableProperty]
    private string _category = "General";

    /// <summary>
    /// Gets or sets whether this is a built-in template prompt.
    /// </summary>
    /// <remarks>
    /// Built-in prompts cannot be edited or deleted directly.
    /// Users can duplicate them to create editable copies.
    /// </remarks>
    [ObservableProperty]
    private bool _isBuiltIn;

    /// <summary>
    /// Gets or sets whether this is the default prompt for new conversations.
    /// </summary>
    /// <remarks>
    /// Only one prompt should have this set to true at a time.
    /// Used to show a badge or indicator in the UI.
    /// </remarks>
    [ObservableProperty]
    private bool _isDefault;

    /// <summary>
    /// Gets or sets whether this prompt is currently selected in a list.
    /// </summary>
    /// <remarks>
    /// Used for visual selection state in list controls.
    /// Managed by the parent ViewModel when selection changes.
    /// </remarks>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets or sets the number of times this prompt has been used.
    /// </summary>
    /// <remarks>
    /// Displayed for analytics and can be used for sorting by popularity.
    /// </remarks>
    [ObservableProperty]
    private int _usageCount;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the character count of the prompt content.
    /// </summary>
    /// <remarks>
    /// Returns 0 if <see cref="Content"/> is null.
    /// Useful for displaying content length in the UI.
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
    /// </para>
    /// </remarks>
    public int EstimatedTokenCount => CharacterCount / CharactersPerToken;

    /// <summary>
    /// Gets a truncated preview of the content for list display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the first 100 characters with newlines replaced by spaces.
    /// Appends "..." if the content was truncated.
    /// </para>
    /// <para>
    /// Returns an empty string if <see cref="Content"/> is null or whitespace.
    /// </para>
    /// </remarks>
    public string ContentPreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                return string.Empty;
            }

            // Clean up newlines for single-line display.
            var clean = Content
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Trim();

            // Collapse multiple consecutive spaces.
            while (clean.Contains("  "))
            {
                clean = clean.Replace("  ", " ");
            }

            return clean.Length > ContentPreviewMaxLength
                ? clean[..ContentPreviewMaxLength] + "..."
                : clean;
        }
    }

    /// <summary>
    /// Gets a label indicating whether this is a template or custom prompt.
    /// </summary>
    /// <remarks>
    /// Returns "Template" for built-in prompts, "Custom" for user-created prompts.
    /// Useful for badges or visual differentiation in the UI.
    /// </remarks>
    public string TypeLabel => IsBuiltIn ? "Template" : "Custom";

    /// <summary>
    /// Gets an icon key based on the prompt's category.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns a string key that can be used to look up icons in the UI:
    /// <list type="bullet">
    ///   <item>"Code" → "CodeIcon"</item>
    ///   <item>"Creative" → "PaletteIcon"</item>
    ///   <item>"Technical" → "DocumentIcon"</item>
    ///   <item>"General" → "ChatIcon"</item>
    ///   <item>Default → "PromptIcon"</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string CategoryIcon => Category switch
    {
        "Code" => "CodeIcon",
        "Creative" => "PaletteIcon",
        "Technical" => "DocumentIcon",
        "General" => "ChatIcon",
        _ => "PromptIcon"
    };

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemPromptViewModel"/> class.
    /// </summary>
    /// <remarks>
    /// Creates an empty ViewModel. Use this for special items like "No prompt" options
    /// or for design-time data binding.
    /// </remarks>
    public SystemPromptViewModel()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemPromptViewModel"/> class
    /// from a domain model.
    /// </summary>
    /// <param name="prompt">The <see cref="SystemPrompt"/> domain model to map from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prompt"/> is null.</exception>
    /// <remarks>
    /// Maps all properties from the domain model to the ViewModel.
    /// This is the primary constructor used when loading prompts from the service.
    /// </remarks>
    public SystemPromptViewModel(SystemPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        Id = prompt.Id;
        Name = prompt.Name;
        Content = prompt.Content;
        Description = prompt.Description;
        Category = prompt.Category;
        IsBuiltIn = prompt.IsBuiltIn;
        IsDefault = prompt.IsDefault;
        UsageCount = prompt.UsageCount;
    }

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Called when <see cref="Content"/> changes to notify computed property updates.
    /// </summary>
    /// <param name="value">The new content value.</param>
    partial void OnContentChanged(string value)
    {
        // Notify computed properties that depend on content.
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(EstimatedTokenCount));
        OnPropertyChanged(nameof(ContentPreview));
    }

    /// <summary>
    /// Called when <see cref="Category"/> changes to notify computed property updates.
    /// </summary>
    /// <param name="value">The new category value.</param>
    partial void OnCategoryChanged(string value)
    {
        // Notify that the icon may have changed.
        OnPropertyChanged(nameof(CategoryIcon));
    }

    /// <summary>
    /// Called when <see cref="IsBuiltIn"/> changes to notify computed property updates.
    /// </summary>
    /// <param name="value">The new IsBuiltIn value.</param>
    partial void OnIsBuiltInChanged(bool value)
    {
        // Notify that the type label has changed.
        OnPropertyChanged(nameof(TypeLabel));
    }

    #endregion
}
