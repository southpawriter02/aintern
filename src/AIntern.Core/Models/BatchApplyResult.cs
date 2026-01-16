namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BATCH APPLY RESULT (v0.4.4a)                                             │
// │ Result of applying a batch of file operations.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Result of applying a batch of file operations.
/// Aggregates individual ApplyResult instances with summary statistics.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public sealed class BatchApplyResult
{
    // ═══════════════════════════════════════════════════════════════════════
    // Summary Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether all selected operations succeeded.
    /// </summary>
    public bool AllSucceeded { get; init; }

    /// <summary>
    /// Number of successful operations.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Number of failed operations.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Number of skipped operations.
    /// </summary>
    public int SkippedCount { get; init; }

    /// <summary>
    /// Total operations attempted (success + failed + skipped).
    /// </summary>
    public int TotalCount => SuccessCount + FailedCount + SkippedCount;

    /// <summary>
    /// Success rate as percentage (0-100).
    /// </summary>
    public double SuccessRate => TotalCount > 0
        ? (double)SuccessCount / TotalCount * 100
        : 0;

    // ═══════════════════════════════════════════════════════════════════════
    // Individual Results
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Individual results for each operation.
    /// Ordered by operation sequence.
    /// </summary>
    public IReadOnlyList<ApplyResult> Results { get; init; } = Array.Empty<ApplyResult>();

    /// <summary>
    /// Operations that failed.
    /// </summary>
    public IEnumerable<ApplyResult> FailedResults =>
        Results.Where(r => !r.Success);

    /// <summary>
    /// Operations that succeeded.
    /// </summary>
    public IEnumerable<ApplyResult> SucceededResults =>
        Results.Where(r => r.Success);

    /// <summary>
    /// Get result for a specific path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The result, or null if not found.</returns>
    public ApplyResult? GetResultForPath(string path) =>
        Results.FirstOrDefault(r =>
            r.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));

    // ═══════════════════════════════════════════════════════════════════════
    // Timing Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// When the batch apply started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// When the batch apply completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Duration of the batch apply.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// Average time per operation.
    /// </summary>
    public TimeSpan AverageOperationTime => TotalCount > 0
        ? TimeSpan.FromTicks(Duration.Ticks / TotalCount)
        : TimeSpan.Zero;

    // ═══════════════════════════════════════════════════════════════════════
    // Undo Support
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Paths of all backup files created.
    /// Used for undo operations.
    /// </summary>
    public IReadOnlyList<string> BackupPaths { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether undo is available for all successful changes.
    /// </summary>
    public bool CanUndoAll => Results.All(r => !r.Success || r.CanUndo);

    /// <summary>
    /// Number of operations that can be undone.
    /// </summary>
    public int UndoableCount => Results.Count(r => r.CanUndo);

    // ═══════════════════════════════════════════════════════════════════════
    // State Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether the batch was cancelled mid-operation.
    /// </summary>
    public bool WasCancelled { get; init; }

    /// <summary>
    /// Whether a rollback was performed after failure.
    /// </summary>
    public bool WasRolledBack { get; init; }

    /// <summary>
    /// Error message if the batch failed overall.
    /// </summary>
    public string? BatchErrorMessage { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Create a successful batch result.
    /// </summary>
    /// <param name="results">Individual operation results.</param>
    /// <param name="startedAt">When the batch started.</param>
    /// <returns>A successful BatchApplyResult.</returns>
    public static BatchApplyResult Success(
        IReadOnlyList<ApplyResult> results,
        DateTime startedAt) => new()
    {
        AllSucceeded = true,
        SuccessCount = results.Count,
        FailedCount = 0,
        SkippedCount = 0,
        Results = results,
        StartedAt = startedAt,
        CompletedAt = DateTime.UtcNow,
        BackupPaths = results
            .Where(r => r.BackupPath != null)
            .Select(r => r.BackupPath!)
            .ToList()
    };

    /// <summary>
    /// Create a partial success result.
    /// </summary>
    /// <param name="results">All operation results.</param>
    /// <param name="startedAt">When the batch started.</param>
    /// <returns>A partial success BatchApplyResult.</returns>
    public static BatchApplyResult PartialSuccess(
        IReadOnlyList<ApplyResult> results,
        DateTime startedAt)
    {
        var succeeded = results.Where(r => r.Success).ToList();
        var failed = results.Where(r => !r.Success).ToList();

        return new()
        {
            AllSucceeded = false,
            SuccessCount = succeeded.Count,
            FailedCount = failed.Count,
            SkippedCount = 0,
            Results = results,
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow,
            BackupPaths = succeeded
                .Where(r => r.BackupPath != null)
                .Select(r => r.BackupPath!)
                .ToList()
        };
    }

    /// <summary>
    /// Create a cancelled result.
    /// </summary>
    /// <param name="completedResults">Results completed before cancellation.</param>
    /// <param name="startedAt">When the batch started.</param>
    /// <returns>A cancelled BatchApplyResult.</returns>
    public static BatchApplyResult Cancelled(
        IReadOnlyList<ApplyResult> completedResults,
        DateTime startedAt) => new()
    {
        AllSucceeded = false,
        SuccessCount = completedResults.Count(r => r.Success),
        FailedCount = completedResults.Count(r => !r.Success),
        SkippedCount = 0,
        Results = completedResults,
        StartedAt = startedAt,
        CompletedAt = DateTime.UtcNow,
        WasCancelled = true,
        BackupPaths = completedResults
            .Where(r => r.Success && r.BackupPath != null)
            .Select(r => r.BackupPath!)
            .ToList()
    };

    /// <summary>
    /// Create a rolled back result.
    /// </summary>
    /// <param name="errorMessage">The error that triggered rollback.</param>
    /// <param name="startedAt">When the batch started.</param>
    /// <returns>A rolled back BatchApplyResult.</returns>
    public static BatchApplyResult RolledBack(
        string errorMessage,
        DateTime startedAt) => new()
    {
        AllSucceeded = false,
        SuccessCount = 0,
        FailedCount = 0,
        SkippedCount = 0,
        Results = Array.Empty<ApplyResult>(),
        StartedAt = startedAt,
        CompletedAt = DateTime.UtcNow,
        WasRolledBack = true,
        BatchErrorMessage = errorMessage
    };
}
