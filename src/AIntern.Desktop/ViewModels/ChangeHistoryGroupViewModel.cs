using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CHANGE HISTORY GROUP VIEW MODEL (v0.4.5h)                                │
// │ ViewModel for grouped change history items.                              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for grouped change history items.
/// </summary>
public partial class ChangeHistoryGroupViewModel : ViewModelBase
{
    // ═══════════════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Unique key for the group.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Display label for the group.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Icon identifier for the group.
    /// </summary>
    public string Icon { get; }

    /// <summary>
    /// Items in this group.
    /// </summary>
    public ObservableCollection<ChangeHistoryItemViewModel> Items { get; }

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether the group is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>
    /// Whether all items in the group are selected.
    /// </summary>
    [ObservableProperty]
    private bool _isAllSelected;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Number of items in this group.
    /// </summary>
    public int ItemCount => Items.Count;

    /// <summary>
    /// Number of undoable items in this group.
    /// </summary>
    public int UndoableCount => Items.Count(i => i.CanUndo);

    /// <summary>
    /// Whether any items are undoable.
    /// </summary>
    public bool HasUndoable => UndoableCount > 0;

    /// <summary>
    /// Summary of line changes.
    /// </summary>
    public string LinesSummary
    {
        get
        {
            var added = Items.Sum(i => i.Record.LinesAdded);
            var removed = Items.Sum(i => i.Record.LinesRemoved);
            return $"+{added} -{removed}";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    public ChangeHistoryGroupViewModel(
        string key,
        string label,
        string icon,
        ObservableCollection<ChangeHistoryItemViewModel> items)
    {
        Key = key;
        Label = label;
        Icon = icon;
        Items = items;

        // Track selection changes
        foreach (var item in items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Toggle expanded state.
    /// </summary>
    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// Select all items in the group.
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        var newState = !IsAllSelected;
        foreach (var item in Items)
        {
            item.IsSelected = newState;
        }
        IsAllSelected = newState;
    }

    /// <summary>
    /// Undo all undoable items in the group.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasUndoable))]
    private async Task UndoAllInGroupAsync()
    {
        var undoable = Items.Where(i => i.CanUndo).ToList();
        foreach (var item in undoable)
        {
            await item.UndoCommand.ExecuteAsync(null);
        }

        RefreshState();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Refresh computed properties.
    /// </summary>
    public void RefreshState()
    {
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(UndoableCount));
        OnPropertyChanged(nameof(HasUndoable));
        OnPropertyChanged(nameof(LinesSummary));
        UndoAllInGroupCommand.NotifyCanExecuteChanged();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════════════

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChangeHistoryItemViewModel.IsSelected))
        {
            IsAllSelected = Items.All(i => i.IsSelected);
        }
        else if (e.PropertyName == nameof(ChangeHistoryItemViewModel.CanUndo) ||
                 e.PropertyName == nameof(ChangeHistoryItemViewModel.WasUndone))
        {
            RefreshState();
        }
    }
}
