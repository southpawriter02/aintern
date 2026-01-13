using AIntern.Core.Events;
using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Service for managing inference settings with preset support and event notification.
/// </summary>
/// <remarks>
/// <para>
/// This service provides centralized management of inference parameters with:
/// </para>
/// <list type="bullet">
///   <item><description><b>Live Settings:</b> In-memory settings that affect all inference operations</description></item>
///   <item><description><b>Preset Support:</b> Load, save, and manage named configurations</description></item>
///   <item><description><b>Event Notification:</b> Subscribers notified of all changes</description></item>
///   <item><description><b>Persistence:</b> Active preset ID saved to settings.json</description></item>
///   <item><description><b>Validation:</b> All updates clamped to valid ranges</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> All async preset operations are protected by a semaphore.
/// Parameter updates are synchronous and thread-safe.
/// </para>
/// <para>
/// <b>Change Detection:</b> Events only fire when values actually change.
/// Float comparisons use epsilon (0.001) to avoid floating-point issues.
/// </para>
/// <para>
/// <b>Initialization:</b> Call <see cref="InitializeAsync"/> during application startup
/// after DI is configured. This loads the last-used preset from settings.json.
/// </para>
/// </remarks>
/// <example>
/// Subscribing to settings changes:
/// <code>
/// _settingsService.SettingsChanged += (s, e) =>
/// {
///     Console.WriteLine($"Changed: {e.ChangedParameter} to {e.NewSettings.Temperature}");
/// };
///
/// _settingsService.UpdateTemperature(1.2f); // Fires SettingsChanged
/// _settingsService.UpdateTemperature(1.2f); // No event (same value)
/// </code>
/// </example>
/// <example>
/// Applying and saving presets:
/// <code>
/// // Apply a built-in preset
/// await _settingsService.ApplyPresetAsync(InferencePreset.PrecisePresetId);
///
/// // Modify and save as new preset
/// _settingsService.UpdateTemperature(0.3f);
/// var newPreset = await _settingsService.SaveAsPresetAsync(
///     "My Code Review",
///     "Custom settings for code review",
///     "Code");
/// </code>
/// </example>
public interface IInferenceSettingsService
{
    #region Properties

    /// <summary>
    /// Gets the current live inference settings.
    /// </summary>
    /// <value>
    /// The current <see cref="InferenceSettings"/> instance.
    /// This is the authoritative source for all inference operations.
    /// </value>
    /// <remarks>
    /// <para>
    /// Changes to this object are reflected immediately in inference operations.
    /// Do not modify the returned object directly; use the Update methods instead.
    /// </para>
    /// <para>
    /// The returned object is the live instance, not a clone. For thread-safe
    /// access, subscribe to <see cref="SettingsChanged"/> events which provide
    /// cloned settings.
    /// </para>
    /// </remarks>
    InferenceSettings CurrentSettings { get; }

    /// <summary>
    /// Gets the currently active preset, or null if using custom settings.
    /// </summary>
    /// <value>
    /// The active <see cref="InferencePreset"/> if settings match a preset;
    /// otherwise <c>null</c> indicating custom/modified settings.
    /// </value>
    /// <remarks>
    /// <para>
    /// This becomes null when any parameter is modified after loading a preset,
    /// indicating the user has customized settings beyond the preset values.
    /// </para>
    /// <para>
    /// Use <see cref="HasUnsavedChanges"/> to detect if settings differ from
    /// the last-applied preset without losing the preset reference.
    /// </para>
    /// </remarks>
    InferencePreset? ActivePreset { get; }

    /// <summary>
    /// Gets whether current settings differ from the active preset.
    /// </summary>
    /// <value>
    /// <c>true</c> if settings have been modified since the preset was loaded;
    /// <c>false</c> if settings match the preset or no preset is active.
    /// </value>
    /// <remarks>
    /// <para>
    /// Used by the UI to show an "unsaved changes" indicator and prompt
    /// users to save their customizations as a new preset.
    /// </para>
    /// <para>
    /// Float comparisons use epsilon (0.001) to avoid floating-point issues.
    /// </para>
    /// </remarks>
    bool HasUnsavedChanges { get; }

