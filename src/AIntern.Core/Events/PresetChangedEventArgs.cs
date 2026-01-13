using AIntern.Core.Models;

namespace AIntern.Core.Events;

/// <summary>
/// Event arguments raised when preset operations occur.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised by <see cref="Interfaces.IInferenceSettingsService"/> for
/// preset CRUD operations and preset activation. Subscribers can react to update
/// preset lists in the UI, sync state, or log changes.
/// </para>
/// <para>
/// <b>Thread Safety:</b> The <see cref="NewPreset"/> and <see cref="PreviousPreset"/>
/// properties contain clones of the preset data, so subscribers can safely access
/// them without locking.
/// </para>
/// <para>
/// <b>Change Types:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="PresetChangeType.Applied"/>: A preset was activated</description></item>
///   <item><description><see cref="PresetChangeType.Created"/>: A new user preset was created</description></item>
///   <item><description><see cref="PresetChangeType.Updated"/>: An existing preset was modified</description></item>
///   <item><description><see cref="PresetChangeType.Deleted"/>: A preset was removed</description></item>
///   <item><description><see cref="PresetChangeType.DefaultChanged"/>: The default preset was changed</description></item>
/// </list>
/// </remarks>
/// <example>
/// Handling preset changes:
/// <code>
/// _settingsService.PresetChanged += (sender, e) =>
/// {
///     switch (e.ChangeType)
///     {
///         case PresetChangeType.Applied:
///             Console.WriteLine($"Now using: {e.NewPreset?.Name}");
///             UpdateActivePresetDisplay(e.NewPreset);
///             break;
///         case PresetChangeType.Created:
///             Console.WriteLine($"New preset: {e.NewPreset?.Name}");
///             RefreshPresetList();
///             break;
///         case PresetChangeType.Updated:
///             Console.WriteLine($"Updated: {e.NewPreset?.Name}");
///             RefreshPresetList();
///             break;
///         case PresetChangeType.Deleted:
///             Console.WriteLine($"Deleted: {e.PreviousPreset?.Name}");
///             RefreshPresetList();
///             break;
///         case PresetChangeType.DefaultChanged:
///             Console.WriteLine($"New default: {e.NewPreset?.Name}");
///             break;
///     }
/// };
/// </code>
/// </example>
public sealed class PresetChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the preset after the operation, if applicable.
    /// </summary>
    /// <value>
    /// The new/updated preset for <see cref="PresetChangeType.Applied"/>,
    /// <see cref="PresetChangeType.Created"/>, <see cref="PresetChangeType.Updated"/>,
    /// and <see cref="PresetChangeType.DefaultChanged"/> operations;
    /// <c>null</c> for <see cref="PresetChangeType.Deleted"/> operations.
    /// </value>
    /// <remarks>
    /// <para>
    /// This is a clone of the preset at the time the event was raised.
    /// Modifying this object will not affect the service's internal state.
    /// </para>
    /// </remarks>
    public InferencePreset? NewPreset { get; init; }

    /// <summary>
    /// Gets the previous preset state, if applicable.
    /// </summary>
    /// <value>
    /// The previous active preset for <see cref="PresetChangeType.Applied"/> operations;
    /// the deleted preset for <see cref="PresetChangeType.Deleted"/> operations;
    /// the previous default for <see cref="PresetChangeType.DefaultChanged"/> operations;
    /// <c>null</c> otherwise.
    /// </value>
    /// <remarks>
    /// <para>
    /// This allows subscribers to compare old and new states or to perform
    /// cleanup operations when a preset is deleted.
    /// </para>
    /// </remarks>
    public InferencePreset? PreviousPreset { get; init; }

    /// <summary>
    /// Gets the type of preset operation that occurred.
    /// </summary>
    /// <value>
    /// A <see cref="PresetChangeType"/> indicating what kind of operation happened.
    /// </value>
    /// <remarks>
    /// <para>
    /// Use this to determine how to handle the change:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Applied:</b> Update active preset display</description></item>
    ///   <item><description><b>Created:</b> Add to preset list</description></item>
    ///   <item><description><b>Updated:</b> Refresh preset in list</description></item>
    ///   <item><description><b>Deleted:</b> Remove from preset list</description></item>
    ///   <item><description><b>DefaultChanged:</b> Update default indicator</description></item>
    /// </list>
    /// </remarks>
    public required PresetChangeType ChangeType { get; init; }
}

/// <summary>
/// Specifies the type of preset change operation.
/// </summary>
/// <remarks>
/// <para>
/// This enum categorizes preset operations for efficient event handling.
/// Subscribers can use this to determine if they need to react to a change.
/// </para>
/// </remarks>
public enum PresetChangeType
{
    /// <summary>
    /// A preset was loaded as the active configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this type is set:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="PresetChangedEventArgs.NewPreset"/> contains the newly active preset</description></item>
    ///   <item><description><see cref="PresetChangedEventArgs.PreviousPreset"/> contains the previously active preset (may be null)</description></item>
    /// </list>
    /// </remarks>
    Applied,

    /// <summary>
    /// A new user preset was created via <see cref="Interfaces.IInferenceSettingsService.SaveAsPresetAsync"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this type is set:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="PresetChangedEventArgs.NewPreset"/> contains the newly created preset</description></item>
    ///   <item><description><see cref="PresetChangedEventArgs.PreviousPreset"/> is null</description></item>
    /// </list>
    /// </remarks>
    Created,

    /// <summary>
    /// An existing preset was updated with new settings via <see cref="Interfaces.IInferenceSettingsService.UpdatePresetAsync"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this type is set:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="PresetChangedEventArgs.NewPreset"/> contains the updated preset</description></item>
    ///   <item><description><see cref="PresetChangedEventArgs.PreviousPreset"/> contains the preset before the update</description></item>
    /// </list>
    /// </remarks>
    Updated,

    /// <summary>
    /// A preset was deleted via <see cref="Interfaces.IInferenceSettingsService.DeletePresetAsync"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this type is set:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="PresetChangedEventArgs.NewPreset"/> is null</description></item>
    ///   <item><description><see cref="PresetChangedEventArgs.PreviousPreset"/> contains the deleted preset</description></item>
    /// </list>
    /// </remarks>
    Deleted,

    /// <summary>
    /// The default preset was changed via <see cref="Interfaces.IInferenceSettingsService.SetDefaultPresetAsync"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this type is set:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="PresetChangedEventArgs.NewPreset"/> contains the new default preset</description></item>
    ///   <item><description><see cref="PresetChangedEventArgs.PreviousPreset"/> contains the previous default preset</description></item>
    /// </list>
    /// </remarks>
    DefaultChanged
}
