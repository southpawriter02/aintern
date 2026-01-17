using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ STATUS BAR SERVICE INTERFACE (v0.4.5i)                                  │
// │ Service for managing status bar content and interactions.               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for managing status bar content and interactions.
/// </summary>
public interface IStatusBarService
{
    /// <summary>Gets the current status bar configuration.</summary>
    StatusBarConfiguration Configuration { get; }

    /// <summary>Gets all current status bar items.</summary>
    IReadOnlyList<StatusBarItem> Items { get; }

    /// <summary>Gets items for a specific section.</summary>
    IReadOnlyList<StatusBarItem> GetItemsForSection(StatusBarSection section);

    /// <summary>Updates the configuration.</summary>
    void UpdateConfiguration(StatusBarConfiguration configuration);

    /// <summary>Registers a custom status bar item.</summary>
    void RegisterItem(StatusBarItem item);

    /// <summary>Unregisters a status bar item by ID.</summary>
    void UnregisterItem(string itemId);

    /// <summary>Updates an existing item by ID.</summary>
    void UpdateItem(string itemId, StatusBarItem newItem);

    /// <summary>Sets the visibility of an item.</summary>
    void SetItemVisibility(string itemId, bool isVisible);

    /// <summary>Executes the command associated with an item.</summary>
    Task ExecuteItemCommandAsync(string itemId, CancellationToken cancellationToken = default);

    /// <summary>Raised when any status bar item changes.</summary>
    event EventHandler<StatusBarItemChangedEventArgs>? ItemChanged;

    /// <summary>Raised when the item collection changes.</summary>
    event EventHandler<StatusBarItemsChangedEventArgs>? ItemsChanged;

    /// <summary>Raised when configuration changes.</summary>
    event EventHandler<StatusBarConfiguration>? ConfigurationChanged;

    /// <summary>Raised when a navigation command should be executed.</summary>
    event EventHandler<string>? CommandRequested;
}
