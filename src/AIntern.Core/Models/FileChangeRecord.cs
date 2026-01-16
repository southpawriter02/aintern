namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE CHANGE RECORD (v0.4.3a)                                             │
// │ Record of a file change for undo tracking and history.                   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Record of a file change for undo tracking and history.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3a.</para>
/// </remarks>
public sealed class FileChangeRecord
{
    // ═══════════════════════════════════════════════════════════════════════
    // Identity
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Unique identifier for this change.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════════════════
    // File Information
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Full absolute path to the changed file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Relative path within the workspace.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Path to the backup file (for undo).
    /// Required for Modified/Deleted changes, null for Created.
    /// </summary>
    public string? BackupPath { get; init; }

    /// <summary>
    /// Path to the new file (for rename tracking).
    /// </summary>
    public string? NewFilePath { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Timestamps
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// When the change was made.
    /// </summary>
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Original file modification time before the change.
    /// </summary>
    public DateTime? OriginalModifiedAt { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Change Information
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Type of change that was made.
    /// </summary>
    public FileChangeType ChangeType { get; init; }

    /// <summary>
    /// ID of the code block that generated this change.
    /// </summary>
    public Guid? CodeBlockId { get; init; }

    /// <summary>
    /// ID of the message containing the code block.
    /// </summary>
    public Guid? MessageId { get; init; }

    /// <summary>
    /// ID of the conversation containing the message.
    /// </summary>
    public Guid? ConversationId { get; init; }

    /// <summary>
    /// Human-readable description of the change.
    /// </summary>
    public string? Description { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Content Hashes
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SHA-256 hash of the original content (before change).
    /// Used for verification during undo.
    /// </summary>
    public string? OriginalContentHash { get; init; }

    /// <summary>
    /// SHA-256 hash of the new content (after change).
    /// Used for conflict detection.
    /// </summary>
    public string? NewContentHash { get; init; }

    /// <summary>
    /// Original file size in bytes.
    /// </summary>
    public long? OriginalSizeBytes { get; init; }

    /// <summary>
    /// New file size in bytes.
    /// </summary>
    public long? NewSizeBytes { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Undo State
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether undo has been performed for this change.
    /// </summary>
    public bool IsUndone { get; set; }

    /// <summary>
    /// When the undo was performed.
    /// </summary>
    public DateTime? UndoneAt { get; set; }

    // ═══════════════════════════════════════════════════════════════════════
    // Statistics
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Number of lines added in this change.
    /// </summary>
    public int LinesAdded { get; init; }

    /// <summary>
    /// Number of lines removed in this change.
    /// </summary>
    public int LinesRemoved { get; init; }

    /// <summary>
    /// Number of lines modified in this change.
    /// </summary>
    public int LinesModified { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Undo Time Calculations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calculates the time remaining for undo based on the configured window.
    /// </summary>
    public TimeSpan GetUndoTimeRemaining(TimeSpan undoWindow)
    {
        if (IsUndone)
            return TimeSpan.Zero;

        var elapsed = DateTime.UtcNow - ChangedAt;
        var remaining = undoWindow - elapsed;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Checks if the undo window has expired.
    /// </summary>
    public bool IsUndoExpired(TimeSpan undoWindow)
    {
        return IsUndone || DateTime.UtcNow - ChangedAt > undoWindow;
    }

    /// <summary>
    /// Gets whether undo is currently available.
    /// </summary>
    public bool CanUndo(TimeSpan undoWindow)
    {
        if (IsUndone)
            return false;

        if (string.IsNullOrEmpty(BackupPath) && ChangeType != FileChangeType.Created)
            return false;

        return !IsUndoExpired(undoWindow);
    }

    /// <summary>
    /// Gets the percentage of undo time remaining (0-100).
    /// </summary>
    public double GetUndoTimeRemainingPercent(TimeSpan undoWindow)
    {
        if (undoWindow <= TimeSpan.Zero)
            return 0;

        var remaining = GetUndoTimeRemaining(undoWindow);
        return (remaining.TotalMilliseconds / undoWindow.TotalMilliseconds) * 100;
    }

    /// <summary>
    /// Gets the file name from the file path.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);
}
