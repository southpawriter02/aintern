using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APPLY PROGRESS VIEW MODEL (v0.4.4g)                                     │
// │ Manages progress overlay state during batch apply operations.           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for the progress overlay shown during batch apply operations.
/// </summary>
/// <remarks>
/// <para>
/// Manages:
/// <list type="bullet">
/// <item>Visibility and phase state</item>
/// <item>Progress percentage and file counts</item>
/// <item>Cancellation support</item>
/// <item>Auto-hide after completion</item>
/// </list>
/// </para>
/// <para>Added in v0.4.4g.</para>
/// </remarks>
public partial class ApplyProgressViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<ApplyProgressViewModel>? _logger;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _autoHideCts;
    private const int AutoHideDelayMs = 1500;

    #region Observable Properties

    /// <summary>
    /// Whether the overlay is visible.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    private bool _isVisible;

    /// <summary>
    /// Current phase of the batch apply operation.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhaseTitle))]
    [NotifyPropertyChangedFor(nameof(IsIndeterminate))]
    [NotifyPropertyChangedFor(nameof(IsCompleted))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private BatchApplyPhase _phase;

    /// <summary>
    /// The current file being processed.
    /// </summary>
    [ObservableProperty]
    private string _currentFile = string.Empty;

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercentText))]
    private double _progressPercent;

    /// <summary>
    /// Number of completed operations.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileCountText))]
    private int _completedCount;

    /// <summary>
    /// Total number of operations.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileCountText))]
    private int _totalCount;

    /// <summary>
    /// Whether the operation can be cancelled.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCancelButton))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _canCancel = true;

    /// <summary>
    /// Error message if an error occurred.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    /// <summary>
    /// Whether cancellation was requested.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCancelButton))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _cancellationRequested;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Human-readable phase title.
    /// </summary>
    public string PhaseTitle => Phase switch
    {
        BatchApplyPhase.Validating => "Validating...",
        BatchApplyPhase.CreatingBackups => "Creating Backups...",
        BatchApplyPhase.CreatingDirectories => "Creating Directories...",
        BatchApplyPhase.WritingFiles => "Writing Files...",
        BatchApplyPhase.Finalizing => "Finalizing...",
        BatchApplyPhase.Completed => "Complete!",
        BatchApplyPhase.RollingBack => "Rolling Back...",
        _ => "Processing..."
    };

    /// <summary>
    /// Whether the progress bar should be indeterminate.
    /// </summary>
    public bool IsIndeterminate => Phase is
        BatchApplyPhase.Validating or
        BatchApplyPhase.Finalizing;

    /// <summary>
    /// Whether the operation is actively in progress.
    /// </summary>
    public bool IsActive => IsVisible && !IsCompleted;

    /// <summary>
    /// Whether the operation completed successfully.
    /// </summary>
    public bool IsCompleted => Phase == BatchApplyPhase.Completed;

    /// <summary>
    /// Whether an error occurred or rollback is in progress.
    /// </summary>
    public bool IsError => Phase == BatchApplyPhase.RollingBack;

    /// <summary>
    /// Whether there is an error message to display.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Whether to show the cancel button.
    /// </summary>
    public bool ShowCancelButton => CanCancel && !CancellationRequested && !IsError && !IsCompleted;

    /// <summary>
    /// Formatted progress percentage text.
    /// </summary>
    public string ProgressPercentText => $"{ProgressPercent:F0}%";

    /// <summary>
    /// Formatted file count text.
    /// </summary>
    public string FileCountText => TotalCount > 0
        ? $"{CompletedCount} of {TotalCount} files"
        : string.Empty;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new ApplyProgressViewModel.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public ApplyProgressViewModel(ILogger<ApplyProgressViewModel>? logger = null)
    {
        _logger = logger;
    }

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Start showing progress for a new operation.
    /// </summary>
    /// <param name="cts">The cancellation token source for cancellation support.</param>
    /// <param name="totalOperations">Total number of operations to process.</param>
    public void Start(CancellationTokenSource cts, int totalOperations = 0)
    {
        _logger?.LogInformation("Starting progress overlay for {Count} operations", totalOperations);

        // Cancel any pending auto-hide
        _autoHideCts?.Cancel();
        _autoHideCts = null;

        _cts = cts;

        // Reset all state
        Phase = BatchApplyPhase.Validating;
        CurrentFile = string.Empty;
        ProgressPercent = 0;
        CompletedCount = 0;
        TotalCount = totalOperations;
        CanCancel = true;
        CancellationRequested = false;
        ErrorMessage = null;

        IsVisible = true;
    }

    /// <summary>
    /// Update progress from a BatchApplyProgress report.
    /// </summary>
    /// <param name="progress">The progress information.</param>
    public void Update(BatchApplyProgress progress)
    {
        // Ensure we're on the UI thread
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Update(progress));
            return;
        }

        _logger?.LogTrace("Progress update: {Phase} - {File} ({Percent}%)",
            progress.Phase, progress.CurrentFile, progress.ProgressPercent);

        Phase = progress.Phase;
        CurrentFile = progress.CurrentFile;
        ProgressPercent = progress.ProgressPercent;
        CompletedCount = progress.CompletedOperations;
        TotalCount = progress.TotalOperations;
        CanCancel = progress.CanCancel;
        CancellationRequested = progress.CancellationRequested;
    }

    /// <summary>
    /// Mark the operation as complete and start auto-hide timer.
    /// </summary>
    public void Complete()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(Complete);
            return;
        }

        _logger?.LogInformation("Progress complete, starting auto-hide timer");

        Phase = BatchApplyPhase.Completed;
        ProgressPercent = 100;
        CanCancel = false;
        CurrentFile = string.Empty;

        StartAutoHide();
    }

    /// <summary>
    /// Mark the operation as failed and show error.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    public void Error(string message)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Error(message));
            return;
        }

        _logger?.LogError("Progress error: {Message}", message);

        Phase = BatchApplyPhase.RollingBack;
        ErrorMessage = message;
        CanCancel = false;
    }

    /// <summary>
    /// Mark rollback as complete and hide.
    /// </summary>
    public void RollbackComplete()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RollbackComplete);
            return;
        }

        _logger?.LogInformation("Rollback complete, hiding overlay");

        CurrentFile = "Rollback complete";
        StartAutoHide();
    }

    /// <summary>
    /// Immediately hide the overlay.
    /// </summary>
    public void Hide()
    {
        _logger?.LogDebug("Hiding progress overlay");

        _autoHideCts?.Cancel();
        _autoHideCts = null;
        IsVisible = false;
    }

    #endregion

    #region Private Methods

    private void StartAutoHide()
    {
        _autoHideCts?.Cancel();
        _autoHideCts = new CancellationTokenSource();

        var token = _autoHideCts.Token;

        Task.Delay(AutoHideDelayMs, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested)
            {
                Dispatcher.UIThread.Post(() => IsVisible = false);
            }
        }, TaskScheduler.Default);
    }

    #endregion

    #region Commands

    private bool CanExecuteCancel() => ShowCancelButton;

    /// <summary>
    /// Cancel the current operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteCancel))]
    private void Cancel()
    {
        if (_cts == null || _cts.IsCancellationRequested)
        {
            return;
        }

        _logger?.LogInformation("User requested cancellation");

        _cts.Cancel();
        CancellationRequested = true;
        CanCancel = false;
        Phase = BatchApplyPhase.RollingBack;
        CurrentFile = "Cancelling...";
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public void Dispose()
    {
        _autoHideCts?.Cancel();
        _autoHideCts?.Dispose();
        _autoHideCts = null;
        _cts = null;

        GC.SuppressFinalize(this);
    }

    #endregion
}
