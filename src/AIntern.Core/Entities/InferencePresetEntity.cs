namespace AIntern.Core.Entities;

/// <summary>
/// Entity class for persisting inference parameter presets to the database.
/// </summary>
public sealed class InferencePresetEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.7f;
    public float TopP { get; set; } = 0.9f;
    public int MaxTokens { get; set; } = 2048;
    public int ContextSize { get; set; } = 4096;
    public bool IsDefault { get; set; }
    public bool IsBuiltIn { get; set; }
}
