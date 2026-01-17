using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ STATUS BAR ITEM VIEW MODEL (v0.4.5i)                                    │
// │ ViewModel for an individual status bar item.                            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for an individual status bar item.
/// </summary>
public partial class StatusBarItemViewModel : ViewModelBase
{
    private readonly Func<string, Task> _executeCommand;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Unique identifier for the item.</summary>
    [ObservableProperty]
    private string _id = string.Empty;

    /// <summary>Display text.</summary>
    [ObservableProperty]
    private string? _text;

    /// <summary>Icon resource key.</summary>
    [ObservableProperty]
    private string? _iconKey;

    /// <summary>Badge count (0 = no badge).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBadge))]
    private int _badgeCount;

    /// <summary>Status color for styling.</summary>
    [ObservableProperty]
    private StatusColor _color = StatusColor.Default;

    /// <summary>Tooltip text.</summary>
    [ObservableProperty]
    private string? _tooltip;

    /// <summary>Whether the item is clickable.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    private bool _isClickable;

    /// <summary>Whether the item is visible.</summary>
    [ObservableProperty]
    private bool _isVisible = true;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Whether to show the badge.</summary>
    public bool ShowBadge => BadgeCount > 0;

    /// <summary>Color class name for styling.</summary>
    public string ColorClass => Color switch
    {
        StatusColor.Success => "success",
        StatusColor.Warning => "warning",
        StatusColor.Error => "error",
        StatusColor.Info => "info",
        StatusColor.Muted => "muted",
        _ => "default"
    };

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    public StatusBarItemViewModel(StatusBarItem item, Func<string, Task> executeCommand)
    {
        _executeCommand = executeCommand ?? throw new ArgumentNullException(nameof(executeCommand));
        UpdateFromItem(item);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Executes the item's command.</summary>
    [RelayCommand(CanExecute = nameof(IsClickable))]
    private async Task ExecuteAsync()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            await _executeCommand(Id);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Updates all properties from a StatusBarItem.</summary>
    public void UpdateFromItem(StatusBarItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Id = item.Id;
        Text = item.Text;
        IconKey = item.IconKey;
        BadgeCount = item.BadgeCount;
        Color = item.Color ?? StatusColor.Default;
        Tooltip = item.Tooltip;
        IsClickable = item.IsClickable;
        IsVisible = item.IsVisible;
    }
}
