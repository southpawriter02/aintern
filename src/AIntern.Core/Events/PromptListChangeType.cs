namespace AIntern.Core.Events;

/// <summary>
/// Specifies the type of change that occurred to the system prompt list.
/// </summary>
/// <remarks>
/// <para>
/// This enum categorizes changes to the system prompt list for efficient event handling.
/// Subscribers can use this to determine if they need to react to a change.
/// </para>
/// <para>
/// Used by <see cref="PromptListChangedEventArgs"/> to indicate what
/// kind of mutation triggered the event.
/// </para>
/// <para>Added in v0.2.4b.</para>
/// </remarks>
public enum PromptListChangeType
{
    /// <summary>
    /// A new system prompt was created.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this type is set, <see cref="PromptListChangedEventArgs.AffectedPromptId"/>
    /// and <see cref="PromptListChangedEventArgs.AffectedPromptName"/> contain
    /// the ID and name of the newly created prompt.
    /// </para>
    /// </remarks>
    PromptCreated,

    /// <summary>
    /// An existing system prompt was updated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This indicates that <see cref="Interfaces.ISystemPromptService.UpdatePromptAsync"/>
    /// was called to modify an existing prompt's name, content, description, category, or tags.
    /// </para>
    /// </remarks>
    PromptUpdated,

    /// <summary>
    /// A system prompt was deleted (soft delete).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This indicates that <see cref="Interfaces.ISystemPromptService.DeletePromptAsync"/>
    /// was called. The prompt is soft-deleted (IsActive set to false).
    /// </para>
    /// <para>
    /// Built-in prompts cannot be deleted; attempting to delete one throws an exception.
    /// </para>
    /// </remarks>
    PromptDeleted,

    /// <summary>
    /// The default prompt designation changed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This indicates that <see cref="Interfaces.ISystemPromptService.SetAsDefaultAsync"/>
    /// was called. The previous default (if any) is cleared and the new prompt is marked as default.
    /// </para>
    /// </remarks>
    DefaultChanged,

    /// <summary>
    /// The prompt list was refreshed from the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This indicates a bulk refresh operation where the entire list may have changed.
    /// Subscribers should reload their prompt lists when receiving this event type.
    /// </para>
    /// </remarks>
    ListRefreshed
}
