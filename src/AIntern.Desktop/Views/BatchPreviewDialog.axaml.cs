using Avalonia.Controls;
using Avalonia.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Views;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BATCH PREVIEW DIALOG (v0.4.4f)                                          │
// │ Modal dialog for previewing all file changes in a batch proposal.       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Modal dialog for previewing all file changes in a batch proposal.
/// </summary>
/// <remarks>
/// <para>
/// Provides keyboard navigation:
/// <list type="bullet">
/// <item>Left/Right arrows: Navigate between files</item>
/// <item>Home/End: Jump to first/last file</item>
/// <item>1-9: Quick navigation to file by index</item>
/// <item>Enter: Apply all files</item>
/// <item>Escape: Cancel</item>
/// </list>
/// </para>
/// <para>Added in v0.4.4f.</para>
/// </remarks>
public partial class BatchPreviewDialog : Window
{
    private BatchPreviewDialogViewModel? _viewModel;
    private readonly ILogger<BatchPreviewDialog>? _logger;

    /// <summary>
    /// Creates a new BatchPreviewDialog with the specified diffs.
    /// </summary>
    /// <param name="diffs">The diff results to preview.</param>
    /// <param name="diffService">The diff service.</param>
    /// <param name="inlineDiffService">The inline diff service.</param>
    /// <param name="logger">Optional logger.</param>
    public BatchPreviewDialog(
        IReadOnlyList<DiffResult> diffs,
        IDiffService diffService,
        IInlineDiffService inlineDiffService,
        ILogger<BatchPreviewDialog>? logger = null)
    {
        InitializeComponent();
        _logger = logger;

        _logger?.LogInformation("Opening BatchPreviewDialog with {Count} diffs", diffs.Count);

        _viewModel = new BatchPreviewDialogViewModel(diffs, this, diffService, inlineDiffService);
        DataContext = _viewModel;

        // Setup keyboard handling
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Parameterless constructor for XAML designer support.
    /// </summary>
    public BatchPreviewDialog()
    {
        InitializeComponent();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel == null)
            return;

        switch (e.Key)
        {
            case Key.Left:
            case Key.PageUp when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                if (_viewModel.PreviousFileCommand.CanExecute(null))
                {
                    _viewModel.PreviousFileCommand.Execute(null);
                    e.Handled = true;
                    _logger?.LogDebug("Keyboard: Previous file");
                }
                break;

            case Key.Right:
            case Key.PageDown when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                if (_viewModel.NextFileCommand.CanExecute(null))
                {
                    _viewModel.NextFileCommand.Execute(null);
                    e.Handled = true;
                    _logger?.LogDebug("Keyboard: Next file");
                }
                break;

            case Key.Home:
                _viewModel.GoToFileCommand.Execute(0);
                e.Handled = true;
                _logger?.LogDebug("Keyboard: First file");
                break;

            case Key.End:
                _viewModel.GoToFileCommand.Execute(_viewModel.TotalFiles - 1);
                e.Handled = true;
                _logger?.LogDebug("Keyboard: Last file");
                break;

            // Number keys 1-9 for quick navigation
            case Key.D1:
            case Key.D2:
            case Key.D3:
            case Key.D4:
            case Key.D5:
            case Key.D6:
            case Key.D7:
            case Key.D8:
            case Key.D9:
                var index = e.Key - Key.D1;
                if (index < _viewModel.TotalFiles)
                {
                    _viewModel.GoToFileCommand.Execute(index);
                    e.Handled = true;
                    _logger?.LogDebug("Keyboard: File {Index}", index + 1);
                }
                break;

            case Key.NumPad1:
            case Key.NumPad2:
            case Key.NumPad3:
            case Key.NumPad4:
            case Key.NumPad5:
            case Key.NumPad6:
            case Key.NumPad7:
            case Key.NumPad8:
            case Key.NumPad9:
                var numIndex = e.Key - Key.NumPad1;
                if (numIndex < _viewModel.TotalFiles)
                {
                    _viewModel.GoToFileCommand.Execute(numIndex);
                    e.Handled = true;
                    _logger?.LogDebug("Keyboard: File {Index} (numpad)", numIndex + 1);
                }
                break;
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        KeyDown -= OnKeyDown;
        _logger?.LogDebug("BatchPreviewDialog closing");
    }
}