    #endregion

    #region Parameter Updates

    /// <summary>
    /// Updates the temperature parameter with clamping.
    /// </summary>
    /// <param name="value">The new temperature value (will be clamped to 0.0-2.0).</param>
    /// <remarks>
    /// <para>
    /// Temperature controls the randomness of token selection:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Low (0.0-0.3):</b> More focused, deterministic outputs</description></item>
    ///   <item><description><b>Medium (0.5-0.9):</b> Balanced creativity and coherence</description></item>
    ///   <item><description><b>High (1.0-2.0):</b> More creative, potentially unpredictable</description></item>
    /// </list>
    /// <para>
    /// Fires <see cref="SettingsChanged"/> if the value differs from current.
    /// Values outside the valid range are clamped without error.
    /// </para>
    /// </remarks>
    void UpdateTemperature(float value);

    /// <summary>
    /// Updates the Top-P (nucleus sampling) parameter with clamping.
    /// </summary>
    /// <param name="value">The new Top-P value (will be clamped to 0.0-1.0).</param>
    /// <remarks>
    /// <para>
    /// Top-P controls diversity by sampling from the smallest set of tokens
    /// whose cumulative probability exceeds P:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>0.9:</b> Considers tokens in the top 90% probability mass</description></item>
    ///   <item><description><b>1.0:</b> Considers all tokens (disabled)</description></item>
    /// </list>
    /// <para>
    /// Fires <see cref="SettingsChanged"/> if the value differs from current.
    /// </para>
    /// </remarks>
    void UpdateTopP(float value);

    /// <summary>
    /// Updates the Top-K parameter with clamping.
    /// </summary>
    /// <param name="value">The new Top-K value (will be clamped to 0-100).</param>
    /// <remarks>
    /// <para>
    /// Top-K limits selection to the K most likely next tokens:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Lower values:</b> More focused outputs</description></item>
    ///   <item><description><b>Higher values:</b> More diversity</description></item>
    ///   <item><description><b>0:</b> Disabled (no limit)</description></item>
    /// </list>
    /// <para>
    /// Fires <see cref="SettingsChanged"/> if the value differs from current.
    /// </para>
    /// </remarks>
    void UpdateTopK(int value);

    /// <summary>
    /// Updates the repetition penalty parameter with clamping.
    /// </summary>
    /// <param name="value">The new penalty value (will be clamped to 1.0-2.0).</param>
    /// <remarks>
    /// <para>
    /// The repetition penalty discourages repeating tokens:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>1.0:</b> No penalty (disabled)</description></item>
    ///   <item><description><b>1.1:</b> Light penalty (recommended)</description></item>
    ///   <item><description><b>1.5+:</b> Strong penalty (may affect coherence)</description></item>
    /// </list>
    /// <para>
    /// Fires <see cref="SettingsChanged"/> if the value differs from current.
    /// </para>
    /// </remarks>
    void UpdateRepetitionPenalty(float value);

    /// <summary>
    /// Updates the max tokens parameter with clamping.
    /// </summary>
    /// <param name="value">The new max tokens value (will be clamped to 64-8192).</param>
    /// <remarks>
    /// <para>
    /// Limits the length of generated responses:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>512-1024:</b> Short responses</description></item>
    ///   <item><description><b>2048-4096:</b> Medium responses</description></item>
    ///   <item><description><b>4096-8192:</b> Long responses</description></item>
    /// </list>
    /// <para>
    /// Fires <see cref="SettingsChanged"/> if the value differs from current.
    /// </para>
    /// </remarks>
    void UpdateMaxTokens(int value);

