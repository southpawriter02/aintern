using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ STATUS BAR SERVICE (v0.4.5i)                                            │
// │ Manages status bar items by aggregating status from multiple sources.   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Implementation of status bar management service.
/// </summary>
/// <remarks>
/// <para>
/// Aggregates status from:
/// <list type="bullet">
/// <item><see cref="IUndoManager"/> - pending changes count</item>
/// <item><see cref="ILlmService"/> - model connection status</item>
/// <item><see cref="ISettingsService"/> - temperature display</item>
/// </list>
/// </para>
/// <para>Added in v0.4.5i.</para>
/// </remarks>
public sealed class StatusBarService : IStatusBarService, IDisposable
{
    private readonly IUndoManager _undoManager;
    private readonly ISettingsService _settingsService;
    private readonly ILlmService _llmService;
    private readonly ILogger<StatusBarService>? _logger;
    private readonly ConcurrentDictionary<string, StatusBarItem> _items = new();

    private StatusBarConfiguration _configuration = StatusBarConfiguration.Default;
    private bool _disposed;

    // ═══════════════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public StatusBarConfiguration Configuration => _configuration;

    /// <inheritdoc />
    public IReadOnlyList<StatusBarItem> Items => _items.Values
        .OrderBy(i => i.Section)
        .ThenBy(i => i.Order)
        .ToList();

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public event EventHandler<StatusBarItemChangedEventArgs>? ItemChanged;

    /// <inheritdoc />
    public event EventHandler<StatusBarItemsChangedEventArgs>? ItemsChanged;

    /// <inheritdoc />
    public event EventHandler<StatusBarConfiguration>? ConfigurationChanged;

