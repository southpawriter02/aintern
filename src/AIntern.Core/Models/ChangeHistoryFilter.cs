namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CHANGE HISTORY FILTER (v0.4.5h)                                          │
// │ Filter criteria for querying change history.                             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Filter criteria for change history queries.
/// </summary>
public sealed record ChangeHistoryFilter
{
    // ═══════════════════════════════════════════════════════════════════════
    // Filter Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Text to search in file names and paths.
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// File extensions to include (e.g., ".cs", ".ts").
    /// Null means include all.
    /// </summary>
    public IReadOnlySet<string>? FileExtensions { get; init; }

    /// <summary>
    /// Change types to include.
    /// Null means include all.
    /// </summary>
    public IReadOnlySet<FileChangeType>? ChangeTypes { get; init; }

    /// <summary>
    /// Date range for filtering by timestamp.
    /// </summary>
    public DateTimeRange? DateRange { get; init; }

    /// <summary>
    /// Only include changes that can still be undone.
    /// </summary>
    public bool OnlyUndoable { get; init; }

    /// <summary>
    /// Filter to changes from a specific conversation.
    /// </summary>
    public Guid? ConversationId { get; init; }

    /// <summary>
    /// Filter to changes from a specific message.
    /// </summary>
    public Guid? MessageId { get; init; }

    /// <summary>
    /// Sort order for results.
    /// </summary>
    public ChangeHistorySortOrder SortOrder { get; init; } = ChangeHistorySortOrder.NewestFirst;

    // ═══════════════════════════════════════════════════════════════════════
    // Static Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Default filter with no restrictions.
    /// </summary>
    public static ChangeHistoryFilter Default => new();

    /// <summary>
    /// Filter for only undoable changes.
    /// </summary>
    public static ChangeHistoryFilter UndoableOnly => new() { OnlyUndoable = true };

    /// <summary>
    /// Filter for changes in the last hour.
    /// </summary>
    public static ChangeHistoryFilter LastHour => new()
    {
        DateRange = DateTimeRange.LastNHours(1)
    };

    /// <summary>
    /// Filter for changes today.
    /// </summary>
    public static ChangeHistoryFilter Today => new()
    {
        DateRange = DateTimeRange.Today
    };

    /// <summary>
    /// Creates a filter for a specific file extension.
    /// </summary>
    public static ChangeHistoryFilter ForExtension(string extension) => new()
    {
        FileExtensions = new HashSet<string> { extension.StartsWith('.') ? extension : $".{extension}" }
    };

    /// <summary>
    /// Creates a filter for a specific change type.
    /// </summary>
    public static ChangeHistoryFilter ForChangeType(FileChangeType changeType) => new()
    {
        ChangeTypes = new HashSet<FileChangeType> { changeType }
    };

    // ═══════════════════════════════════════════════════════════════════════
    // Matching Logic
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks if a record matches this filter.
    /// </summary>
    /// <param name="record">The record to check.</param>
    /// <param name="undoWindow">The undo window duration (only needed if OnlyUndoable is true).</param>
    /// <returns>True if the record matches all filter criteria.</returns>
    public bool Matches(FileChangeRecord record, TimeSpan undoWindow = default)
    {
        // Search text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            if (!record.FilePath.ToLowerInvariant().Contains(searchLower) &&
                !record.RelativePath.ToLowerInvariant().Contains(searchLower))
            {
                return false;
            }
        }

        // File extension filter
        if (FileExtensions is { Count: > 0 })
        {
            var ext = Path.GetExtension(record.FilePath).ToLowerInvariant();
            if (!FileExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        // Change type filter
        if (ChangeTypes is { Count: > 0 })
        {
            if (!ChangeTypes.Contains(record.ChangeType))
            {
                return false;
            }
        }

        // Date range filter
        if (DateRange is not null)
        {
            if (!DateRange.Contains(record.ChangedAt))
            {
                return false;
            }
        }

        // Only undoable filter
        if (OnlyUndoable && !record.CanUndo(undoWindow))
        {
            return false;
        }

        // Conversation filter
        if (ConversationId.HasValue && record.ConversationId != ConversationId.Value)
        {
            return false;
        }

        // Message filter
        if (MessageId.HasValue && record.MessageId != MessageId.Value)
        {
            return false;
        }

        return true;
    }
}
