using AIntern.Core.Events;
using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Service for managing system prompts with CRUD operations, template support, and event notification.
/// </summary>
/// <remarks>
/// <para>
/// This service provides centralized management of system prompts with:
/// </para>
/// <list type="bullet">
///   <item><description><b>Current Prompt:</b> The selected prompt for new conversations</description></item>
///   <item><description><b>CRUD Operations:</b> Create, read, update, delete prompts</description></item>
///   <item><description><b>Template Support:</b> Built-in prompts that serve as starting points</description></item>
///   <item><description><b>Event Notification:</b> Subscribers notified of list and selection changes</description></item>
///   <item><description><b>Persistence:</b> Current prompt ID saved to settings.json</description></item>
///   <item><description><b>Soft Delete:</b> Deleted prompts are deactivated, not removed</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> All async operations are protected by a semaphore.
/// Event handlers receive immutable data safe to access without locking.
/// </para>
/// <para>
/// <b>Change Detection:</b> Events only fire when values actually change.
/// Operations that result in no change are logged as [SKIP] but do not fire events.
/// </para>
/// <para>
/// <b>Initialization:</b> Call <see cref="InitializeAsync"/> during application startup
/// after DI is configured. This loads the last-selected prompt from settings.json.
/// </para>
/// <para>Added in v0.2.4b.</para>
/// </remarks>
/// <example>
/// Subscribing to prompt changes:
/// <code>
/// _promptService.PromptListChanged += (s, e) =>
/// {
///     Console.WriteLine($"List changed: {e.ChangeType} - {e.AffectedPromptName}");
///     RefreshPromptList();
/// };
///
/// _promptService.CurrentPromptChanged += (s, e) =>
/// {
///     Console.WriteLine($"Now using: {e.NewPrompt?.Name}");
///     UpdatePromptSelector(e.NewPrompt);
/// };
/// </code>
/// </example>
/// <example>
/// Creating and using prompts:
/// <code>
/// // Create a new prompt
/// var prompt = await _promptService.CreatePromptAsync(
///     "Code Reviewer",
///     "You are a senior code reviewer. Provide detailed feedback.",
///     "Reviews code for bugs, style, and best practices.",
///     "Code");
///
/// // Set as current prompt
/// await _promptService.SetCurrentPromptAsync(prompt.Id);
///
/// // Create from template
/// var customPrompt = await _promptService.CreateFromTemplateAsync(
///     SystemPromptTemplates.DefaultAssistantId,
///     "My Custom Assistant");
/// </code>
/// </example>
public interface ISystemPromptService
{
    #region Properties

    /// <summary>
    /// Gets the currently selected system prompt for new conversations.
    /// </summary>
    /// <value>
    /// The current <see cref="SystemPrompt"/> instance, or <c>null</c> if no prompt is selected.
    /// </value>
    /// <remarks>
    /// <para>
    /// This prompt is used when creating new conversations. The selection persists
    /// across application restarts via settings.json.
    /// </para>
    /// <para>
    /// After <see cref="InitializeAsync"/> completes, this is typically non-null
    /// as the service falls back to the default prompt if none is persisted.
    /// </para>
    /// <para>
    /// The returned object is a snapshot. Subscribe to <see cref="CurrentPromptChanged"/>
    /// for updates when the selection changes.
    /// </para>
    /// </remarks>
    SystemPrompt? CurrentPrompt { get; }

    #endregion

    #region Query Operations

    /// <summary>
    /// Retrieves all active system prompts.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all active prompts, ordered by category then by name.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This returns both built-in templates and user-created prompts.
    /// Soft-deleted prompts (IsActive = false) are excluded.
    /// </para>
    /// <para>
    /// For filtered results, use <see cref="GetUserPromptsAsync"/> or
    /// <see cref="GetTemplatesAsync"/>.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<SystemPrompt>> GetAllPromptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all user-created (non-built-in) prompts.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of active user-created prompts, ordered by name.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Filters to prompts where IsBuiltIn = false and IsActive = true.
    /// These are prompts created by the user via <see cref="CreatePromptAsync"/>
    /// or <see cref="CreateFromTemplateAsync"/>.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<SystemPrompt>> GetUserPromptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all built-in template prompts.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of active built-in prompts, ordered by name.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Filters to prompts where IsBuiltIn = true and IsActive = true.
    /// Built-in prompts are seeded during database initialization and
    /// cannot be deleted (only soft-deleted via <see cref="DeletePromptAsync"/>).
    /// </para>
    /// <para>
    /// Templates can be used as starting points via <see cref="CreateFromTemplateAsync"/>.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<SystemPrompt>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a system prompt by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the prompt.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The prompt if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns both active and inactive prompts.
    /// Use this when you need to access a specific prompt by ID,
    /// such as when restoring a persisted selection.
    /// </para>
    /// </remarks>
    Task<SystemPrompt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the default system prompt.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The prompt marked as default, or <c>null</c> if no default is set.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The default prompt is used as a fallback during <see cref="InitializeAsync"/>
    /// when no persisted selection is found.
    /// </para>
    /// <para>
    /// Only one prompt can be marked as default at a time.
    /// Use <see cref="SetAsDefaultAsync"/> to change the default.
    /// </para>
    /// </remarks>
    Task<SystemPrompt?> GetDefaultPromptAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches prompts by name or description.
    /// </summary>
    /// <param name="searchTerm">The search term to match.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of matching active prompts.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The search is case-insensitive and matches any substring of
    /// the name or description fields. Only active prompts are returned.
    /// </para>
    /// <para>
    /// Returns an empty list if the search term is null or whitespace.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<SystemPrompt>> SearchPromptsAsync(string searchTerm, CancellationToken cancellationToken = default);

