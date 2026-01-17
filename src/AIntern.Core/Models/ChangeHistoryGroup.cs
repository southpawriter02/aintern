namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CHANGE HISTORY GROUP (v0.4.5h)                                           │
// │ Grouped change history records.                                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a group of change history records.
/// </summary>
/// <param name="Key">Unique key for the group (file path, date, change type, etc.).</param>
/// <param name="Label">Human-readable label for display.</param>
/// <param name="Icon">Icon identifier for the group.</param>
/// <param name="Items">Records in this group.</param>
public sealed record ChangeHistoryGroup(
    string Key,
    string Label,
    string? Icon,
    IReadOnlyList<FileChangeRecord> Items)
{
    /// <summary>
    /// Number of items in this group.
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// Number of undoable items in this group.
    /// </summary>
    public int UndoableCount(TimeSpan undoWindow) => Items.Count(i => i.CanUndo(undoWindow));

    /// <summary>
    /// Total lines added across all items.
    /// </summary>
    public int TotalLinesAdded => Items.Sum(i => i.LinesAdded);

    /// <summary>
    /// Total lines removed across all items.
    /// </summary>
    public int TotalLinesRemoved => Items.Sum(i => i.LinesRemoved);

    /// <summary>
    /// Most recent change timestamp in the group.
    /// </summary>
    public DateTime? MostRecent => Items.Count > 0 ? Items.Max(i => i.ChangedAt) : null;
}
