namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APPLY RESULT TYPE (v0.4.3a)                                              │
// │ Enum for categorizing apply operation outcomes.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Type of apply result indicating success category or failure reason.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3a.</para>
/// <para>Values 0-9 are success states, 10+ are failure states.</para>
/// </remarks>
public enum ApplyResultType
{
    // ═══════════════════════════════════════════════════════════════════════
    // Success States (0-9)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Successfully applied changes (generic success).
    /// </summary>
    Success = 0,

    /// <summary>
    /// Successfully created a new file.
    /// </summary>
    Created = 1,

    /// <summary>
    /// Successfully modified an existing file.
    /// </summary>
    Modified = 2,

    // ═══════════════════════════════════════════════════════════════════════
    // Failure States (10+)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Conflict detected - file was modified since the diff was computed.
    /// </summary>
    Conflict = 10,

    /// <summary>
    /// Target file was not found.
    /// </summary>
    FileNotFound = 11,

    /// <summary>
    /// Permission denied to write file.
    /// </summary>
    PermissionDenied = 12,

    /// <summary>
    /// Validation failed (e.g., invalid path, binary file).
    /// </summary>
    ValidationFailed = 13,

    /// <summary>
    /// Operation was cancelled by user.
    /// </summary>
    Cancelled = 14,

    /// <summary>
    /// File is locked by another process.
    /// </summary>
    FileLocked = 15,

    /// <summary>
    /// Disk is full or quota exceeded.
    /// </summary>
    DiskFull = 16,

    /// <summary>
    /// Path is outside the allowed workspace.
    /// </summary>
    PathOutsideWorkspace = 17,

    /// <summary>
    /// Unknown error occurred.
    /// </summary>
    Error = 99
}

/// <summary>
/// Extension methods for <see cref="ApplyResultType"/>.
/// </summary>
public static class ApplyResultTypeExtensions
{
    /// <summary>
    /// Gets whether the result type represents a success.
    /// </summary>
    public static bool IsSuccess(this ApplyResultType type)
        => (int)type < 10;

    /// <summary>
    /// Gets whether the result type represents a failure that can be retried.
    /// </summary>
    public static bool IsRetryable(this ApplyResultType type)
        => type is ApplyResultType.FileLocked
            or ApplyResultType.Conflict
            or ApplyResultType.PermissionDenied;

    /// <summary>
    /// Gets a human-readable description of the result type.
    /// </summary>
    public static string GetDescription(this ApplyResultType type) => type switch
    {
        ApplyResultType.Success => "Changes applied successfully",
        ApplyResultType.Created => "File created successfully",
        ApplyResultType.Modified => "File modified successfully",
        ApplyResultType.Conflict => "File was modified externally - conflict detected",
        ApplyResultType.FileNotFound => "Target file was not found",
        ApplyResultType.PermissionDenied => "Permission denied to write file",
        ApplyResultType.ValidationFailed => "Validation failed",
        ApplyResultType.Cancelled => "Operation cancelled by user",
        ApplyResultType.FileLocked => "File is locked by another process",
        ApplyResultType.DiskFull => "Insufficient disk space",
        ApplyResultType.PathOutsideWorkspace => "Path is outside the workspace",
        ApplyResultType.Error => "An unknown error occurred",
        _ => "Unknown result"
    };
}
