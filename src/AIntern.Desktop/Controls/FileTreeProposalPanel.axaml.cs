using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Controls;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PROPOSAL PANEL (v0.4.4e)                                      │
// │ Code-behind for the main proposal panel control.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Code-behind for the FileTreeProposalPanel control.
/// Handles keyboard shortcuts and event forwarding.
/// </summary>
/// <remarks>
/// <para>
/// This control provides:
/// <list type="bullet">
/// <item>Keyboard shortcuts (Ctrl+A, Ctrl+D, Ctrl+P, Ctrl+Enter, Esc)</item>
/// <item>ViewModel event subscriptions</item>
/// <item>Routed events for undo toast and errors</item>
/// </list>
/// </para>
/// <para>Added in v0.4.4e.</para>
/// </remarks>
public partial class FileTreeProposalPanel : UserControl
{
    private readonly ILogger<FileTreeProposalPanel>? _logger;
    private FileTreeProposalViewModel? _viewModel;

    /// <summary>
    /// Creates a new FileTreeProposalPanel.
    /// </summary>
    public FileTreeProposalPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Handles DataContext changes to subscribe/unsubscribe from ViewModel events.
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old ViewModel
        if (_viewModel != null)
        {
            _viewModel.PreviewRequested -= OnPreviewRequested;
            _viewModel.ApplyCompleted -= OnApplyCompleted;
            _viewModel.ApplyCancelled -= OnApplyCancelled;
            _logger?.LogDebug("Unsubscribed from old ViewModel events");
        }

