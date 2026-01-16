namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APPLY PREVIEW (v0.4.3a)                                                  │
// │ Preview information before applying changes.                             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Preview of an apply operation showing effects before execution.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3a.</para>
/// </remarks>
public sealed class ApplyPreview
{
    /// <summary>
    /// The computed diff to be applied.
    /// </summary>
    public DiffResult? Diff { get; init; }

    /// <summary>
    /// Full absolute path to the target file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Relative path within the workspace.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Whether the target file currently exists.
    /// </summary>
    public bool TargetExists { get; init; }

    /// <summary>
    /// Whether a conflict was detected.
    /// </summary>
    public bool HasConflict { get; init; }

    /// <summary>
    /// Conflict details (if any).
    /// </summary>
    public ConflictCheckResult? ConflictResult { get; init; }

    /// <summary>
    /// Whether the file can be written (permissions check).
    /// </summary>
    public bool CanWrite { get; init; }

    /// <summary>
    /// Reason why the file cannot be written (if applicable).
    /// </summary>
    public string? WriteBlockedReason { get; init; }

    /// <summary>
    /// Whether the file is locked by another process.
    /// </summary>
    public bool IsFileLocked { get; init; }

    /// <summary>
    /// Detected file encoding.
    /// </summary>
    public string? DetectedEncoding { get; init; }

    /// <summary>
    /// Detected line ending style.
    /// </summary>
    public LineEndingStyle DetectedLineEndings { get; init; }

    /// <summary>
    /// Current file size in bytes.
    /// </summary>
    public long? CurrentSizeBytes { get; init; }

    /// <summary>
    /// Estimated size after apply.
    /// </summary>
    public long? EstimatedNewSizeBytes { get; init; }

    /// <summary>
    /// Whether this will create a new file.
    /// </summary>
    public bool IsNewFile => !TargetExists;

    /// <summary>
    /// Whether the apply can proceed safely.
    /// </summary>
    public bool CanApply => CanWrite && !IsFileLocked && (!HasConflict || ConflictResult?.CanOverwrite == true);

    /// <summary>
    /// Gets a summary of the preview.
    /// </summary>
    public string GetSummary()
    {
        if (!CanApply)
        {
            if (!CanWrite)
                return $"Cannot write to {RelativePath}: {WriteBlockedReason}";
            if (IsFileLocked)
                return $"File is locked: {RelativePath}";
            if (HasConflict)
                return $"Conflict detected in {RelativePath}";
        }

        var action = IsNewFile ? "Create" : "Modify";
        var stats = Diff?.Stats?.Summary ?? "no changes";
        return $"{action} {RelativePath} ({stats})";
    }
}

/// <summary>
/// Result of checking for conflicts before apply.
/// </summary>
public sealed class ConflictCheckResult
{
    /// <summary>
    /// Whether a conflict was detected.
    /// </summary>
    public bool HasConflict { get; init; }

    /// <summary>
    /// Hash of the expected content (when diff was computed).
    /// </summary>
    public string? ExpectedHash { get; init; }

    /// <summary>
    /// Hash of the actual current content.
    /// </summary>
    public string? ActualHash { get; init; }

    /// <summary>
    /// When the file was last modified.
    /// </summary>
    public DateTime? FileModifiedAt { get; init; }

    /// <summary>
    /// When the diff was computed.
    /// </summary>
    public DateTime? DiffComputedAt { get; init; }

    /// <summary>
    /// Human-readable description of the conflict.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether the conflict can be overwritten (user permission).
    /// </summary>
    public bool CanOverwrite { get; init; }

    /// <summary>
    /// Creates a result indicating no conflict.
    /// </summary>
    public static ConflictCheckResult NoConflict() => new()
    {
        HasConflict = false
    };

    /// <summary>
    /// Creates a result indicating a conflict was detected.
    /// </summary>
    public static ConflictCheckResult Detected(
        string expectedHash,
        string actualHash,
        string description) => new()
    {
        HasConflict = true,
        ExpectedHash = expectedHash,
        ActualHash = actualHash,
        Description = description,
        CanOverwrite = false
    };
}