    /// <summary>
    /// Updates the context size parameter with clamping.
    /// </summary>
    /// <param name="value">The new context size (will be clamped to 512-32768).</param>
    /// <remarks>
    /// <para>
    /// Total tokens available for prompt plus response:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>2048-4096:</b> Standard conversations</description></item>
    ///   <item><description><b>8192-16384:</b> Extended conversations</description></item>
    ///   <item><description><b>32768:</b> Very long documents</description></item>
    /// </list>
    /// <para>
    /// Must not exceed the model's maximum context length.
    /// Fires <see cref="SettingsChanged"/> if the value differs from current.
    /// </para>
    /// </remarks>
    void UpdateContextSize(uint value);

    /// <summary>
    /// Updates the random seed parameter.
    /// </summary>
    /// <param name="value">The new seed value (-1 for random, 0+ for reproducible).</param>
    /// <remarks>
    /// <para>
    /// The seed controls random number generation:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>-1:</b> Use a random seed each generation (non-reproducible)</description></item>
    ///   <item><description><b>0+:</b> Use the specified seed for reproducible output</description></item>
    /// </list>
    /// <para>
    /// Fires <see cref="SettingsChanged"/> if the value differs from current.
    /// Values below -1 are clamped to -1.
    /// </para>
    /// </remarks>
    void UpdateSeed(int value);

    /// <summary>
    /// Updates all parameters at once from an InferenceSettings instance.
    /// </summary>
    /// <param name="settings">The settings to apply. Will be cloned and validated.</param>
    /// <remarks>
    /// <para>
    /// This method is useful for applying settings from external sources
    /// or restoring previous settings. The provided settings are cloned
    /// to prevent external modification.
    /// </para>
    /// <para>
    /// Each parameter is clamped to its valid range using <see cref="ParameterConstants"/>.
    /// Fires <see cref="SettingsChanged"/> with <see cref="InferenceSettingsChangeType.AllChanged"/>
    /// if any value differs from current.
    /// </para>
    /// </remarks>
    void UpdateAll(InferenceSettings settings);

    #endregion

    #region Preset Operations

