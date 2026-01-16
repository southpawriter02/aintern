namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ UNDO EVENTS (v0.4.3d)                                                    │
// │ Event arguments for undo system notifications.                           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event args raised when an undo becomes available.
/// </summary>
public sealed class UndoAvailableEventArgs : EventArgs
{
    /// <summary>Absolute path to the changed file.</summary>
    public required string FilePath { get; init; }

    /// <summary>Relative path within the workspace.</summary>
    public required string RelativePath { get; init; }

    /// <summary>Type of change that was made.</summary>
    public required FileChangeType ChangeType { get; init; }

    /// <summary>When the undo window expires.</summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>Unique identifier of the change.</summary>
    public required Guid ChangeId { get; init; }

    /// <summary>The full undo state.</summary>
    public required UndoState UndoState { get; init; }
}

/// <summary>
/// Event args raised when an undo time window expires.
/// </summary>
public sealed class UndoExpiredEventArgs : EventArgs
{
    /// <summary>Absolute path to the file.</summary>
    public required string FilePath { get; init; }

    /// <summary>Unique identifier of the change.</summary>
    public required Guid ChangeId { get; init; }

    /// <summary>When the undo was originally available.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>When the undo expired.</summary>
    public required DateTime ExpiredAt { get; init; }
}

/// <summary>
/// Event args raised when an undo operation completes.
/// </summary>
public sealed class UndoCompletedEventArgs : EventArgs
{
    /// <summary>Absolute path to the file.</summary>
    public required string FilePath { get; init; }

    /// <summary>Unique identifier of the change.</summary>
    public required Guid ChangeId { get; init; }

    /// <summary>Whether the undo was successful.</summary>
    public required bool Success { get; init; }

    /// <summary>Error message if undo failed.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Event args raised periodically with updated time remaining.
/// </summary>
public sealed class TimeRemainingChangedEventArgs : EventArgs
{
    /// <summary>All pending undo states with updated time remaining.</summary>
    public required IReadOnlyList<UndoState> PendingUndos { get; init; }

    /// <summary>Number of undos expiring soon.</summary>
    public int ExpiringSoonCount { get; init; }
}
