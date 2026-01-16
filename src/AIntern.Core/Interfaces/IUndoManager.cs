using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ UNDO MANAGER INTERFACE (v0.4.3d)                                         │
// │ Manages undo operations with time-based expiration.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Manages undo operations with time-based expiration.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3d.</para>
/// </remarks>
public interface IUndoManager : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Gets the configured undo time window.</summary>
    TimeSpan UndoWindow { get; }

    /// <summary>Gets the number of pending undos.</summary>
    int PendingUndoCount { get; }

    /// <summary>Gets whether any undos are available.</summary>
    bool HasPendingUndos { get; }

    // ═══════════════════════════════════════════════════════════════════════
    // Undo Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Undo the last change to a file.</summary>
    Task<bool> UndoAsync(string filePath);

    /// <summary>Undo a specific change by ID.</summary>
    Task<bool> UndoByIdAsync(Guid changeId);

    /// <summary>Undo all pending changes.</summary>
    Task<int> UndoAllAsync();

    /// <summary>Undo multiple changes by ID.</summary>
    Task<int> UndoMultipleAsync(IEnumerable<Guid> changeIds);

    // ═══════════════════════════════════════════════════════════════════════
    // Query Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Check if undo is available for a file.</summary>
    bool CanUndo(string filePath);

    /// <summary>Check if undo is available for a change ID.</summary>
    bool CanUndoById(Guid changeId);

    /// <summary>Get time remaining for undo on a file.</summary>
    TimeSpan GetTimeRemaining(string filePath);

    /// <summary>Get time remaining for a specific change.</summary>
    TimeSpan GetTimeRemainingById(Guid changeId);

    /// <summary>Get the undo state for a file.</summary>
    UndoState? GetUndoState(string filePath);

    /// <summary>Get the undo state for a change ID.</summary>
    UndoState? GetUndoStateById(Guid changeId);

    /// <summary>Get all pending undo states.</summary>
    IReadOnlyList<UndoState> GetAllPendingUndos();

    // ═══════════════════════════════════════════════════════════════════════
    // Timer Management
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Pause the countdown for a specific undo.</summary>
    bool PauseCountdown(Guid changeId);

    /// <summary>Resume the countdown for a paused undo.</summary>
    bool ResumeCountdown(Guid changeId);

    /// <summary>Extend the undo time for a change.</summary>
    bool ExtendTime(Guid changeId, TimeSpan additionalTime);

    /// <summary>Dismiss an undo without performing it.</summary>
    bool Dismiss(Guid changeId);

    // ═══════════════════════════════════════════════════════════════════════
    // State Management
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Register a new change for undo tracking.</summary>
    void RegisterChange(FileChangeRecord changeRecord);

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Raised when an undo becomes available.</summary>
    event EventHandler<UndoAvailableEventArgs>? UndoAvailable;

    /// <summary>Raised when an undo time window expires.</summary>
    event EventHandler<UndoExpiredEventArgs>? UndoExpired;

    /// <summary>Raised when an undo operation completes.</summary>
    event EventHandler<UndoCompletedEventArgs>? UndoCompleted;

    /// <summary>Raised periodically with updated time remaining.</summary>
    event EventHandler<TimeRemainingChangedEventArgs>? TimeRemainingChanged;

    /// <summary>Raised when all pending undos have expired.</summary>
    event EventHandler? AllUndosExpired;
}
