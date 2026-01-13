namespace AIntern.Core.Events;

/// <summary>
/// Event arguments raised when the system prompt list is modified.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised by <see cref="Interfaces.ISystemPromptService"/> whenever
/// a prompt is created, updated, deleted, or when the default designation changes.
/// Subscribers can react to update UI elements, refresh prompt lists, or log changes.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All properties are immutable after construction,
/// so subscribers can safely access them without locking.
/// </para>
/// <para>
/// <b>Change Types:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="PromptListChangeType.PromptCreated"/>: New prompt added</description></item>
///   <item><description><see cref="PromptListChangeType.PromptUpdated"/>: Existing prompt modified</description></item>
///   <item><description><see cref="PromptListChangeType.PromptDeleted"/>: Prompt removed (soft delete)</description></item>
///   <item><description><see cref="PromptListChangeType.DefaultChanged"/>: Default prompt designation changed</description></item>
///   <item><description><see cref="PromptListChangeType.ListRefreshed"/>: Entire list refreshed from database</description></item>
/// </list>
/// <para>Added in v0.2.4b.</para>
/// </remarks>
/// <example>
/// Handling prompt list changes:
/// <code>
/// _promptService.PromptListChanged += (sender, e) =>
/// {
///     switch (e.ChangeType)
///     {
///         case PromptListChangeType.PromptCreated:
///             Console.WriteLine($"New prompt created: {e.AffectedPromptName} ({e.AffectedPromptId})");
///             break;
///         case PromptListChangeType.PromptUpdated:
///             Console.WriteLine($"Prompt updated: {e.AffectedPromptName}");
///             break;
///         case PromptListChangeType.PromptDeleted:
///             Console.WriteLine($"Prompt deleted: {e.AffectedPromptId}");
///             break;
///         case PromptListChangeType.DefaultChanged:
///             Console.WriteLine($"New default prompt: {e.AffectedPromptName}");
///             break;
///         case PromptListChangeType.ListRefreshed:
///             Console.WriteLine("Prompt list refreshed from database");
///             break;
///     }
///     // Refresh the prompt list in UI
///     RefreshPromptListAsync();
/// };
/// </code>
/// </example>
public sealed class PromptListChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of change that occurred to the prompt list.
    /// </summary>
    /// <value>
    /// A <see cref="PromptListChangeType"/> indicating what kind of mutation happened.
    /// </value>
    /// <remarks>
    /// <para>
    /// Use this to determine how to handle the change:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>PromptCreated:</b> Add the new prompt to cached lists</description></item>
    ///   <item><description><b>PromptUpdated:</b> Update the cached prompt with new values</description></item>
    ///   <item><description><b>PromptDeleted:</b> Remove the prompt from cached lists</description></item>
    ///   <item><description><b>DefaultChanged:</b> Update default indicators in UI</description></item>
    ///   <item><description><b>ListRefreshed:</b> Reload entire prompt list from service</description></item>
    /// </list>
    /// </remarks>
    public required PromptListChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the unique identifier of the affected prompt, if applicable.
    /// </summary>
    /// <value>
    /// The GUID of the prompt that was created, updated, deleted, or set as default;
    /// <c>null</c> for <see cref="PromptListChangeType.ListRefreshed"/> events.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property is set for all change types except <see cref="PromptListChangeType.ListRefreshed"/>,
    /// which affects the entire list rather than a specific prompt.
    /// </para>
    /// <para>
    /// For <see cref="PromptListChangeType.PromptDeleted"/>, this contains the ID of the
    /// deleted prompt even though it may no longer be retrievable from the service.
    /// </para>
    /// </remarks>
    public Guid? AffectedPromptId { get; init; }

    /// <summary>
    /// Gets the name of the affected prompt, if applicable.
    /// </summary>
    /// <value>
    /// The display name of the prompt that was created, updated, deleted, or set as default;
    /// <c>null</c> for <see cref="PromptListChangeType.ListRefreshed"/> events.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides a human-readable identifier for logging and UI updates
    /// without requiring an additional lookup.
    /// </para>
    /// <para>
    /// For <see cref="PromptListChangeType.PromptUpdated"/>, this contains the name
    /// <em>after</em> the update, which may differ from the original name if the
    /// name was changed.
    /// </para>
    /// </remarks>
    public string? AffectedPromptName { get; init; }
}
