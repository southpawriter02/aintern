using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BATCH PREVIEW DIALOG VIEW MODEL (v0.4.4f)                               │
// │ Manages state and navigation for the batch preview dialog.              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for the batch preview dialog, managing navigation and actions.
/// </summary>
/// <remarks>
/// <para>
/// Provides:
/// <list type="bullet">
/// <item>Collection of diff previews for all files</item>
/// <item>Selected preview tracking and navigation</item>
/// <item>Summary statistics (total, new, modified files)</item>
/// <item>Apply and cancel commands</item>
/// </list>
/// </para>
/// <para>Added in v0.4.4f.</para>
/// </remarks>
public partial class BatchPreviewDialogViewModel : ViewModelBase
{
    private readonly Window _dialog;
    private readonly ILogger<BatchPreviewDialogViewModel>? _logger;

    #region Observable Properties

    /// <summary>
    /// Collection of diff previews for all files in the batch.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DiffPreviewViewModel> _diffPreviews = new();

    /// <summary>
    /// Currently selected preview for display in the diff viewer.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNavigatePrevious))]
    [NotifyPropertyChangedFor(nameof(CanNavigateNext))]
    [NotifyCanExecuteChangedFor(nameof(NextFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousFileCommand))]
    private DiffPreviewViewModel? _selectedPreview;

    /// <summary>
    /// Index of the currently selected preview.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNavigatePrevious))]
    [NotifyPropertyChangedFor(nameof(CanNavigateNext))]
    [NotifyCanExecuteChangedFor(nameof(NextFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousFileCommand))]
    private int _selectedIndex;

    /// <summary>
    /// Whether an apply operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isApplying;

    /// <summary>
    /// Progress percentage of the apply operation.
    /// </summary>
    [ObservableProperty]
    private double _progress;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Total number of files in the preview.
    /// </summary>
    public int TotalFiles => DiffPreviews.Count;

    /// <summary>
    /// Number of new files (files that don't exist yet).
    /// </summary>
    public int NewFiles => DiffPreviews.Count(d => d.IsNewFile);

    /// <summary>
    /// Number of modified files (files that already exist).
    /// </summary>
    public int ModifiedFiles => DiffPreviews.Count(d => !d.IsNewFile);

    /// <summary>
    /// Whether there are any modified files.
    /// </summary>
    public bool HasModifiedFiles => ModifiedFiles > 0;

    /// <summary>
    /// Total lines added across all files.
    /// </summary>
    public int TotalAddedLines => DiffPreviews.Sum(d => d.AddedLines);

    /// <summary>
    /// Total lines removed across all files.
    /// </summary>
    public int TotalRemovedLines => DiffPreviews.Sum(d => d.RemovedLines);

    /// <summary>
    /// Whether there are multiple files to navigate between.
    /// </summary>
    public bool HasMultipleFiles => TotalFiles > 1;

    /// <summary>
    /// Whether navigation to previous file is possible.
    /// </summary>
    public bool CanNavigatePrevious => SelectedIndex > 0;

    /// <summary>
    /// Whether navigation to next file is possible.
    /// </summary>
    public bool CanNavigateNext => SelectedIndex < TotalFiles - 1;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new BatchPreviewDialogViewModel.
    /// </summary>
    /// <param name="diffs">The diff results to preview.</param>
    /// <param name="dialog">The parent dialog window.</param>
    /// <param name="diffService">The diff service for diff viewers.</param>
    /// <param name="inlineDiffService">The inline diff service for diff viewers.</param>
    /// <param name="logger">Optional logger.</param>
    public BatchPreviewDialogViewModel(
        IReadOnlyList<DiffResult> diffs,
        Window dialog,
        IDiffService diffService,
        IInlineDiffService inlineDiffService,
        ILogger<BatchPreviewDialogViewModel>? logger = null)
    {
        _dialog = dialog;
        _logger = logger;

        _logger?.LogInformation("Creating BatchPreviewDialogViewModel with {Count} diffs", diffs.Count);

        // Create preview ViewModels for each diff
        foreach (var diff in diffs)
        {
            DiffPreviews.Add(new DiffPreviewViewModel(diff, diffService, inlineDiffService));
        }

        // Select first file by default
        if (DiffPreviews.Count > 0)
        {
            SelectedPreview = DiffPreviews[0];
            SelectedIndex = 0;
            _logger?.LogDebug("Selected first file: {FileName}", SelectedPreview.FileName);
        }

        _logger?.LogDebug(
            "Preview stats: {Total} total, {New} new, {Modified} modified, +{Added} -{Removed} lines",
            TotalFiles, NewFiles, ModifiedFiles, TotalAddedLines, TotalRemovedLines);
    }

    #endregion

    #region Navigation Commands

    /// <summary>
    /// Navigates to the next file in the list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigateNext))]
    private void NextFile()
    {
        if (SelectedIndex < DiffPreviews.Count - 1)
        {
            SelectedIndex++;
            SelectedPreview = DiffPreviews[SelectedIndex];
            _logger?.LogDebug("Navigated to next file: {FileName} ({Index}/{Total})",
                SelectedPreview?.FileName, SelectedIndex + 1, TotalFiles);
        }
    }

    /// <summary>
    /// Navigates to the previous file in the list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigatePrevious))]
    private void PreviousFile()
    {
        if (SelectedIndex > 0)
        {
            SelectedIndex--;
            SelectedPreview = DiffPreviews[SelectedIndex];
            _logger?.LogDebug("Navigated to previous file: {FileName} ({Index}/{Total})",
                SelectedPreview?.FileName, SelectedIndex + 1, TotalFiles);
        }
    }

    /// <summary>
    /// Navigates to a specific file by index.
    /// </summary>
    /// <param name="index">The index of the file to navigate to.</param>
    [RelayCommand]
    private void GoToFile(int index)
    {
        if (index >= 0 && index < DiffPreviews.Count)
        {
            SelectedIndex = index;
            SelectedPreview = DiffPreviews[index];
            _logger?.LogDebug("Navigated to file at index {Index}: {FileName}",
                index, SelectedPreview?.FileName);
        }
    }

    /// <summary>
    /// Selects a specific preview.
    /// </summary>
    /// <param name="preview">The preview to select.</param>
    [RelayCommand]
    private void SelectPreview(DiffPreviewViewModel? preview)
    {
        if (preview == null)
            return;

        var index = DiffPreviews.IndexOf(preview);
        if (index >= 0)
        {
            SelectedIndex = index;
            SelectedPreview = preview;
            _logger?.LogDebug("Selected preview: {FileName} at index {Index}",
                preview.FileName, index);
        }
    }

    #endregion

    #region Action Commands

    /// <summary>
    /// Confirms and applies all files.
    /// </summary>
    [RelayCommand]
    private void ApplyAll()
    {
        _logger?.LogInformation("User confirmed applying all {Count} files", TotalFiles);
        _dialog.Close(true);
    }

    /// <summary>
    /// Cancels the preview and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _logger?.LogInformation("User cancelled batch preview");
        _dialog.Close(false);
    }

    #endregion

    #region Partial Methods

    partial void OnSelectedIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CanNavigatePrevious));
        OnPropertyChanged(nameof(CanNavigateNext));
    }

    #endregion
}