    /// <inheritdoc />
    public event EventHandler<string>? CommandRequested;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    public StatusBarService(
        IUndoManager undoManager,
        ISettingsService settingsService,
        ILlmService llmService,
        ILogger<StatusBarService>? logger = null)
    {
        _undoManager = undoManager ?? throw new ArgumentNullException(nameof(undoManager));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _logger = logger;

        _logger?.LogDebug("[StatusBarService] Initializing with service dependencies");

        // Initialize default items
        InitializeDefaultItems();

        // Subscribe to events
        _undoManager.UndoAvailable += OnUndoAvailable;
        _undoManager.UndoCompleted += OnUndoCompleted;
        _undoManager.UndoExpired += OnUndoExpired;
        _llmService.ModelStateChanged += OnModelStateChanged;
        _settingsService.SettingsChanged += OnSettingsChanged;

        _logger?.LogDebug("[StatusBarService] Event subscriptions established");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Initialization
    // ═══════════════════════════════════════════════════════════════════════

    private void InitializeDefaultItems()
    {
        var settings = _settingsService.CurrentSettings;
        var undoableCount = _undoManager.PendingUndoCount;
        var modelName = _llmService.CurrentModelName ?? "No model";
        var isConnected = _llmService.IsModelLoaded;

        var defaultItems = new[]
        {
            StatusBarItem.ModelStatus(modelName, isConnected),
            StatusBarItem.PendingChanges(undoableCount),
            StatusBarItem.SavedStatus(true), // Default to saved
            StatusBarItem.Temperature(settings.Temperature)
        };

        foreach (var item in defaultItems)
        {
            _items.TryAdd(item.Id, item);
        }

        _logger?.LogDebug("[StatusBarService] Initialized {Count} default items", defaultItems.Length);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public IReadOnlyList<StatusBarItem> GetItemsForSection(StatusBarSection section)
    {
        return _items.Values
            .Where(i => i.Section == section && i.IsVisible)
            .OrderBy(i => i.Order)
            .ToList();
    }

    /// <inheritdoc />
    public void UpdateConfiguration(StatusBarConfiguration configuration)
    {
        var old = _configuration;
        _configuration = configuration;

        _logger?.LogDebug("[StatusBarService] Configuration updated, CompactMode: {Compact}", configuration.CompactMode);

        // Update item visibility based on configuration
        SetItemVisibility("model-status", configuration.ShowModelStatus);
        SetItemVisibility("pending-changes", configuration.ShowPendingChanges);
        SetItemVisibility("saved-status", configuration.ShowSavedStatus);
        SetItemVisibility("temperature", configuration.ShowTemperature);

        ConfigurationChanged?.Invoke(this, configuration);
    }

    /// <inheritdoc />
    public void RegisterItem(StatusBarItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_items.TryAdd(item.Id, item))
        {
            _logger?.LogDebug("[StatusBarService] Registered item: {Id}", item.Id);

            ItemsChanged?.Invoke(this, new StatusBarItemsChangedEventArgs
            {
                AddedItems = new[] { item },
                RemovedItemIds = Array.Empty<string>()
            });

            ItemChanged?.Invoke(this, new StatusBarItemChangedEventArgs
            {
                ItemId = item.Id,
                OldItem = item,
                NewItem = item,
                ChangeType = StatusBarItemChangeType.Added
            });
        }
    }

    /// <inheritdoc />
    public void UnregisterItem(string itemId)
    {
        if (_items.TryRemove(itemId, out var removed))
        {
            _logger?.LogDebug("[StatusBarService] Unregistered item: {Id}", itemId);

            ItemsChanged?.Invoke(this, new StatusBarItemsChangedEventArgs
            {
                AddedItems = Array.Empty<StatusBarItem>(),
                RemovedItemIds = new[] { itemId }
            });

            ItemChanged?.Invoke(this, new StatusBarItemChangedEventArgs
            {
                ItemId = itemId,
                OldItem = removed,
                NewItem = removed,
                ChangeType = StatusBarItemChangeType.Removed
            });
        }
    }

    /// <inheritdoc />
    public void UpdateItem(string itemId, StatusBarItem newItem)
    {
        if (_items.TryGetValue(itemId, out var oldItem))
        {
            if (_items.TryUpdate(itemId, newItem, oldItem))
            {
                _logger?.LogDebug("[StatusBarService] Updated item: {Id}", itemId);

                ItemChanged?.Invoke(this, new StatusBarItemChangedEventArgs
                {
                    ItemId = itemId,
                    OldItem = oldItem,
                    NewItem = newItem,
                    ChangeType = StatusBarItemChangeType.Updated
                });
            }
        }
    }

    /// <inheritdoc />
    public void SetItemVisibility(string itemId, bool isVisible)
    {
        if (_items.TryGetValue(itemId, out var oldItem) && oldItem.IsVisible != isVisible)
        {
            var newItem = oldItem with { IsVisible = isVisible };

            if (_items.TryUpdate(itemId, newItem, oldItem))
            {
                _logger?.LogDebug("[StatusBarService] Item {Id} visibility: {Visible}", itemId, isVisible);

                ItemChanged?.Invoke(this, new StatusBarItemChangedEventArgs
                {
                    ItemId = itemId,
                    OldItem = oldItem,
                    NewItem = newItem,
                    ChangeType = StatusBarItemChangeType.VisibilityChanged
                });
            }
        }
    }

    /// <inheritdoc />
    public Task ExecuteItemCommandAsync(string itemId, CancellationToken cancellationToken = default)
    {
        if (!_items.TryGetValue(itemId, out var item) || string.IsNullOrEmpty(item.CommandId))
            return Task.CompletedTask;

        _logger?.LogDebug("[StatusBarService] Executing command: {Command} for item: {Id}",
            item.CommandId, itemId);

        // Raise the command request event for the ViewModel to handle
        CommandRequested?.Invoke(this, item.CommandId);

        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event Handlers
    // ═══════════════════════════════════════════════════════════════════════

    private void OnUndoAvailable(object? sender, UndoAvailableEventArgs e)
    {
        UpdatePendingChangesCount();
    }

    private void OnUndoCompleted(object? sender, UndoCompletedEventArgs e)
    {
        UpdatePendingChangesCount();
    }

    private void OnUndoExpired(object? sender, UndoExpiredEventArgs e)
    {
        UpdatePendingChangesCount();
    }

    private void UpdatePendingChangesCount()
    {
        var count = _undoManager.PendingUndoCount;
        var newItem = StatusBarItem.PendingChanges(count);

        if (_items.TryGetValue("pending-changes", out var oldItem))
        {
            // Preserve visibility from configuration
            newItem = newItem with { IsVisible = count > 0 && _configuration.ShowPendingChanges };

            _items.TryUpdate("pending-changes", newItem, oldItem);

            _logger?.LogDebug("[StatusBarService] Pending changes count: {Count}", count);

            ItemChanged?.Invoke(this, new StatusBarItemChangedEventArgs
            {
                ItemId = "pending-changes",
                OldItem = oldItem,
                NewItem = newItem,
                ChangeType = StatusBarItemChangeType.Updated
            });
        }
    }

    private void OnModelStateChanged(object? sender, ModelStateChangedEventArgs e)
    {
        var modelName = e.ModelName ?? "No model";
        var isConnected = e.IsLoaded;
        var newItem = StatusBarItem.ModelStatus(modelName, isConnected);

        if (_items.TryGetValue("model-status", out var oldItem))
        {
            _items.TryUpdate("model-status", newItem, oldItem);

            _logger?.LogDebug("[StatusBarService] Model status: {Name}, Connected: {Connected}",
                modelName, isConnected);

            ItemChanged?.Invoke(this, new StatusBarItemChangedEventArgs
            {
                ItemId = "model-status",
                OldItem = oldItem,
                NewItem = newItem,
                ChangeType = StatusBarItemChangeType.Updated
            });
        }
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        var newItem = StatusBarItem.Temperature(e.Settings.Temperature);

        if (_items.TryGetValue("temperature", out var oldItem))
        {
            _items.TryUpdate("temperature", newItem, oldItem);

            _logger?.LogDebug("[StatusBarService] Temperature: {Temp}", e.Settings.Temperature);

            ItemChanged?.Invoke(this, new StatusBarItemChangedEventArgs
            {
                ItemId = "temperature",
                OldItem = oldItem,
                NewItem = newItem,
                ChangeType = StatusBarItemChangeType.Updated
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Update Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the saved status indicator.
    /// </summary>
    public void UpdateSavedStatus(bool isSaved, DateTime? lastSaved = null)
    {
        var newItem = StatusBarItem.SavedStatus(isSaved, lastSaved);

        if (_items.TryGetValue("saved-status", out var oldItem))
        {
            _items.TryUpdate("saved-status", newItem, oldItem);

            _logger?.LogDebug("[StatusBarService] Saved status: {Saved}", isSaved);

            ItemChanged?.Invoke(this, new StatusBarItemChangedEventArgs
            {
                ItemId = "saved-status",
                OldItem = oldItem,
                NewItem = newItem,
                ChangeType = StatusBarItemChangeType.Updated
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose
    // ═══════════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;

        _undoManager.UndoAvailable -= OnUndoAvailable;
        _undoManager.UndoCompleted -= OnUndoCompleted;
        _undoManager.UndoExpired -= OnUndoExpired;
        _llmService.ModelStateChanged -= OnModelStateChanged;
        _settingsService.SettingsChanged -= OnSettingsChanged;

        _disposed = true;
        _logger?.LogDebug("[StatusBarService] Disposed");
    }
}