    #endregion

    #region Mutation Operations

    /// <summary>
    /// Creates a new system prompt.
    /// </summary>
    /// <param name="name">The display name for the prompt (must be unique).</param>
    /// <param name="content">The system prompt text sent to the model.</param>
    /// <param name="description">Optional description of the prompt's purpose.</param>
    /// <param name="category">Optional category for organization (defaults to "General").</param>
    /// <param name="tags">Optional tags for searchability.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The newly created prompt.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if name or content is null or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a prompt with the same name already exists.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Creates a user-defined prompt (IsBuiltIn = false).
    /// The prompt is immediately active and can be selected.
    /// </para>
    /// <para>
    /// Fires <see cref="PromptListChanged"/> with <see cref="PromptListChangeType.PromptCreated"/>.
    /// </para>
    /// </remarks>
    Task<SystemPrompt> CreatePromptAsync(
        string name,
        string content,
        string? description = null,
        string? category = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new prompt from an existing template.
    /// </summary>
    /// <param name="templateId">The ID of the template prompt to clone.</param>
    /// <param name="newName">
    /// Optional name for the new prompt. If null or already exists,
    /// a unique name is generated (e.g., "Template Name (1)").
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The newly created prompt.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the template is not found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Clones the template's content, description, category, and tags.
    /// The new prompt is a user-created prompt (IsBuiltIn = false)
    /// that can be freely modified or deleted.
    /// </para>
    /// <para>
    /// If the specified name already exists, the service automatically
    /// appends a number suffix (e.g., "My Prompt (1)", "My Prompt (2)").
    /// </para>
    /// <para>
    /// Fires <see cref="PromptListChanged"/> with <see cref="PromptListChangeType.PromptCreated"/>.
    /// </para>
    /// </remarks>
    Task<SystemPrompt> CreateFromTemplateAsync(
        Guid templateId,
        string? newName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing system prompt.
    /// </summary>
    /// <param name="id">The ID of the prompt to update.</param>
    /// <param name="name">New name, or null to keep current.</param>
    /// <param name="content">New content, or null to keep current.</param>
    /// <param name="description">New description, or null to keep current.</param>
    /// <param name="category">New category, or null to keep current.</param>
    /// <param name="tags">New tags, or null to keep current.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated prompt.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the prompt is not found or if the new name conflicts with an existing prompt.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Only non-null parameters are updated. Pass null to preserve existing values.
    /// Both built-in and user-created prompts can be updated.
    /// </para>
    /// <para>
    /// If the updated prompt is the current prompt, the <see cref="CurrentPrompt"/>
    /// property is refreshed but <see cref="CurrentPromptChanged"/> is not fired
    /// (use <see cref="PromptListChanged"/> to detect content changes).
    /// </para>
    /// <para>
    /// Fires <see cref="PromptListChanged"/> with <see cref="PromptListChangeType.PromptUpdated"/>
    /// if any values changed.
    /// </para>
    /// </remarks>
    Task<SystemPrompt> UpdatePromptAsync(
        Guid id,
        string? name = null,
        string? content = null,
        string? description = null,
        string? category = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a system prompt.
    /// </summary>
    /// <param name="id">The ID of the prompt to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the prompt is built-in (IsBuiltIn = true) or not found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Soft-deletes the prompt by setting IsActive = false.
    /// The prompt is hidden from queries but can be restored.
    /// </para>
    /// <para>
    /// Built-in prompts cannot be deleted. Attempting to delete one
    /// throws an <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// If the deleted prompt was the <see cref="CurrentPrompt"/>,
    /// the service automatically selects the default prompt and fires
    /// <see cref="CurrentPromptChanged"/>.
    /// </para>
    /// <para>
    /// Fires <see cref="PromptListChanged"/> with <see cref="PromptListChangeType.PromptDeleted"/>.
    /// </para>
    /// </remarks>
    Task DeletePromptAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates an existing prompt.
    /// </summary>
    /// <param name="id">The ID of the prompt to duplicate.</param>
    /// <param name="newName">
    /// Optional name for the duplicate. If null or already exists,
    /// a unique name is generated (e.g., "Original Name (Copy)").
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The newly created duplicate prompt.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the source prompt is not found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Creates a copy of the prompt with a new ID and name.
    /// The duplicate is a user-created prompt (IsBuiltIn = false).
    /// </para>
    /// <para>
    /// Usage count and timestamps are reset for the duplicate.
    /// The duplicate is not marked as default even if the source was.
    /// </para>
    /// <para>
    /// Fires <see cref="PromptListChanged"/> with <see cref="PromptListChangeType.PromptCreated"/>.
    /// </para>
    /// </remarks>
    Task<SystemPrompt> DuplicatePromptAsync(
        Guid id,
        string? newName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a prompt as the default for new conversations.
    /// </summary>
    /// <param name="id">The ID of the prompt to set as default.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the prompt is not found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Clears the default flag from the previous default (if any)
    /// and sets it on the specified prompt atomically.
    /// </para>
    /// <para>
    /// The default prompt is used as a fallback during <see cref="InitializeAsync"/>
    /// when no persisted selection is found.
    /// </para>
    /// <para>
    /// Fires <see cref="PromptListChanged"/> with <see cref="PromptListChangeType.DefaultChanged"/>.
    /// </para>
    /// </remarks>
    Task SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the current prompt selection for new conversations.
    /// </summary>
    /// <param name="id">
    /// The ID of the prompt to select, or <c>null</c> to clear the selection
    /// (falls back to default prompt).
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the specified prompt is not found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Updates the <see cref="CurrentPrompt"/> property and persists
    /// the selection to settings.json for restoration on next startup.
    /// </para>
    /// <para>
    /// Increments the prompt's usage count for analytics.
    /// </para>
    /// <para>
    /// If the same prompt is already selected, this is a no-op and
    /// no event is fired.
    /// </para>
    /// <para>
    /// Fires <see cref="CurrentPromptChanged"/> if the selection actually changed.
    /// </para>
    /// </remarks>
    Task SetCurrentPromptAsync(Guid? id, CancellationToken cancellationToken = default);

    #endregion

    #region Utility Operations

    /// <summary>
    /// Formats a prompt for inclusion in the conversation context.
    /// </summary>
    /// <param name="prompt">The prompt to format.</param>
    /// <returns>The formatted prompt text ready for the model.</returns>
    /// <remarks>
    /// <para>
    /// This method provides a hook for model-specific formatting.
    /// Currently returns the prompt content as-is, but may be extended
    /// to add model-specific markers or transformations.
    /// </para>
    /// <para>
    /// Returns an empty string if prompt is null.
    /// </para>
    /// </remarks>
    string FormatPromptForContext(SystemPrompt? prompt);

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initializes the service by loading the last selected prompt.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Call this once during application startup after DI is configured.
    /// This method:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Reads CurrentSystemPromptId from settings.json</description></item>
    ///   <item><description>Loads and sets the prompt if found and active</description></item>
    ///   <item><description>Falls back to the default prompt if not found</description></item>
    ///   <item><description>Falls back to the first available prompt if no default exists</description></item>
    /// </list>
    /// <para>
    /// Does not fire <see cref="CurrentPromptChanged"/> during initialization
    /// to avoid triggering subscribers before they're ready.
    /// </para>
    /// <para>
    /// Safe to call multiple times; subsequent calls are no-ops if already initialized.
    /// </para>
    /// </remarks>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Events

    /// <summary>
    /// Raised when the system prompt list is modified.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event fires for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Prompt creation (<see cref="PromptListChangeType.PromptCreated"/>)</description></item>
    ///   <item><description>Prompt updates (<see cref="PromptListChangeType.PromptUpdated"/>)</description></item>
    ///   <item><description>Prompt deletion (<see cref="PromptListChangeType.PromptDeleted"/>)</description></item>
    ///   <item><description>Default designation changes (<see cref="PromptListChangeType.DefaultChanged"/>)</description></item>
    ///   <item><description>List refresh operations (<see cref="PromptListChangeType.ListRefreshed"/>)</description></item>
    /// </list>
    /// <para>
    /// Subscribers should refresh their prompt lists when receiving this event.
    /// The <see cref="PromptListChangedEventArgs.AffectedPromptId"/> and
    /// <see cref="PromptListChangedEventArgs.AffectedPromptName"/> properties
    /// identify which prompt was affected (except for ListRefreshed).
    /// </para>
    /// </remarks>
    event EventHandler<PromptListChangedEventArgs>? PromptListChanged;

    /// <summary>
    /// Raised when the current prompt selection changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event fires when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="SetCurrentPromptAsync"/> is called with a different prompt</description></item>
    ///   <item><description>The current prompt is deleted and falls back to default</description></item>
    /// </list>
    /// <para>
    /// Does not fire during <see cref="InitializeAsync"/> to avoid
    /// triggering subscribers before the application is fully loaded.
    /// </para>
    /// <para>
    /// Subscribers receive both the new and previous prompts via
    /// <see cref="CurrentPromptChangedEventArgs"/> for comparison.
    /// </para>
    /// </remarks>
    event EventHandler<CurrentPromptChangedEventArgs>? CurrentPromptChanged;

    #endregion
}
