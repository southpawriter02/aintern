namespace AIntern.Core.Models;

/// <summary>
/// Configuration for context attachment limits.
/// </summary>
public sealed class ContextLimitsConfig
{
    /// <summary>
    /// Maximum number of files that can be attached at once.
    /// </summary>
    public int MaxFilesAttached { get; set; } = 10;

    /// <summary>
    /// Maximum tokens per individual file.
    /// </summary>
    public int MaxTokensPerFile { get; set; } = 4000;

    /// <summary>
    /// Maximum total tokens across all attached contexts.
    /// </summary>
    public int MaxTotalContextTokens { get; set; } = 8000;

    /// <summary>
    /// Maximum file size in bytes (files larger are rejected).
    /// </summary>
    public int MaxFileSizeBytes { get; set; } = 500_000; // 500KB

    /// <summary>
    /// Warning threshold as percentage of limit (0.0-1.0).
    /// </summary>
    public double WarningThreshold { get; set; } = 0.8;

    /// <summary>
    /// Maximum lines to show in preview.
    /// </summary>
    public int MaxPreviewLines { get; set; } = 20;

    /// <summary>
    /// Maximum characters in preview content.
    /// </summary>
    public int MaxPreviewCharacters { get; set; } = 500;
}
