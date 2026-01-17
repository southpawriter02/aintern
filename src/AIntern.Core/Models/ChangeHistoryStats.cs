namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CHANGE HISTORY STATS (v0.4.5h)                                           │
// │ Aggregated statistics for change history.                                │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Aggregated statistics for change history.
/// </summary>
public sealed record ChangeHistoryStats
{
    // ═══════════════════════════════════════════════════════════════════════
    // Count Statistics
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Total number of changes in the filtered set.
    /// </summary>
    public int TotalChanges { get; init; }

    /// <summary>
    /// Number of changes that can still be undone.
    /// </summary>
    public int UndoableCount { get; init; }

    /// <summary>
    /// Number of changes that were undone.
    /// </summary>
    public int UndoneCount { get; init; }

    /// <summary>
    /// Number of unique files affected.
    /// </summary>
    public int FilesAffected { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Line Statistics
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Total lines added across all changes.
    /// </summary>
    public int TotalLinesAdded { get; init; }

    /// <summary>
    /// Total lines removed across all changes.
    /// </summary>
    public int TotalLinesRemoved { get; init; }

    /// <summary>
    /// Net line change (added - removed).
    /// </summary>
    public int NetLineChange => TotalLinesAdded - TotalLinesRemoved;

    // ═══════════════════════════════════════════════════════════════════════
    // Time Statistics
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Timestamp of the oldest change.
    /// </summary>
    public DateTime? OldestChange { get; init; }

    /// <summary>
    /// Timestamp of the newest change.
    /// </summary>
    public DateTime? NewestChange { get; init; }

    /// <summary>
    /// Time span covered by the changes.
    /// </summary>
    public TimeSpan? TimeSpan => OldestChange.HasValue && NewestChange.HasValue
        ? NewestChange.Value - OldestChange.Value
        : null;

    // ═══════════════════════════════════════════════════════════════════════
    // Breakdowns
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Breakdown by change type.
    /// </summary>
    public IReadOnlyDictionary<FileChangeType, int> ByChangeType { get; init; } =
        new Dictionary<FileChangeType, int>();

    /// <summary>
    /// Breakdown by file extension.
    /// </summary>
    public IReadOnlyDictionary<string, int> ByFileExtension { get; init; } =
        new Dictionary<string, int>();

    // ═══════════════════════════════════════════════════════════════════════
    // Static Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Empty stats instance.
    /// </summary>
    public static ChangeHistoryStats Empty => new();

    /// <summary>
    /// Computes stats from a collection of records.
    /// </summary>
    /// <param name="records">The records to compute stats from.</param>
    /// <param name="undoWindow">The undo window duration for can-undo calculation.</param>
    /// <returns>Computed statistics.</returns>
    public static ChangeHistoryStats FromRecords(
        IEnumerable<FileChangeRecord> records,
        TimeSpan undoWindow = default)
    {
        var list = records.ToList();
        if (list.Count == 0) return Empty;

        var byChangeType = list
            .GroupBy(r => r.ChangeType)
            .ToDictionary(g => g.Key, g => g.Count());

        var byExtension = list
            .GroupBy(r => Path.GetExtension(r.FilePath).ToLowerInvariant())
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key, g => g.Count());

        return new ChangeHistoryStats
        {
            TotalChanges = list.Count,
            UndoableCount = list.Count(r => r.CanUndo(undoWindow)),
            UndoneCount = list.Count(r => r.IsUndone),
            FilesAffected = list.Select(r => r.FilePath).Distinct().Count(),
            TotalLinesAdded = list.Sum(r => r.LinesAdded),
            TotalLinesRemoved = list.Sum(r => r.LinesRemoved),
            OldestChange = list.Min(r => r.ChangedAt),
            NewestChange = list.Max(r => r.ChangedAt),
            ByChangeType = byChangeType,
            ByFileExtension = byExtension
        };
    }
}
