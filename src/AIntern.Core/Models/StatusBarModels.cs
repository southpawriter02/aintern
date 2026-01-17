using AIntern.Core.Interfaces;

namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ STATUS BAR MODELS (v0.4.5i)                                             │
// │ Models for the status bar display and behavior.                         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Defines the sections of the status bar for positioning and ordering.
/// </summary>
public enum StatusBarSection
{
    /// <summary>Left-aligned section for primary status (model info).</summary>
    Left = 0,

    /// <summary>Center section (typically empty, provides spacing).</summary>
    Center = 1,

    /// <summary>Right section for secondary status items.</summary>
    Right = 2,

    /// <summary>Far-right section for tertiary items (temperature, etc.).</summary>
    FarRight = 3
}

/// <summary>
/// Status indicator colors for status bar items.
/// </summary>
public enum StatusColor
{
    /// <summary>Default/neutral color.</summary>
    Default,

    /// <summary>Success/positive indicator (green).</summary>
    Success,

    /// <summary>Warning indicator (yellow/orange).</summary>
    Warning,

    /// <summary>Error indicator (red).</summary>
    Error,

    /// <summary>Info indicator (blue).</summary>
    Info,

    /// <summary>Muted/secondary indicator (gray).</summary>
    Muted
}

/// <summary>
/// Type of change to a status bar item.
/// </summary>
public enum StatusBarItemChangeType
{
    /// <summary>Item was added.</summary>
    Added,

    /// <summary>Item was updated.</summary>
    Updated,

    /// <summary>Item was removed.</summary>
    Removed,

    /// <summary>Item visibility changed.</summary>
    VisibilityChanged
}

/// <summary>
/// Represents an item that can be displayed in the status bar.
/// </summary>
/// <param name="Id">Unique identifier for the status bar item.</param>
/// <param name="Section">Which section of the status bar this item belongs to.</param>
/// <param name="Order">Order within the section (lower = further left).</param>
/// <param name="IsVisible">Whether the item should currently be displayed.</param>
/// <param name="IsClickable">Whether the item responds to clicks.</param>
/// <param name="Tooltip">Tooltip text to display on hover.</param>
public record StatusBarItem(
    string Id,
    StatusBarSection Section,
    int Order,
    bool IsVisible = true,
    bool IsClickable = false,
    string? Tooltip = null)
{
    /// <summary>The content to display (text, icon, or both).</summary>
    public string? Text { get; init; }

    /// <summary>Icon resource key for the item.</summary>
    public string? IconKey { get; init; }

    /// <summary>Badge count to show (0 = no badge).</summary>
    public int BadgeCount { get; init; }

    /// <summary>Status color indicator (null = default).</summary>
    public StatusColor? Color { get; init; }

    /// <summary>Command identifier to invoke on click.</summary>
    public string? CommandId { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Creates a model status item.</summary>
    public static StatusBarItem ModelStatus(string modelName, bool isConnected) => new(
        Id: "model-status",
        Section: StatusBarSection.Left,
        Order: 0,
        IsVisible: true,
        IsClickable: true,
        Tooltip: isConnected ? $"Connected to {modelName}" : "Click to configure model")
    {
        Text = $"Model: {modelName}",
        IconKey = "CircleIcon",
        Color = isConnected ? StatusColor.Success : StatusColor.Warning,
        CommandId = "ShowModelSettings"
    };

    /// <summary>Creates a pending changes item.</summary>
    public static StatusBarItem PendingChanges(int count) => new(
        Id: "pending-changes",
        Section: StatusBarSection.Right,
        Order: 0,
        IsVisible: count > 0,
        IsClickable: true,
        Tooltip: "Click to view change history")
    {
        Text = count switch
        {
            1 => "1 pending change",
            _ => $"{count} pending changes"
        },
        IconKey = "EditIcon",
        BadgeCount = count,
        CommandId = "ShowChangeHistory"
    };

    /// <summary>Creates a saved status item.</summary>
    public static StatusBarItem SavedStatus(bool isSaved, DateTime? lastSaved = null) => new(
        Id: "saved-status",
        Section: StatusBarSection.Right,
        Order: 10,
        IsVisible: true,
        IsClickable: false,
        Tooltip: lastSaved.HasValue
            ? $"Last saved: {lastSaved.Value:g}"
            : "All changes saved")
    {
        Text = isSaved ? "Saved" : "Unsaved",
        IconKey = isSaved ? "CheckIcon" : "WarningIcon",
        Color = isSaved ? StatusColor.Success : StatusColor.Warning
    };

    /// <summary>Creates a temperature display item.</summary>
    public static StatusBarItem Temperature(double temperature) => new(
        Id: "temperature",
        Section: StatusBarSection.FarRight,
        Order: 0,
        IsVisible: true,
        IsClickable: true,
        Tooltip: $"Model temperature: {temperature:F2}\nClick to adjust")
    {
        Text = $"T: {temperature:F1}",
        CommandId = "ShowTemperatureSlider"
    };
}

/// <summary>
/// Configuration options for the status bar display and behavior.
/// </summary>
/// <param name="ShowModelStatus">Whether to show model connection status.</param>
/// <param name="ShowPendingChanges">Whether to show pending changes indicator.</param>
/// <param name="ShowSavedStatus">Whether to show saved/unsaved status.</param>
/// <param name="ShowTemperature">Whether to show temperature display.</param>
/// <param name="AnimateChanges">Whether to animate status changes.</param>
/// <param name="CompactMode">Whether to use compact display mode.</param>
public record StatusBarConfiguration(
    bool ShowModelStatus = true,
    bool ShowPendingChanges = true,
    bool ShowSavedStatus = true,
    bool ShowTemperature = true,
    bool AnimateChanges = true,
    bool CompactMode = false)
{
    /// <summary>Default configuration with all features enabled.</summary>
    public static StatusBarConfiguration Default => new();

    /// <summary>Compact configuration for smaller displays.</summary>
    public static StatusBarConfiguration Compact => new(CompactMode: true);

    /// <summary>Minimal configuration showing only essential items.</summary>
    public static StatusBarConfiguration Minimal => new(
        ShowModelStatus: true,
        ShowPendingChanges: true,
        ShowSavedStatus: false,
        ShowTemperature: false);
}

/// <summary>Event args for individual item changes.</summary>
public class StatusBarItemChangedEventArgs : EventArgs
{
    /// <summary>ID of the changed item.</summary>
    public required string ItemId { get; init; }

    /// <summary>The item before the change.</summary>
    public required StatusBarItem OldItem { get; init; }

    /// <summary>The item after the change.</summary>
    public required StatusBarItem NewItem { get; init; }

    /// <summary>Type of change that occurred.</summary>
    public required StatusBarItemChangeType ChangeType { get; init; }
}

/// <summary>Event args for collection changes.</summary>
public class StatusBarItemsChangedEventArgs : EventArgs
{
    /// <summary>Items that were added.</summary>
    public required IReadOnlyList<StatusBarItem> AddedItems { get; init; }

    /// <summary>IDs of items that were removed.</summary>
    public required IReadOnlyList<string> RemovedItemIds { get; init; }
}
