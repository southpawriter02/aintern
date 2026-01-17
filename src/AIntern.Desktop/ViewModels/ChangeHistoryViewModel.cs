using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CHANGE HISTORY VIEW MODEL (v0.4.5h)                                      │
// │ Main ViewModel for the Change History Panel.                             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Main ViewModel for the Change History Panel.
/// </summary>
public partial class ChangeHistoryViewModel : ViewModelBase, IDisposable
{
    private readonly IChangeHistoryService _historyService;
    private readonly IUndoManager _undoManager;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<ChangeHistoryViewModel> _logger;

    private CancellationTokenSource? _loadCts;
    private System.Timers.Timer? _updateTimer;
    private bool _disposed;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Flat list of change history items.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ChangeHistoryItemViewModel> _items = new();

    /// <summary>
    /// Grouped list of change history items.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ChangeHistoryGroupViewModel> _groupedItems = new();

    /// <summary>
    /// Currently selected item.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private ChangeHistoryItemViewModel? _selectedItem;

    /// <summary>
    /// Collection of selected items.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ChangeHistoryItemViewModel> _selectedItems = new();

    /// <summary>
    /// Aggregated statistics.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    [NotifyPropertyChangedFor(nameof(HasUndoableChanges))]
    private ChangeHistoryStats _stats = ChangeHistoryStats.Empty;

    /// <summary>
    /// Current filter.
    /// </summary>
    [ObservableProperty]
    private ChangeHistoryFilter _filter = ChangeHistoryFilter.Default;

    /// <summary>
    /// Whether data is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Whether grouping is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isGrouped;

    /// <summary>
    /// Current grouping mode.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGrouped))]
    private HistoryGroupMode _groupBy = HistoryGroupMode.None;

    /// <summary>
    /// Search text.
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Whether the filter panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showFilters;

    /// <summary>
    /// Error message if any.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Only show undoable changes.
    /// </summary>
    [ObservableProperty]
    private bool _onlyShowUndoable;

    /// <summary>
    /// Current sort order.
    /// </summary>
    [ObservableProperty]
    private ChangeHistorySortOrder _sortOrder = ChangeHistorySortOrder.NewestFirst;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    public bool HasChanges => Stats.TotalChanges > 0;
    public bool HasUndoableChanges => Stats.UndoableCount > 0;
    public int SelectedCount => SelectedItems.Count;
    public bool HasSelection => SelectedCount > 0;
    public bool CanUndoSelected => SelectedItems.Any(i => i.CanUndo);

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    public event EventHandler<FileChangeRecord>? ShowDiffRequested;
    public event EventHandler<string>? NavigateToFileRequested;
    public event EventHandler? HistoryCleared;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    public ChangeHistoryViewModel(
        IChangeHistoryService historyService,
        IUndoManager undoManager,
        IClipboardService clipboardService,
        ISettingsService settingsService,
        IDispatcher dispatcher,
        ILogger<ChangeHistoryViewModel> logger)
    {
        _historyService = historyService;
        _undoManager = undoManager;
        _clipboardService = clipboardService;
        _settingsService = settingsService;
        _dispatcher = dispatcher;
        _logger = logger;

        // Subscribe to service events
        _historyService.HistoryRecorded += OnHistoryRecorded;
        _historyService.HistoryCleared += OnHistoryCleared;
        _undoManager.TimeRemainingChanged += OnTimeRemainingChanged;
        _undoManager.UndoCompleted += OnUndoCompleted;

        // Track selection changes
        SelectedItems.CollectionChanged += OnSelectedItemsChanged;

        // Start update timer for relative times
        StartUpdateTimer();

        _logger.LogDebug("[ChangeHistoryViewModel] Initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Load change history.
    /// </summary>
    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var filter = BuildFilter();
            var records = await _historyService.GetHistoryAsync(filter, _loadCts.Token);
            Stats = await _historyService.GetStatsAsync(filter, _loadCts.Token);

            await _dispatcher.InvokeAsync(() =>
            {
                Items.Clear();
                foreach (var record in records)
                {
                    Items.Add(CreateItemViewModel(record));
                }

                if (GroupBy != HistoryGroupMode.None)
                {
                    RefreshGroupedView();
                }
            });

            _logger.LogDebug("[ChangeHistoryViewModel] Loaded {Count} history items", records.Count);
        }
        catch (OperationCanceledException)
        {
            // Ignored - new load started
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChangeHistoryViewModel] Failed to load history");
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refresh history.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadHistoryAsync();
    }

