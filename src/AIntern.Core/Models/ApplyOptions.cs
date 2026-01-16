namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APPLY OPTIONS (v0.4.3a)                                                  │
// │ Configuration options for apply operations.                              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Configuration options for applying code changes to files.
/// Immutable record with sensible defaults.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3a.</para>
/// </remarks>
public sealed record ApplyOptions
{
    // ═══════════════════════════════════════════════════════════════════════
    // Backup Options
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether to create a backup before modifying the file.
    /// Enables undo functionality when true.
    /// </summary>
    public bool CreateBackup { get; init; } = true;

    /// <summary>
    /// Directory for storing backup files.
    /// If null, uses the default backup directory.
    /// </summary>
    public string? BackupDirectory { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Conflict Options
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether to allow overwriting when a conflict is detected.
    /// A conflict occurs when the file has been modified since the diff was computed.
    /// </summary>
    public bool AllowConflictOverwrite { get; init; } = false;

    /// <summary>
    /// Whether to check for conflicts before applying.
    /// Disabling this skips the hash verification step.
    /// </summary>
    public bool CheckForConflicts { get; init; } = true;

    // ═══════════════════════════════════════════════════════════════════════
    // Editor Integration Options
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether to refresh/reload the editor after applying changes.
    /// </summary>
    public bool RefreshEditorAfterApply { get; init; } = true;

    /// <summary>
    /// Whether to scroll to the first change after applying.
    /// </summary>
    public bool ScrollToFirstChange { get; init; } = true;

    // ═══════════════════════════════════════════════════════════════════════
    // Undo Options
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Time window during which undo is available.
    /// After this period, backups are eligible for cleanup.
    /// </summary>
    public TimeSpan UndoWindow { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Maximum number of undo records to keep per file.
    /// Older records are pruned when this limit is exceeded.
    /// </summary>
    public int MaxUndoRecordsPerFile { get; init; } = 10;

    // ═══════════════════════════════════════════════════════════════════════
    // Dialog Options
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether to show a confirmation dialog before applying.
    /// </summary>
    public bool ShowConfirmationDialog { get; init; } = true;

    /// <summary>
    /// Whether to show a toast notification after successful apply.
    /// </summary>
    public bool ShowSuccessToast { get; init; } = true;

    /// <summary>
    /// Duration to show the success toast (with undo button).
    /// </summary>
    public TimeSpan ToastDuration { get; init; } = TimeSpan.FromSeconds(10);

    // ═══════════════════════════════════════════════════════════════════════
    // File Handling Options
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validate that the file encoding can be detected and preserved.
    /// </summary>
    public bool ValidateEncoding { get; init; } = true;

    /// <summary>
    /// Preserve the original file's line ending style (CRLF, LF, CR).
    /// </summary>
    public bool PreserveLineEndings { get; init; } = true;

    /// <summary>
    /// Create parent directories if they don't exist (for new files).
    /// </summary>
    public bool CreateParentDirectories { get; init; } = true;

    /// <summary>
    /// Whether to preserve the original file's attributes (readonly, hidden, etc.).
    /// </summary>
    public bool PreserveFileAttributes { get; init; } = true;

    /// <summary>
    /// Whether to update the file's modification timestamp.
    /// </summary>
    public bool UpdateTimestamp { get; init; } = true;

    // ═══════════════════════════════════════════════════════════════════════
    // Validation Options
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maximum file size (in bytes) that can be modified.
    /// </summary>
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Whether to validate that the file appears to be a text file.
    /// </summary>
    public bool ValidateTextFile { get; init; } = true;

    // ═══════════════════════════════════════════════════════════════════════
    // Static Presets
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Default options with all safety features enabled.
    /// </summary>
    public static ApplyOptions Default => new();

    /// <summary>
    /// Options for silent apply (no dialogs, no backup).
    /// Use with caution - no undo capability.
    /// </summary>
    public static ApplyOptions Silent => new()
    {
        CreateBackup = false,
        ShowConfirmationDialog = false,
        ShowSuccessToast = false,
        CheckForConflicts = false
    };

    /// <summary>
    /// Options optimized for batch operations.
    /// Creates backups but skips dialogs for efficiency.
    /// </summary>
    public static ApplyOptions Batch => new()
    {
        CreateBackup = true,
        ShowConfirmationDialog = false,
        ShowSuccessToast = false,
        RefreshEditorAfterApply = false
    };

    /// <summary>
    /// Options with extended undo window (1 hour).
    /// </summary>
    public static ApplyOptions ExtendedUndo => new()
    {
        UndoWindow = TimeSpan.FromHours(1),
        MaxUndoRecordsPerFile = 20
    };

    // ═══════════════════════════════════════════════════════════════════════
    // Builder Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates options with backup disabled.
    /// </summary>
    public ApplyOptions WithoutBackup() => this with { CreateBackup = false };

    /// <summary>
    /// Creates options with a custom undo window.
    /// </summary>
    public ApplyOptions WithUndoWindow(TimeSpan window) => this with { UndoWindow = window };

    /// <summary>
    /// Creates options that allow conflict overwrite.
    /// </summary>
    public ApplyOptions WithConflictOverwrite() => this with { AllowConflictOverwrite = true };

    /// <summary>
    /// Creates options with dialogs disabled.
    /// </summary>
    public ApplyOptions WithoutDialogs() => this with
    {
        ShowConfirmationDialog = false,
        ShowSuccessToast = false
    };
}