        // Subscribe to new ViewModel
        _viewModel = DataContext as FileTreeProposalViewModel;
        if (_viewModel != null)
        {
            _viewModel.PreviewRequested += OnPreviewRequested;
            _viewModel.ApplyCompleted += OnApplyCompleted;
            _viewModel.ApplyCancelled += OnApplyCancelled;
            _logger?.LogDebug("Subscribed to new ViewModel events");
        }
    }

    /// <summary>
    /// Handles keyboard shortcuts.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        var modifiers = e.KeyModifiers;

        switch (e.Key)
        {
            // Ctrl+A: Select All
            case Key.A when modifiers.HasFlag(KeyModifiers.Control):
                if (_viewModel.SelectAllCommand.CanExecute(null))
                {
                    _viewModel.SelectAllCommand.Execute(null);
                    e.Handled = true;
                    _logger?.LogDebug("Keyboard shortcut: Select All");
                }
                break;

            // Ctrl+D: Deselect All
            case Key.D when modifiers.HasFlag(KeyModifiers.Control):
                if (_viewModel.DeselectAllCommand.CanExecute(null))
                {
                    _viewModel.DeselectAllCommand.Execute(null);
                    e.Handled = true;
                    _logger?.LogDebug("Keyboard shortcut: Deselect All");
                }
                break;

            // Ctrl+P: Preview
            case Key.P when modifiers.HasFlag(KeyModifiers.Control):
                if (_viewModel.PreviewCommand.CanExecute(null))
                {
                    _ = _viewModel.PreviewCommand.ExecuteAsync(null);
                    e.Handled = true;
                    _logger?.LogDebug("Keyboard shortcut: Preview");
                }
                break;

            // Ctrl+Enter: Apply
            case Key.Enter when modifiers.HasFlag(KeyModifiers.Control):
                if (_viewModel.ApplyCommand.CanExecute(null))
                {
                    _ = _viewModel.ApplyCommand.ExecuteAsync(null);
                    e.Handled = true;
                    _logger?.LogDebug("Keyboard shortcut: Apply");
                }
                break;

            // Esc: Cancel
            case Key.Escape:
                if (_viewModel.CancelCommand.CanExecute(null))
                {
                    _viewModel.CancelCommand.Execute(null);
                    e.Handled = true;
                    _logger?.LogDebug("Keyboard shortcut: Cancel");
                }
                break;
        }
    }

    /// <summary>
    /// Handles preview requested event from ViewModel.
    /// </summary>
    private void OnPreviewRequested(object? sender, IReadOnlyList<DiffResult> diffs)
    {
        _logger?.LogInformation("Preview requested with {Count} diffs", diffs.Count);
        // TODO: Open batch preview dialog
        // This will be implemented when the BatchPreviewDialog is available
    }

    /// <summary>
    /// Handles apply completed event from ViewModel.
    /// </summary>
    private void OnApplyCompleted(object? sender, BatchApplyResult result)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _logger?.LogInformation(
                "Apply completed: Success={Success}, Failed={Failed}",
                result.SuccessCount,
                result.FailedCount);

            if (result.AllSucceeded)
            {
                RaiseEvent(new UndoToastRequestedEventArgs(result));
            }
            else
            {
                RaiseEvent(new ApplyErrorEventArgs(result));
            }
        });
    }

    /// <summary>
    /// Handles apply cancelled event from ViewModel.
    /// </summary>
    private void OnApplyCancelled(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _logger?.LogWarning("Apply operation was cancelled");
            RaiseEvent(new ApplyCancelledEventArgs());
        });
    }

    /// <summary>
    /// Cleans up event subscriptions when unloaded.
    /// </summary>
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_viewModel != null)
        {
            _viewModel.PreviewRequested -= OnPreviewRequested;
            _viewModel.ApplyCompleted -= OnApplyCompleted;
            _viewModel.ApplyCancelled -= OnApplyCancelled;
            _logger?.LogDebug("Cleaned up ViewModel event subscriptions");
        }
    }

    #region Routed Events

    /// <summary>
    /// Event raised when an undo toast should be shown.
    /// </summary>
    public static readonly RoutedEvent<UndoToastRequestedEventArgs> UndoToastRequestedEvent =
        RoutedEvent.Register<FileTreeProposalPanel, UndoToastRequestedEventArgs>(
            nameof(UndoToastRequested),
            RoutingStrategies.Bubble);

    /// <summary>
    /// Raised when an undo toast should be shown.
    /// </summary>
    public event EventHandler<UndoToastRequestedEventArgs> UndoToastRequested
    {
        add => AddHandler(UndoToastRequestedEvent, value);
        remove => RemoveHandler(UndoToastRequestedEvent, value);
    }

    /// <summary>
    /// Event raised when apply errors occur.
    /// </summary>
    public static readonly RoutedEvent<ApplyErrorEventArgs> ApplyErrorEvent =
        RoutedEvent.Register<FileTreeProposalPanel, ApplyErrorEventArgs>(
            nameof(ApplyError),
            RoutingStrategies.Bubble);

    /// <summary>
    /// Raised when apply errors occur.
    /// </summary>
    public event EventHandler<ApplyErrorEventArgs> ApplyError
    {
        add => AddHandler(ApplyErrorEvent, value);
        remove => RemoveHandler(ApplyErrorEvent, value);
    }

    /// <summary>
    /// Event raised when apply is cancelled.
    /// </summary>
    public static readonly RoutedEvent<ApplyCancelledEventArgs> ApplyCancelledEvent =
        RoutedEvent.Register<FileTreeProposalPanel, ApplyCancelledEventArgs>(
            nameof(ApplyCancelledRouted),
            RoutingStrategies.Bubble);

    /// <summary>
    /// Raised when apply is cancelled.
    /// </summary>
    public event EventHandler<ApplyCancelledEventArgs> ApplyCancelledRouted
    {
        add => AddHandler(ApplyCancelledEvent, value);
        remove => RemoveHandler(ApplyCancelledEvent, value);
    }

    #endregion
}

/// <summary>
/// Event args for undo toast request.
/// </summary>
public class UndoToastRequestedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// The batch apply result.
    /// </summary>
    public BatchApplyResult Result { get; }

    /// <summary>
    /// Creates new UndoToastRequestedEventArgs.
    /// </summary>
    public UndoToastRequestedEventArgs(BatchApplyResult result)
        : base(FileTreeProposalPanel.UndoToastRequestedEvent)
    {
        Result = result;
    }
}

/// <summary>
/// Event args for apply errors.
/// </summary>
public class ApplyErrorEventArgs : RoutedEventArgs
{
    /// <summary>
    /// The batch apply result.
    /// </summary>
    public BatchApplyResult Result { get; }

    /// <summary>
    /// Creates new ApplyErrorEventArgs.
    /// </summary>
    public ApplyErrorEventArgs(BatchApplyResult result)
        : base(FileTreeProposalPanel.ApplyErrorEvent)
    {
        Result = result;
    }
}

/// <summary>
/// Event args for apply cancellation.
/// </summary>
public class ApplyCancelledEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Creates new ApplyCancelledEventArgs.
    /// </summary>
    public ApplyCancelledEventArgs()
        : base(FileTreeProposalPanel.ApplyCancelledEvent)
    {
    }
}
