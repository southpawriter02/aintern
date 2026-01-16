using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CONFLICT WARNING DIALOG VIEW MODEL (v0.4.3g)                             │
// │ ViewModel for the Conflict Warning dialog.                               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for the Conflict Warning dialog.
/// Presents conflict information and resolution options to the user.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3g.</para>
/// </remarks>
public partial class ConflictWarningDialogViewModel : ViewModelBase
{
    private Action<ConflictResolution>? _closeAction;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Gets the full path to the conflicting file.</summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>Gets the file name for display.</summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>Gets the human-readable conflict message.</summary>
    [ObservableProperty]
    private string _conflictMessage = string.Empty;

    /// <summary>Gets the specific reason for the conflict.</summary>
    [ObservableProperty]
    private ConflictReason _conflictReason;

    /// <summary>Gets the file's last modification time.</summary>
    [ObservableProperty]
    private DateTime _lastModified;

    /// <summary>Gets when the original snapshot was taken.</summary>
    [ObservableProperty]
    private DateTime _snapshotTime;

    /// <summary>Gets the user's selected resolution.</summary>
    [ObservableProperty]
    private ConflictResolution _selectedResolution = ConflictResolution.Cancel;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Gets the time elapsed since the snapshot was taken.</summary>
    public TimeSpan TimeSinceSnapshot => DateTime.UtcNow - SnapshotTime;

    /// <summary>Gets a human-readable description of time since snapshot.</summary>
    public string TimeSinceSnapshotText => FormatTimeSpan(TimeSinceSnapshot);

    /// <summary>Gets a detailed explanation based on conflict reason.</summary>
    public string ConflictExplanation => ConflictReason switch
    {
        ConflictReason.FileCreated =>
            "This file was created after the proposal was generated. " +
            "Force applying will overwrite the newly created file.",

        ConflictReason.FileDeleted =>
            "This file was deleted after the proposal was generated. " +
            "Force applying will recreate the file with the proposed content.",

        ConflictReason.ContentModified =>
            "This file has been modified since the proposal was generated. " +
            "Force applying may overwrite recent edits.",

        ConflictReason.PermissionChanged =>
            "File permissions have changed since the proposal was generated. " +
            "You may need to check file access before applying.",

        _ =>
            "The file state has changed since the proposal was generated. " +
            "Review the conflict details before proceeding."
    };

    /// <summary>Gets whether force apply should show extra warning.</summary>
    public bool IsDestructiveConflict => ConflictReason is
        ConflictReason.ContentModified or
        ConflictReason.FileCreated;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance for design-time or testing.
    /// </summary>
    public ConflictWarningDialogViewModel()
    {
    }

    /// <summary>
    /// Initializes a new instance with conflict information.
    /// </summary>
    public ConflictWarningDialogViewModel(
        ConflictInfo conflict,
        string filePath,
        Action<ConflictResolution> closeAction)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path required", nameof(filePath));

        _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));

        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        ConflictMessage = conflict.Message ?? GetDefaultMessage(conflict.Reason);
        ConflictReason = conflict.Reason;
        LastModified = conflict.LastModified ?? DateTime.UtcNow;
        SnapshotTime = conflict.SnapshotTime ?? DateTime.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Selects the Refresh Diff resolution.</summary>
    [RelayCommand]
    private void RefreshDiff()
    {
        SelectedResolution = ConflictResolution.RefreshDiff;
        _closeAction?.Invoke(SelectedResolution);
    }

    /// <summary>Selects the Force Apply resolution.</summary>
    [RelayCommand]
    private void ForceApply()
    {
        SelectedResolution = ConflictResolution.ForceApply;
        _closeAction?.Invoke(SelectedResolution);
    }

    /// <summary>Selects the Cancel resolution.</summary>
    [RelayCommand]
    private void Cancel()
    {
        SelectedResolution = ConflictResolution.Cancel;
        _closeAction?.Invoke(SelectedResolution);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Sets the close action.</summary>
    public void SetCloseAction(Action<ConflictResolution> closeAction)
    {
        _closeAction = closeAction;
    }

    private static string GetDefaultMessage(ConflictReason reason) => reason switch
    {
        ConflictReason.FileCreated => "File was created after the proposal was generated",
        ConflictReason.FileDeleted => "File was deleted after the proposal was generated",
        ConflictReason.ContentModified => "File content has been modified",
        ConflictReason.PermissionChanged => "File permissions have changed",
        _ => "File has been modified"
    };

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 60)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago";

        return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago";
    }
}
