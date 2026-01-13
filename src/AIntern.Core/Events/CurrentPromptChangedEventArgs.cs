using AIntern.Core.Models;

namespace AIntern.Core.Events;

/// <summary>
/// Event arguments raised when the currently selected system prompt changes.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised by <see cref="Interfaces.ISystemPromptService"/> whenever
/// the current prompt selection changes via <see cref="Interfaces.ISystemPromptService.SetCurrentPromptAsync"/>,
/// or when the current prompt is deleted and falls back to the default.
/// </para>
/// <para>
/// <b>Thread Safety:</b> The <see cref="NewPrompt"/> and <see cref="PreviousPrompt"/>
/// properties contain immutable domain models, so subscribers can safely access
/// them without locking.
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Update the prompt selector UI to show the new selection</description></item>
///   <item><description>Refresh conversation context when prompt changes mid-session</description></item>
///   <item><description>Log prompt changes for analytics</description></item>
///   <item><description>Sync prompt selection across multiple UI components</description></item>
/// </list>
/// <para>Added in v0.2.4b.</para>
/// </remarks>
/// <example>
/// Handling current prompt changes:
/// <code>
/// _promptService.CurrentPromptChanged += (sender, e) =>
/// {
///     if (e.NewPrompt is not null)
///     {
///         Console.WriteLine($"Switched to prompt: {e.NewPrompt.Name}");
///         if (e.PreviousPrompt is not null)
///         {
///             Console.WriteLine($"  (was: {e.PreviousPrompt.Name})");
///         }
///     }
///     else
///     {
///         Console.WriteLine("No prompt selected (using default behavior)");
///     }
///
///     // Update UI to reflect the change
///     UpdatePromptSelectorUI(e.NewPrompt);
/// };
/// </code>
/// </example>
public sealed class CurrentPromptChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the newly selected system prompt.
    /// </summary>
    /// <value>
    /// The <see cref="SystemPrompt"/> that is now selected for new conversations,
    /// or <c>null</c> if no prompt is selected (which should fall back to default behavior).
    /// </value>
    /// <remarks>
    /// <para>
    /// This is a snapshot of the prompt at the time of selection. If the prompt
    /// is later modified, this object will not reflect those changes.
    /// </para>
    /// <para>
    /// When <c>null</c>, the service will use default prompt behavior. In practice,
    /// <see cref="Interfaces.ISystemPromptService.InitializeAsync"/> ensures a default
    /// prompt is always selected, so this is typically non-null.
    /// </para>
    /// </remarks>
    public SystemPrompt? NewPrompt { get; init; }

    /// <summary>
    /// Gets the previously selected system prompt.
    /// </summary>
    /// <value>
    /// The <see cref="SystemPrompt"/> that was selected before the change,
    /// or <c>null</c> if there was no previous selection (e.g., during initialization).
    /// </value>
    /// <remarks>
    /// <para>
    /// This property is useful for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Logging the transition between prompts</description></item>
    ///   <item><description>Providing "undo" functionality</description></item>
    ///   <item><description>Comparing old and new prompts for analytics</description></item>
    /// </list>
    /// <para>
    /// During initialization (<see cref="Interfaces.ISystemPromptService.InitializeAsync"/>),
    /// this will be <c>null</c> since there is no prior selection.
    /// </para>
    /// </remarks>
    public SystemPrompt? PreviousPrompt { get; init; }
}
