namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BATCH APPLY PROGRESS (v0.4.4a)                                           │
// │ Progress information during batch apply operations.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Progress information during batch apply.
/// Reported through IProgress&lt;BatchApplyProgress&gt;.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public sealed class BatchApplyProgress
{
    // ═══════════════════════════════════════════════════════════════════════
    // Count Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Total number of operations to process.
    /// </summary>
    public int TotalOperations { get; init; }

    /// <summary>
    /// Number of completed operations.
    /// </summary>
    public int CompletedOperations { get; init; }

    /// <summary>
    /// Number of failed operations so far.
    /// </summary>
    public int FailedOperations { get; init; }

    /// <summary>
    /// Operations remaining to process.
    /// </summary>
    public int RemainingOperations => TotalOperations - CompletedOperations;

    // ═══════════════════════════════════════════════════════════════════════
    // Current Operation Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Current file being processed.
    /// </summary>
    public string CurrentFile { get; init; } = string.Empty;

    /// <summary>
    /// Current operation type.
    /// </summary>
    public FileOperationType CurrentOperation { get; init; }

    /// <summary>
    /// Current operation ID.
    /// </summary>
    public Guid? CurrentOperationId { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Progress Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public double ProgressPercent => TotalOperations > 0
        ? (double)CompletedOperations / TotalOperations * 100
        : 0;

    /// <summary>
    /// Estimated time remaining based on average operation time.
    /// Null if not enough data to estimate.
    /// </summary>
    public TimeSpan? EstimatedRemaining { get; init; }

    /// <summary>
    /// Time elapsed since batch started.
    /// </summary>
    public TimeSpan Elapsed { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Phase Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Current phase of the batch apply.
    /// </summary>
    public BatchApplyPhase Phase { get; init; }

    /// <summary>
    /// Human-readable phase description.
    /// </summary>
    public string PhaseDescription => Phase switch
    {
        BatchApplyPhase.Validating => "Validating operations...",
        BatchApplyPhase.CreatingBackups => "Creating backups...",
        BatchApplyPhase.CreatingDirectories => "Creating directories...",
        BatchApplyPhase.WritingFiles => $"Writing files ({CompletedOperations}/{TotalOperations})...",
        BatchApplyPhase.Finalizing => "Finalizing...",
        BatchApplyPhase.Completed => "Completed",
        BatchApplyPhase.RollingBack => "Rolling back changes...",
        _ => "Processing..."
    };

    // ═══════════════════════════════════════════════════════════════════════
    // Control Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether the operation can be cancelled at this point.
    /// </summary>
    public bool CanCancel { get; init; } = true;

    /// <summary>
    /// Whether cancellation has been requested.
    /// </summary>
    public bool CancellationRequested { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Status Message
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Detailed status message for display.
    /// </summary>
    public string StatusMessage { get; init; } = string.Empty;
}
