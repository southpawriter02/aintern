namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APPLY RESULT (v0.4.3a)                                                   │
// │ Result of applying a code change to a file.                              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Result of applying a code change to a file.
/// Provides comprehensive information about the operation outcome.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3a.</para>
/// </remarks>
public sealed class ApplyResult
{
    // ═══════════════════════════════════════════════════════════════════════
    // Core Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether the apply operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Type of result (success category or failure reason).
    /// </summary>
    public ApplyResultType ResultType { get; init; }

    /// <summary>
    /// Error message (populated when Success is false).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Exception that caused the failure (if any).
    /// </summary>
    public Exception? Exception { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // File Information
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Full absolute path to the affected file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Relative path within the workspace.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Path to the backup file (if created).
    /// </summary>
    public string? BackupPath { get; init; }

    /// <summary>
    /// File size after apply (in bytes).
    /// </summary>
    public long? FileSizeBytes { get; init; }

    /// <summary>
    /// Detected file encoding.
    /// </summary>
    public string? Encoding { get; init; }

    /// <summary>
    /// Detected line ending style.
    /// </summary>
    public LineEndingStyle? LineEndings { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Change Information
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The diff that was applied.
    /// </summary>
    public DiffResult? AppliedDiff { get; init; }

    /// <summary>
    /// When the change was applied.
    /// </summary>
    public DateTime AppliedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the code block that was applied.
    /// </summary>
    public Guid? CodeBlockId { get; init; }

    /// <summary>
    /// ID of the message containing the code block.
    /// </summary>
    public Guid? MessageId { get; init; }

    /// <summary>
    /// Number of lines added.
    /// </summary>
    public int LinesAdded { get; init; }

    /// <summary>
    /// Number of lines removed.
    /// </summary>
    public int LinesRemoved { get; init; }

    /// <summary>
    /// Number of lines modified.
    /// </summary>
    public int LinesModified { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Undo Information
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether undo is available for this change.
    /// </summary>
    public bool CanUndo { get; init; }

    /// <summary>
    /// ID of the change record (for undo operations).
    /// </summary>
    public Guid? ChangeRecordId { get; init; }

    /// <summary>
    /// When the undo capability expires.
    /// </summary>
    public DateTime? UndoExpiresAt { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Conflict Information
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether a conflict was detected and overwritten.
    /// </summary>
    public bool ConflictOverwritten { get; init; }

    /// <summary>
    /// Hash of the expected content (before apply).
    /// </summary>
    public string? ExpectedContentHash { get; init; }

    /// <summary>
    /// Hash of the actual content found.
    /// </summary>
    public string? ActualContentHash { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a successful result for a modified file.
    /// </summary>
    public static ApplyResult Modified(
        string filePath,
        string relativePath,
        DiffResult? diff = null,
        string? backupPath = null,
        Guid? codeBlockId = null) => new()
    {
        Success = true,
        FilePath = filePath,
        RelativePath = relativePath,
        ResultType = ApplyResultType.Modified,
        AppliedDiff = diff,
        BackupPath = backupPath,
        CodeBlockId = codeBlockId,
        CanUndo = !string.IsNullOrEmpty(backupPath),
        LinesAdded = diff?.Stats?.AddedLines ?? 0,
        LinesRemoved = diff?.Stats?.RemovedLines ?? 0,
        LinesModified = diff?.Stats?.ModifiedLines ?? 0
    };

    /// <summary>
    /// Creates a successful result for a newly created file.
    /// </summary>
    public static ApplyResult Created(
        string filePath,
        string relativePath,
        string? backupPath = null,
        Guid? codeBlockId = null,
        int linesAdded = 0) => new()
    {
        Success = true,
        FilePath = filePath,
        RelativePath = relativePath,
        ResultType = ApplyResultType.Created,
        BackupPath = backupPath,
        CodeBlockId = codeBlockId,
        CanUndo = true,
        LinesAdded = linesAdded
    };

    /// <summary>
    /// Creates a successful result (generic).
    /// </summary>
    public static ApplyResult Succeeded(
        string filePath,
        string relativePath,
        ApplyResultType type,
        DiffResult? diff = null,
        string? backupPath = null) => new()
    {
        Success = true,
        FilePath = filePath,
        RelativePath = relativePath,
        ResultType = type,
        AppliedDiff = diff,
        BackupPath = backupPath,
        CanUndo = !string.IsNullOrEmpty(backupPath)
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ApplyResult Failed(
        string filePath,
        ApplyResultType type,
        string errorMessage,
        Exception? exception = null) => new()
    {
        Success = false,
        FilePath = filePath,
        ResultType = type,
        ErrorMessage = errorMessage,
        Exception = exception,
        CanUndo = false
    };

    /// <summary>
    /// Creates a conflict result.
    /// </summary>
    public static ApplyResult Conflict(
        string filePath,
        string relativePath,
        string expectedHash,
        string actualHash,
        string description) => new()
    {
        Success = false,
        FilePath = filePath,
        RelativePath = relativePath,
        ResultType = ApplyResultType.Conflict,
        ErrorMessage = description,
        ExpectedContentHash = expectedHash,
        ActualContentHash = actualHash,
        CanUndo = false
    };

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    public static ApplyResult Cancelled(string filePath) => new()
    {
        Success = false,
        FilePath = filePath,
        ResultType = ApplyResultType.Cancelled,
        ErrorMessage = "Operation was cancelled by the user.",
        CanUndo = false
    };

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets a human-readable summary of the result.
    /// </summary>
    public string GetSummary()
    {
        if (Success)
        {
            var changes = new List<string>();
            if (LinesAdded > 0) changes.Add($"+{LinesAdded}");
            if (LinesRemoved > 0) changes.Add($"-{LinesRemoved}");
            if (LinesModified > 0) changes.Add($"~{LinesModified}");

            var changeSummary = changes.Count > 0
                ? $" ({string.Join(", ", changes)})"
                : string.Empty;

            return ResultType switch
            {
                ApplyResultType.Created => $"Created {RelativePath}{changeSummary}",
                ApplyResultType.Modified => $"Modified {RelativePath}{changeSummary}",
                _ => $"Applied changes to {RelativePath}{changeSummary}"
            };
        }

        return $"Failed to apply changes to {RelativePath}: {ErrorMessage}";
    }

    /// <summary>
    /// Gets the file name from the file path.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets whether this result represents a new file creation.
    /// </summary>
    public bool IsNewFile => ResultType == ApplyResultType.Created;

    /// <summary>
    /// Gets whether this result represents a failure that can be retried.
    /// </summary>
    public bool IsRetryable => ResultType.IsRetryable();
}
