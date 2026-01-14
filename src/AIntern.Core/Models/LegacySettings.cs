// -----------------------------------------------------------------------
// <copyright file="LegacySettings.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Model representing the v0.1.0 settings.json file format for migration purposes.
//     Added in v0.2.5d.
// </summary>
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace AIntern.Core.Models;

/// <summary>
/// Represents the v0.1.0 legacy settings file format.
/// </summary>
/// <remarks>
/// <para>
/// This internal model is used exclusively by the migration service to read
/// settings from the old JSON-based configuration format (settings.json).
/// </para>
/// <para>
/// Properties use <see cref="JsonPropertyNameAttribute"/> to match the exact
/// casing used in legacy settings files.
/// </para>
/// </remarks>
internal sealed class LegacySettings
{
    /// <summary>
    /// Gets or sets the last model file path that was loaded.
    /// </summary>
    /// <value>
    /// The full path to the last GGUF model file, or <c>null</c> if none was set.
    /// </value>
    [JsonPropertyName("lastModelPath")]
    public string? LastModelPath { get; set; }

    /// <summary>
    /// Gets or sets the default context size for model inference.
    /// </summary>
    /// <value>
    /// The context window size in tokens. Defaults to 4096.
    /// </value>
    [JsonPropertyName("defaultContextSize")]
    public uint DefaultContextSize { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the default number of GPU layers to offload.
    /// </summary>
    /// <value>
    /// The number of layers to offload to GPU. -1 indicates automatic detection.
    /// Defaults to -1.
    /// </value>
    [JsonPropertyName("defaultGpuLayers")]
    public int DefaultGpuLayers { get; set; } = -1;

    /// <summary>
    /// Gets or sets the default temperature for text generation.
    /// </summary>
    /// <value>
    /// The sampling temperature (0.0-2.0). Lower values are more deterministic.
    /// Defaults to 0.7.
    /// </value>
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.7f;

    /// <summary>
    /// Gets or sets the application theme.
    /// </summary>
    /// <value>
    /// The theme name ("Light" or "Dark"). Defaults to "Dark".
    /// </value>
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Dark";
}
