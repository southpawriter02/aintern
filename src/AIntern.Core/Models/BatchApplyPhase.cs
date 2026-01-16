namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BATCH APPLY PHASE (v0.4.4a)                                              │
// │ Phase of a batch apply operation.                                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Phase of a batch apply operation.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public enum BatchApplyPhase
{
    /// <summary>
    /// Validating operations before apply.
    /// Checking paths, permissions, conflicts.
    /// </summary>
    Validating,

    /// <summary>
    /// Creating backup copies of existing files.
    /// Only for files that will be modified.
    /// </summary>
    CreatingBackups,

    /// <summary>
    /// Creating required directories.
    /// Parent directories for new files.
    /// </summary>
    CreatingDirectories,

    /// <summary>
    /// Writing file contents.
    /// Main phase of the operation.
    /// </summary>
    WritingFiles,

    /// <summary>
    /// Finalizing the operation.
    /// Cleanup, notifications.
    /// </summary>
    Finalizing,

    /// <summary>
    /// Operation completed successfully.
    /// Terminal success state.
    /// </summary>
    Completed,

    /// <summary>
    /// Rolling back due to error.
    /// Restoring from backups.
    /// </summary>
    RollingBack
}
