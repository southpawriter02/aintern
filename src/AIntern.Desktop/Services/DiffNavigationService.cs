using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF NAVIGATION SERVICE (v0.4.2g)                                        │
// │ Manages navigation state for diff hunks.                                 │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Manages navigation state for diff hunks, tracking current position
/// and providing methods to navigate between hunks.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2g.</para>
/// <para>
/// This service is instantiated per-diff session and maintains the current
/// hunk index. It raises events when navigation occurs to allow the UI
/// to scroll to the appropriate hunk.
/// </para>
/// </remarks>
public sealed class DiffNavigationService
{
    private readonly ILogger<DiffNavigationService>? _logger;
    private readonly List<DiffHunk> _hunks = new();
    private int _currentIndex = -1;

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Event raised when the current hunk changes.
    /// </summary>
    public event EventHandler<HunkChangedEventArgs>? CurrentHunkChanged;

    // ═══════════════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the zero-based index of the current hunk, or -1 if no hunks are loaded.
    /// </summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>
    /// Gets the total number of hunks.
    /// </summary>
    public int TotalHunks => _hunks.Count;

    /// <summary>
    /// Gets whether there are any hunks to navigate.
    /// </summary>
    public bool HasHunks => _hunks.Count > 0;

    /// <summary>
    /// Gets the current hunk, or null if no hunks are loaded or index is invalid.
    /// </summary>
    public DiffHunk? CurrentHunk => _currentIndex >= 0 && _currentIndex < _hunks.Count
        ? _hunks[_currentIndex]
        : null;

    /// <summary>
    /// Gets whether navigation to the next hunk is possible.
    /// </summary>
    public bool CanMoveNext => _currentIndex < _hunks.Count - 1;

    /// <summary>
    /// Gets whether navigation to the previous hunk is possible.
    /// </summary>
    public bool CanMovePrevious => _currentIndex > 0;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructors
    // ═══════════════════════════════════════════════════════════════════════

    public DiffNavigationService()
    {
    }

    public DiffNavigationService(ILogger<DiffNavigationService> logger)
    {
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Initialization Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes the navigation service with a collection of hunks.
    /// Resets the current index to the first hunk (0) if hunks exist, or -1 if empty.
    /// </summary>
    public void SetHunks(IEnumerable<DiffHunk> hunks)
    {
        ArgumentNullException.ThrowIfNull(hunks);

        var previousIndex = _currentIndex;
        var previousHunk = CurrentHunk;

        _hunks.Clear();
        _hunks.AddRange(hunks);
        _currentIndex = _hunks.Count > 0 ? 0 : -1;

        _logger?.LogDebug("SetHunks: {Count} hunks, currentIndex={Index}", _hunks.Count, _currentIndex);

        if (_currentIndex != previousIndex || CurrentHunk != previousHunk)
        {
            OnCurrentHunkChanged(previousIndex, _currentIndex);
        }
    }

    /// <summary>
    /// Clears all hunks and resets navigation state.
    /// </summary>
    public void Clear()
    {
        var previousIndex = _currentIndex;

        _hunks.Clear();
        _currentIndex = -1;

        _logger?.LogDebug("Clear: navigation reset");

        if (previousIndex != -1)
        {
            OnCurrentHunkChanged(previousIndex, -1);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Navigation Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Navigates to the next hunk if available.
    /// </summary>
    /// <returns>True if navigation occurred, false if already at the last hunk.</returns>
    public bool MoveNext()
    {
        if (!CanMoveNext)
        {
            _logger?.LogTrace("MoveNext: already at last hunk");
            return false;
        }

        var previousIndex = _currentIndex;
        _currentIndex++;
        _logger?.LogDebug("MoveNext: {Previous} → {Current}", previousIndex, _currentIndex);
        OnCurrentHunkChanged(previousIndex, _currentIndex);
        return true;
    }

    /// <summary>
    /// Navigates to the previous hunk if available.
    /// </summary>
    /// <returns>True if navigation occurred, false if already at the first hunk.</returns>
    public bool MovePrevious()
    {
        if (!CanMovePrevious)
        {
            _logger?.LogTrace("MovePrevious: already at first hunk");
            return false;
        }

        var previousIndex = _currentIndex;
        _currentIndex--;
        _logger?.LogDebug("MovePrevious: {Previous} → {Current}", previousIndex, _currentIndex);
        OnCurrentHunkChanged(previousIndex, _currentIndex);
        return true;
    }

    /// <summary>
    /// Navigates directly to a specific hunk by index.
    /// </summary>
    /// <returns>True if navigation occurred, false if index is out of range.</returns>
    public bool MoveTo(int index)
    {
        if (index < 0 || index >= _hunks.Count)
        {
            _logger?.LogWarning("MoveTo: index {Index} out of range [0, {Max})", index, _hunks.Count);
            return false;
        }

        if (index == _currentIndex)
        {
            return true; // Already at the target
        }

        var previousIndex = _currentIndex;
        _currentIndex = index;
        _logger?.LogDebug("MoveTo: {Previous} → {Current}", previousIndex, _currentIndex);
        OnCurrentHunkChanged(previousIndex, _currentIndex);
        return true;
    }

    /// <summary>
    /// Navigates to the first hunk.
    /// </summary>
    public bool MoveToFirst() => MoveTo(0);

    /// <summary>
    /// Navigates to the last hunk.
    /// </summary>
    public bool MoveToLast() => MoveTo(_hunks.Count - 1);

    // ═══════════════════════════════════════════════════════════════════════
    // Query Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Finds the index of the hunk containing the specified line number.
    /// </summary>
    /// <returns>The index of the containing hunk, or -1 if not found.</returns>
    public int FindHunkContainingLine(int lineNumber, DiffSide side)
    {
        for (int i = 0; i < _hunks.Count; i++)
        {
            var hunk = _hunks[i];
            var start = side == DiffSide.Original
                ? hunk.OriginalStartLine
                : hunk.ProposedStartLine;
            var count = side == DiffSide.Original
                ? hunk.OriginalLineCount
                : hunk.ProposedLineCount;

            if (lineNumber >= start && lineNumber < start + count)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets the hunk at the specified index without changing the current position.
    /// </summary>
    public DiffHunk? GetHunkAt(int index)
    {
        if (index >= 0 && index < _hunks.Count)
        {
            return _hunks[index];
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════════════

    private void OnCurrentHunkChanged(int previousIndex, int newIndex)
    {
        CurrentHunkChanged?.Invoke(this, new HunkChangedEventArgs(
            previousIndex,
            newIndex,
            GetHunkAt(previousIndex),
            GetHunkAt(newIndex)
        ));
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Event Args
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Event arguments for when the current hunk changes.
/// </summary>
public sealed class HunkChangedEventArgs : EventArgs
{
    public int PreviousIndex { get; }
    public int NewIndex { get; }
    public DiffHunk? PreviousHunk { get; }
    public DiffHunk? NewHunk { get; }

    public HunkChangedEventArgs(
        int previousIndex,
        int newIndex,
        DiffHunk? previousHunk,
        DiffHunk? newHunk)
    {
        PreviousIndex = previousIndex;
        NewIndex = newIndex;
        PreviousHunk = previousHunk;
        NewHunk = newHunk;
    }
}
