using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CHANGE HISTORY ITEM VIEW MODEL (v0.4.5h)                                 │
// │ ViewModel for individual change history items.                           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for individual change history items.
/// </summary>
public partial class ChangeHistoryItemViewModel : ViewModelBase
{
    private readonly IUndoManager _undoManager;
    private readonly IClipboardService _clipboardService;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The underlying change record.
    /// </summary>
    [ObservableProperty]
    private FileChangeRecord _record;

    /// <summary>
    /// Whether the item is selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Whether the item is expanded (showing diff).
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Whether an undo operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isUndoing;

    /// <summary>
    /// Whether this item was undone.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUndo))]
    private bool _wasUndone;

    /// <summary>
    /// Status message for feedback.
    /// </summary>
    [ObservableProperty]
    private string? _statusMessage;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// File name from the path.
    /// </summary>
    public string FileName => Record.FileName;

    /// <summary>
    /// Relative path within workspace.
    /// </summary>
    public string RelativePath => Record.RelativePath;

    /// <summary>
    /// Formatted relative time (e.g., "2 minutes ago").
    /// </summary>
    public string RelativeTime => FormatRelativeTime(Record.ChangedAt);

    /// <summary>
    /// Absolute time formatted for display.
    /// </summary>
    public string AbsoluteTime => Record.ChangedAt.ToLocalTime().ToString("g");

    /// <summary>
    /// Change type label for display.
    /// </summary>
    public string ChangeTypeLabel => Record.ChangeType switch
    {
        FileChangeType.Created => "Created",
        FileChangeType.Modified => "Modified",
        FileChangeType.Deleted => "Deleted",
        FileChangeType.Renamed => "Renamed",
        _ => "Changed"
    };

    /// <summary>
    /// Icon for the change type.
    /// </summary>
    public string ChangeTypeIcon => Record.ChangeType switch
    {
        FileChangeType.Created => "PlusCircleIcon",
        FileChangeType.Modified => "EditIcon",
        FileChangeType.Deleted => "TrashIcon",
        FileChangeType.Renamed => "ArrowRightIcon",
        _ => "FileIcon"
    };

    /// <summary>
    /// Whether undo is available.
    /// </summary>
    public bool CanUndo => !WasUndone && !IsUndoing && _undoManager.CanUndoById(Record.Id);

    /// <summary>
    /// Formatted time remaining for undo.
    /// </summary>
    public string TimeRemaining
    {
        get
        {
            if (WasUndone) return "Undone";
            var remaining = _undoManager.GetTimeRemainingById(Record.Id);
            if (remaining <= TimeSpan.Zero) return "Expired";
            return FormatTimeSpan(remaining);
        }
    }

    /// <summary>
    /// Progress percentage for undo time (0-100).
    /// </summary>
    public double TimeRemainingProgress
    {
        get
        {
            if (WasUndone) return 0;
            var state = _undoManager.GetUndoStateById(Record.Id);
            return state?.ProgressPercentage ?? 0;
        }
    }

    /// <summary>
    /// Whether undo is expiring soon (less than 60 seconds).
    /// </summary>
    public bool IsExpiringSoon
    {
        get
        {
            if (WasUndone) return false;
            var remaining = _undoManager.GetTimeRemainingById(Record.Id);
            return remaining > TimeSpan.Zero && remaining.TotalSeconds <= 60;
        }
    }

    /// <summary>
    /// Line stats formatted for display.
    /// </summary>
    public string LineStats => $"+{Record.LinesAdded} -{Record.LinesRemoved}";

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raised when view diff is requested.
    /// </summary>
    public event EventHandler<FileChangeRecord>? ViewDiffRequested;

    /// <summary>
    /// Raised when file navigation is requested.
    /// </summary>
    public event EventHandler<string>? NavigateToFileRequested;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    public ChangeHistoryItemViewModel(
        FileChangeRecord record,
        IUndoManager undoManager,
        IClipboardService clipboardService)
    {
        _record = record;
        _undoManager = undoManager;
        _clipboardService = clipboardService;
        _wasUndone = record.IsUndone;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Undo this change.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task UndoAsync()
    {
        if (IsUndoing) return;

        IsUndoing = true;
        try
        {
            var success = await _undoManager.UndoByIdAsync(Record.Id);
            if (success)
            {
                WasUndone = true;
                StatusMessage = "Undone";
            }
            else
            {
                StatusMessage = "Undo failed";
            }
        }
        finally
        {
            IsUndoing = false;
        }
    }

    /// <summary>
    /// View the diff for this change.
    /// </summary>
    [RelayCommand]
    private void ViewDiff()
    {
        ViewDiffRequested?.Invoke(this, Record);
    }

    /// <summary>
    /// Open the file in the editor.
    /// </summary>
    [RelayCommand]
    private void OpenFile()
    {
        NavigateToFileRequested?.Invoke(this, Record.FilePath);
    }

    /// <summary>
    /// Copy the file path to clipboard.
    /// </summary>
    [RelayCommand]
    private async Task CopyPathAsync()
    {
        await _clipboardService.SetTextAsync(Record.FilePath);
        StatusMessage = "Path copied";
        await ClearStatusAfterDelayAsync();
    }

    /// <summary>
    /// Toggle expanded state.
    /// </summary>
    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Update time-dependent properties.
    /// </summary>
    public void UpdateTimeRemaining()
    {
        OnPropertyChanged(nameof(RelativeTime));
        OnPropertyChanged(nameof(TimeRemaining));
        OnPropertyChanged(nameof(TimeRemainingProgress));
        OnPropertyChanged(nameof(IsExpiringSoon));
        OnPropertyChanged(nameof(CanUndo));
        UndoCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Mark this item as undone.
    /// </summary>
    public void MarkAsUndone()
    {
        WasUndone = true;
        OnPropertyChanged(nameof(TimeRemaining));
        OnPropertyChanged(nameof(TimeRemainingProgress));
        UndoCommand.NotifyCanExecuteChanged();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════════════

    private async Task ClearStatusAfterDelayAsync()
    {
        await Task.Delay(2000);
        StatusMessage = null;
    }

    private static string FormatRelativeTime(DateTime dateTime)
    {
        var elapsed = DateTime.UtcNow - dateTime;

        if (elapsed.TotalSeconds < 60)
            return "Just now";
        if (elapsed.TotalMinutes < 60)
            return $"{(int)elapsed.TotalMinutes} min ago";
        if (elapsed.TotalHours < 24)
            return $"{(int)elapsed.TotalHours} hours ago";
        if (elapsed.TotalDays < 7)
            return $"{(int)elapsed.TotalDays} days ago";

        return dateTime.ToLocalTime().ToString("MMM d");
    }

    private static string FormatTimeSpan(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return time.ToString(@"h\:mm\:ss");
        return time.ToString(@"mm\:ss");
    }
}
