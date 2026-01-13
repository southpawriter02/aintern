using AIntern.Core.Models;

namespace AIntern.Core.Events;

/// <summary>
/// Event arguments raised when inference settings are modified.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised by <see cref="Interfaces.IInferenceSettingsService"/> whenever
/// any inference parameter changes. Subscribers can react to update UI elements,
/// apply settings to running inference, or log changes.
/// </para>
/// <para>
/// <b>Thread Safety:</b> The <see cref="NewSettings"/> property contains a clone
/// of the current settings, so subscribers can safely access it without locking.
/// </para>
/// <para>
/// <b>Change Types:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="InferenceSettingsChangeType.ParameterChanged"/>: Single parameter modified</description></item>
///   <item><description><see cref="InferenceSettingsChangeType.PresetApplied"/>: Preset loaded</description></item>
///   <item><description><see cref="InferenceSettingsChangeType.ResetToDefaults"/>: Reset to defaults</description></item>
///   <item><description><see cref="InferenceSettingsChangeType.AllChanged"/>: Multiple parameters via UpdateAll</description></item>
/// </list>
/// </remarks>
/// <example>
/// Handling settings changes:
/// <code>
/// _settingsService.SettingsChanged += (sender, e) =>
/// {
///     switch (e.ChangeType)
///     {
///         case InferenceSettingsChangeType.ParameterChanged:
///             Console.WriteLine($"{e.ChangedParameter} changed to {GetValue(e.NewSettings, e.ChangedParameter)}");
///             break;
///         case InferenceSettingsChangeType.PresetApplied:
///             Console.WriteLine("Preset applied - all settings updated");
///             break;
///         case InferenceSettingsChangeType.ResetToDefaults:
///             Console.WriteLine("Settings reset to defaults");
///             break;
///         case InferenceSettingsChangeType.AllChanged:
///             Console.WriteLine("Multiple settings changed");
///             break;
///     }
/// };
/// </code>
/// </example>
public sealed class InferenceSettingsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new inference settings after the change.
    /// </summary>
    /// <value>
    /// A cloned copy of the current settings. Safe to access without locking.
    /// </value>
    /// <remarks>
    /// <para>
    /// This is always a deep copy of the settings at the time the event was raised.
    /// Modifying this object will not affect the service's internal state.
    /// </para>
    /// </remarks>
    public required InferenceSettings NewSettings { get; init; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    /// <value>
    /// An <see cref="InferenceSettingsChangeType"/> indicating what kind of change happened.
    /// </value>
    /// <remarks>
    /// <para>
    /// Use this to determine how to handle the change:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>ParameterChanged:</b> Check <see cref="ChangedParameter"/> for which one</description></item>
    ///   <item><description><b>PresetApplied:</b> All values may have changed</description></item>
    ///   <item><description><b>ResetToDefaults:</b> Values reset to default preset</description></item>
    ///   <item><description><b>AllChanged:</b> Multiple values changed via UpdateAll</description></item>
    /// </list>
    /// </remarks>
    public required InferenceSettingsChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the name of the parameter that changed, if applicable.
    /// </summary>
    /// <value>
    /// The parameter name (e.g., "Temperature", "MaxTokens") for single-parameter changes;
    /// <c>null</c> for bulk changes like preset application.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property is only set when <see cref="ChangeType"/> is
    /// <see cref="InferenceSettingsChangeType.ParameterChanged"/>.
    /// </para>
    /// <para>
    /// Possible values: "Temperature", "TopP", "TopK", "RepetitionPenalty",
    /// "MaxTokens", "ContextSize", "Seed".
    /// </para>
    /// </remarks>
    public string? ChangedParameter { get; init; }
}

/// <summary>
/// Specifies the type of inference settings change.
/// </summary>
/// <remarks>
/// <para>
/// This enum categorizes changes to inference settings for efficient event handling.
/// Subscribers can use this to determine if they need to react to a change.
/// </para>
/// </remarks>
public enum InferenceSettingsChangeType
{
    /// <summary>
    /// A single parameter was modified via an Update method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this type is set, <see cref="InferenceSettingsChangedEventArgs.ChangedParameter"/>
    /// contains the name of the parameter that changed.
    /// </para>
    /// </remarks>
    ParameterChanged,

    /// <summary>
    /// A preset was loaded, replacing all current settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This indicates that <see cref="Interfaces.IInferenceSettingsService.ApplyPresetAsync"/>
    /// was called and all settings may have changed.
    /// </para>
    /// </remarks>
    PresetApplied,

    /// <summary>
    /// Settings were reset to defaults via ResetToDefaultsAsync.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This indicates that <see cref="Interfaces.IInferenceSettingsService.ResetToDefaultsAsync"/>
    /// was called, which applies the default preset (Balanced).
    /// </para>
    /// </remarks>
    ResetToDefaults,

    /// <summary>
    /// Multiple parameters changed at once via UpdateAll.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This indicates that <see cref="Interfaces.IInferenceSettingsService.UpdateAll"/>
    /// was called with a new <see cref="InferenceSettings"/> instance.
    /// </para>
    /// </remarks>
    AllChanged
}