    /// <summary>
    /// Undo selected changes.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUndoSelected))]
    private async Task UndoSelectedAsync()
    {
        var toUndo = SelectedItems.Where(i => i.CanUndo).ToList();
        _logger.LogInformation("[ChangeHistoryViewModel] Undoing {Count} selected changes", toUndo.Count);

        foreach (var item in toUndo)
        {
            await item.UndoCommand.ExecuteAsync(null);
        }

        await RefreshStatsAsync();
    }

    /// <summary>
    /// Undo all undoable changes.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasUndoableChanges))]
    private async Task UndoAllAsync()
    {
        var undoable = Items.Where(i => i.CanUndo).ToList();
        _logger.LogInformation("[ChangeHistoryViewModel] Undoing all {Count} changes", undoable.Count);

        foreach (var item in undoable)
        {
            await item.UndoCommand.ExecuteAsync(null);
        }

        await RefreshStatsAsync();
    }

    /// <summary>
    /// Clear all history.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasChanges))]
    private async Task ClearHistoryAsync()
    {
        _logger.LogInformation("[ChangeHistoryViewModel] Clearing all history");

        await _historyService.ClearHistoryAsync();

        Items.Clear();
        GroupedItems.Clear();
        Stats = ChangeHistoryStats.Empty;

        HistoryCleared?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Export history.
    /// </summary>
    [RelayCommand]
    private async Task ExportAsync(ChangeHistoryExportFormat format)
    {
        _logger.LogInformation("[ChangeHistoryViewModel] Exporting history as {Format}", format);

        var filter = BuildFilter();
        var stream = await _historyService.ExportAsync(format, filter);

        // Stream is ready for save dialog - caller handles file save
    }

    /// <summary>
    /// Toggle grouping mode.
    /// </summary>
    [RelayCommand]
    private void ToggleGrouping()
    {
        GroupBy = GroupBy switch
        {
            HistoryGroupMode.None => HistoryGroupMode.ByFile,
            HistoryGroupMode.ByFile => HistoryGroupMode.ByTimePeriod,
            HistoryGroupMode.ByTimePeriod => HistoryGroupMode.ByChangeType,
            HistoryGroupMode.ByChangeType => HistoryGroupMode.ByDirectory,
            HistoryGroupMode.ByDirectory => HistoryGroupMode.None,
            _ => HistoryGroupMode.None
        };

        IsGrouped = GroupBy != HistoryGroupMode.None;
        RefreshGroupedView();
    }

    /// <summary>
    /// Set specific grouping mode.
    /// </summary>
    [RelayCommand]
    private void SetGroupBy(HistoryGroupMode mode)
    {
        GroupBy = mode;
        IsGrouped = mode != HistoryGroupMode.None;
        RefreshGroupedView();
    }

    /// <summary>
    /// Toggle filter panel visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleFilters()
    {
        ShowFilters = !ShowFilters;
    }

    /// <summary>
    /// Apply current filter.
    /// </summary>
    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        await LoadHistoryAsync();
    }

