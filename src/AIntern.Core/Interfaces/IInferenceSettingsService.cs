using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Service for managing inference parameter settings and presets.
/// </summary>
public interface IInferenceSettingsService
{
    /// <summary>
    /// Gets the current inference settings.
    /// </summary>
    InferenceSettings CurrentSettings { get; }

    /// <summary>
    /// Updates the current settings.
    /// </summary>
    void UpdateSettings(InferenceSettings settings);

    /// <summary>
    /// Gets all available presets (built-in and user-created).
    /// </summary>
    Task<IReadOnlyList<InferencePreset>> GetPresetsAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves a new preset or updates an existing one.
    /// </summary>
    Task<InferencePreset> SavePresetAsync(string name, InferenceSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Deletes a user-created preset.
    /// </summary>
    Task DeletePresetAsync(Guid presetId, CancellationToken ct = default);

    /// <summary>
    /// Applies a preset to current settings.
    /// </summary>
    Task ApplyPresetAsync(Guid presetId, CancellationToken ct = default);

    /// <summary>
    /// Sets a preset as the default.
    /// </summary>
    Task SetDefaultPresetAsync(Guid presetId, CancellationToken ct = default);

    /// <summary>
    /// Resets current settings to defaults.
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Raised when settings change.
    /// </summary>
    event EventHandler<InferenceSettingsChangedEventArgs>? SettingsChanged;
}

/// <summary>
/// Current inference settings.
/// </summary>
public record InferenceSettings
{
    public float Temperature { get; init; } = 0.7f;
    public float TopP { get; init; } = 0.9f;
    public int MaxTokens { get; init; } = 2048;
    public int ContextSize { get; init; } = 4096;

    public static InferenceSettings Default => new();
}

/// <summary>
/// Represents a saved inference preset.
/// </summary>
public record InferencePreset
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public InferenceSettings Settings { get; init; } = new();
    public bool IsDefault { get; init; }
    public bool IsBuiltIn { get; init; }
}

/// <summary>
/// Event args for settings changes.
/// </summary>
public sealed class InferenceSettingsChangedEventArgs : EventArgs
{
    public InferenceSettings NewSettings { get; }
    public InferenceSettings? PreviousSettings { get; }

    public InferenceSettingsChangedEventArgs(InferenceSettings newSettings, InferenceSettings? previousSettings = null)
    {
        NewSettings = newSettings;
        PreviousSettings = previousSettings;
    }
}