    /// <summary>
    /// Retrieves all available presets (built-in and user-created).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All presets ordered by built-in status then name.</returns>
    /// <remarks>
    /// <para>
    /// Built-in presets appear first, followed by user-created presets.
    /// Each category is sorted alphabetically by name.
    /// </para>
    /// <para>
    /// Built-in presets:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Balanced (default)</description></item>
    ///   <item><description>Creative</description></item>
    ///   <item><description>Precise</description></item>
    ///   <item><description>Fast</description></item>
    ///   <item><description>Code Review</description></item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<InferencePreset>> GetPresetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a preset, updating all current settings.
    /// </summary>
    /// <param name="presetId">The ID of the preset to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the preset is not found.</exception>
    /// <remarks>
    /// <para>
    /// This operation:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Loads the preset from the repository</description></item>
    ///   <item><description>Replaces all current settings with preset values</description></item>
    ///   <item><description>Sets <see cref="ActivePreset"/> to the loaded preset</description></item>
    ///   <item><description>Persists the active preset ID to settings.json</description></item>
    ///   <item><description>Increments the preset's usage count</description></item>
    ///   <item><description>Fires both <see cref="SettingsChanged"/> and <see cref="PresetChanged"/></description></item>
    /// </list>
    /// </remarks>
    Task ApplyPresetAsync(Guid presetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves current settings as a new preset.
    /// </summary>
    /// <param name="name">The name for the new preset (must be unique).</param>
    /// <param name="description">Optional description of the preset's use case.</param>
    /// <param name="category">Optional category for grouping (e.g., "Code", "Creative").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The newly created preset.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a preset with the same name already exists.</exception>
    /// <remarks>
    /// <para>
    /// Creates a new user preset with the current settings values.
    /// The new preset is not automatically applied; call <see cref="ApplyPresetAsync"/>
    /// to make it active.
    /// </para>
    /// <para>
    /// Fires <see cref="PresetChanged"/> with <see cref="PresetChangeType.Created"/>.
    /// </para>
    /// </remarks>
    Task<InferencePreset> SaveAsPresetAsync(
        string name,
        string? description = null,
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing preset with current settings.
    /// </summary>
    /// <param name="presetId">The ID of the preset to update.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the preset is built-in (IsBuiltIn = true) or not found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Updates the preset's settings values with the current live settings.
    /// The preset's name, description, and category are preserved.
    /// </para>
    /// <para>
    /// Fires <see cref="PresetChanged"/> with <see cref="PresetChangeType.Updated"/>.
    /// </para>
    /// </remarks>
    Task UpdatePresetAsync(Guid presetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user-created preset.
    /// </summary>
    /// <param name="presetId">The ID of the preset to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the preset is built-in (IsBuiltIn = true) or not found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Permanently removes the preset from the database.
    /// If the deleted preset was the active preset, <see cref="ActivePreset"/>
    /// becomes null but current settings are preserved.
    /// </para>
    /// <para>
    /// Fires <see cref="PresetChanged"/> with <see cref="PresetChangeType.Deleted"/>.
    /// </para>
    /// </remarks>
    Task DeletePresetAsync(Guid presetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets settings to the default preset (Balanced).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Equivalent to calling <see cref="ApplyPresetAsync"/> with
    /// <see cref="InferencePreset.BalancedPresetId"/>.
    /// </para>
    /// <para>
    /// Fires <see cref="SettingsChanged"/> with <see cref="InferenceSettingsChangeType.ResetToDefaults"/>
    /// and <see cref="PresetChanged"/> with <see cref="PresetChangeType.Applied"/>.
    /// </para>
    /// </remarks>
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a preset as the new default for new conversations.
    /// </summary>
    /// <param name="presetId">The ID of the preset to make default.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the preset is not found.</exception>
    /// <remarks>
    /// <para>
    /// The default preset is loaded when <see cref="ResetToDefaultsAsync"/> is called
    /// or when no active preset ID is found during <see cref="InitializeAsync"/>.
    /// </para>
    /// <para>
    /// Fires <see cref="PresetChanged"/> with <see cref="PresetChangeType.DefaultChanged"/>.
    /// </para>
    /// </remarks>
    Task SetDefaultPresetAsync(Guid presetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes the service by loading the last active preset.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Call this once during application startup after DI is configured.
    /// This method:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Reads ActivePresetId from settings.json</description></item>
    ///   <item><description>Applies the preset if found</description></item>
    ///   <item><description>Falls back to the default preset (Balanced) if not found</description></item>
    /// </list>
    /// <para>
    /// Safe to call multiple times; subsequent calls are no-ops if already initialized.
    /// </para>
    /// </remarks>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Events

    /// <summary>
    /// Raised when any inference setting changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribers receive a clone of the current settings for thread safety.
    /// The event includes information about what changed via
    /// <see cref="InferenceSettingsChangedEventArgs.ChangeType"/> and
    /// <see cref="InferenceSettingsChangedEventArgs.ChangedParameter"/>.
    /// </para>
    /// <para>
    /// This event fires for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Individual parameter updates</description></item>
    ///   <item><description>Preset application</description></item>
    ///   <item><description>Reset to defaults</description></item>
    ///   <item><description>UpdateAll operations</description></item>
    /// </list>
    /// </remarks>
    event EventHandler<InferenceSettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Raised when preset operations occur (apply, create, update, delete).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event fires for preset-level operations, not individual parameter changes.
    /// Use this to update preset lists in the UI or sync preset state.
    /// </para>
    /// <para>
    /// The event provides both the new and previous preset state where applicable,
    /// enabling subscribers to perform diff-based updates.
    /// </para>
    /// </remarks>
    event EventHandler<PresetChangedEventArgs>? PresetChanged;

    #endregion
}