    /// <summary>
    /// Clear filter.
    /// </summary>
    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        SearchText = string.Empty;
        OnlyShowUndoable = false;
        SortOrder = ChangeHistorySortOrder.NewestFirst;
        Filter = ChangeHistoryFilter.Default;
        await LoadHistoryAsync();
    }

    /// <summary>
    /// Select all items.
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        SelectedItems.Clear();
        foreach (var item in Items)
        {
            item.IsSelected = true;
            SelectedItems.Add(item);
        }
    }

    /// <summary>
    /// Clear selection.
    /// </summary>
    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }
        SelectedItems.Clear();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event Handlers
    // ═══════════════════════════════════════════════════════════════════════

    private void OnHistoryRecorded(object? sender, FileChangeRecord record)
    {
        _dispatcher.InvokeAsync(() =>
        {
            var vm = CreateItemViewModel(record);
            Items.Insert(0, vm);

            // Update stats
            Stats = Stats with
            {
                TotalChanges = Stats.TotalChanges + 1,
                UndoableCount = Stats.UndoableCount + 1
            };

            // Trim to max items
            TrimToMaxItems();

            if (IsGrouped)
            {
                RefreshGroupedView();
            }
        });
    }

    private void OnHistoryCleared(object? sender, int count)
    {
        _dispatcher.InvokeAsync(async () =>
        {
            await LoadHistoryAsync();
        });
    }

    private void OnTimeRemainingChanged(object? sender, TimeRemainingChangedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            foreach (var item in Items)
            {
                item.UpdateTimeRemaining();
            }
        });
    }

    private void OnUndoCompleted(object? sender, UndoCompletedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            var item = Items.FirstOrDefault(i => i.Record.Id == e.ChangeId);
            item?.MarkAsUndone();

            Stats = Stats with
            {
                UndoableCount = Math.Max(0, Stats.UndoableCount - 1),
                UndoneCount = Stats.UndoneCount + 1
            };
        });
    }

    private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanUndoSelected));
        UndoSelectedCommand.NotifyCanExecuteChanged();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════════════

    private ChangeHistoryFilter BuildFilter()
    {
        return Filter with
        {
            SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
            OnlyUndoable = OnlyShowUndoable,
            SortOrder = SortOrder
        };
    }

    private ChangeHistoryItemViewModel CreateItemViewModel(FileChangeRecord record)
    {
        var vm = new ChangeHistoryItemViewModel(record, _undoManager, _clipboardService);
        vm.ViewDiffRequested += (_, r) => ShowDiffRequested?.Invoke(this, r);
        vm.NavigateToFileRequested += (_, path) => NavigateToFileRequested?.Invoke(this, path);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ChangeHistoryItemViewModel.IsSelected))
            {
                if (vm.IsSelected && !SelectedItems.Contains(vm))
                {
                    SelectedItems.Add(vm);
                }
                else if (!vm.IsSelected && SelectedItems.Contains(vm))
                {
                    SelectedItems.Remove(vm);
                }
            }
        };
        return vm;
    }

    private void RefreshGroupedView()
    {
        GroupedItems.Clear();

        if (GroupBy == HistoryGroupMode.None) return;

        var grouped = GroupBy switch
        {
            HistoryGroupMode.ByFile => GroupByFile(),
            HistoryGroupMode.ByTimePeriod => GroupByTimePeriod(),
            HistoryGroupMode.ByChangeType => GroupByChangeType(),
            HistoryGroupMode.ByDirectory => GroupByDirectory(),
            _ => Enumerable.Empty<ChangeHistoryGroupViewModel>()
        };

        foreach (var group in grouped)
        {
            GroupedItems.Add(group);
        }
    }

    private IEnumerable<ChangeHistoryGroupViewModel> GroupByFile()
    {
        return Items
            .GroupBy(i => i.Record.FilePath)
            .Select(g => new ChangeHistoryGroupViewModel(
                key: g.Key,
                label: Path.GetFileName(g.Key),
                icon: "FileIcon",
                items: new ObservableCollection<ChangeHistoryItemViewModel>(g)))
            .OrderByDescending(g => g.Items.Max(i => i.Record.ChangedAt));
    }

    private IEnumerable<ChangeHistoryGroupViewModel> GroupByTimePeriod()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var groups = new[]
        {
            ("Today", Items.Where(i => i.Record.ChangedAt.Date == today)),
            ("Yesterday", Items.Where(i => i.Record.ChangedAt.Date == today.AddDays(-1))),
            ("This Week", Items.Where(i =>
                i.Record.ChangedAt.Date < today.AddDays(-1) &&
                i.Record.ChangedAt.Date >= today.AddDays(-(int)today.DayOfWeek))),
            ("Older", Items.Where(i =>
                i.Record.ChangedAt.Date < today.AddDays(-(int)today.DayOfWeek)))
        };

        return groups
            .Where(g => g.Item2.Any())
            .Select(g => new ChangeHistoryGroupViewModel(
                key: g.Item1,
                label: g.Item1,
                icon: "CalendarIcon",
                items: new ObservableCollection<ChangeHistoryItemViewModel>(g.Item2)));
    }

    private IEnumerable<ChangeHistoryGroupViewModel> GroupByChangeType()
    {
        return Items
            .GroupBy(i => i.Record.ChangeType)
            .Select(g => new ChangeHistoryGroupViewModel(
                key: g.Key.ToString(),
                label: g.Key switch
                {
                    FileChangeType.Created => "Created Files",
                    FileChangeType.Modified => "Modified Files",
                    FileChangeType.Deleted => "Deleted Files",
                    FileChangeType.Renamed => "Renamed Files",
                    _ => "Other"
                },
                icon: g.Key switch
                {
                    FileChangeType.Created => "PlusCircleIcon",
                    FileChangeType.Modified => "EditIcon",
                    FileChangeType.Deleted => "TrashIcon",
                    FileChangeType.Renamed => "ArrowRightIcon",
                    _ => "FileIcon"
                },
                items: new ObservableCollection<ChangeHistoryItemViewModel>(g)));
    }

    private IEnumerable<ChangeHistoryGroupViewModel> GroupByDirectory()
    {
        return Items
            .GroupBy(i => Path.GetDirectoryName(i.Record.RelativePath) ?? "/")
            .Select(g => new ChangeHistoryGroupViewModel(
                key: g.Key,
                label: string.IsNullOrEmpty(g.Key) ? "Root" : g.Key,
                icon: "FolderIcon",
                items: new ObservableCollection<ChangeHistoryItemViewModel>(g)));
    }

    private async Task RefreshStatsAsync()
    {
        var filter = BuildFilter();
        Stats = await _historyService.GetStatsAsync(filter);
    }

    private void TrimToMaxItems()
    {
        var maxItems = _settingsService.CurrentSettings.MaxChangeHistoryItems;
        while (Items.Count > maxItems)
        {
            Items.RemoveAt(Items.Count - 1);
        }
    }

    private void StartUpdateTimer()
    {
        _updateTimer = new System.Timers.Timer(60000); // Update every minute
        _updateTimer.Elapsed += (_, _) =>
        {
            _dispatcher.InvokeAsync(() =>
            {
                foreach (var item in Items)
                {
                    // Use public method instead of protected OnPropertyChanged
                    item.UpdateTimeRemaining();
                }
            });
        };
        _updateTimer.Start();
    }

    partial void OnGroupByChanged(HistoryGroupMode value)
    {
        IsGrouped = value != HistoryGroupMode.None;
        RefreshGroupedView();
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search - will trigger load after short delay
        _ = Task.Delay(300).ContinueWith(async _ =>
        {
            await _dispatcher.InvokeAsync(async () => await LoadHistoryAsync());
        });
    }

    partial void OnOnlyShowUndoableChanged(bool value)
    {
        _ = LoadHistoryAsync();
    }

    partial void OnSortOrderChanged(ChangeHistorySortOrder value)
    {
        _ = LoadHistoryAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IDisposable
    // ═══════════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        _loadCts?.Cancel();
        _loadCts?.Dispose();

        _historyService.HistoryRecorded -= OnHistoryRecorded;
        _historyService.HistoryCleared -= OnHistoryCleared;
        _undoManager.TimeRemainingChanged -= OnTimeRemainingChanged;
        _undoManager.UndoCompleted -= OnUndoCompleted;

        _logger.LogDebug("[ChangeHistoryViewModel] Disposed");
    }
}
