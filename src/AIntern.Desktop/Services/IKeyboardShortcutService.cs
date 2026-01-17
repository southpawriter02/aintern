using AIntern.Desktop.Models;
using Avalonia.Input;

namespace AIntern.Desktop.Services;

/// <summary>
/// Service for managing and dispatching keyboard shortcuts (v0.4.5f).
/// </summary>
public interface IKeyboardShortcutService
{
    /// <summary>
    /// Raised when a shortcut is triggered.
    /// </summary>
    event EventHandler<ShortcutTriggeredEventArgs>? ShortcutTriggered;

    /// <summary>
    /// Raised when shortcut bindings change.
    /// </summary>
    event EventHandler? ShortcutsChanged;

    /// <summary>
    /// Event raised when a command should be executed (v0.3.5g compatibility).
    /// </summary>
    event EventHandler<string>? CommandRequested;

    #region Registration

    /// <summary>
    /// Registers a shortcut binding.
    /// </summary>
    void Register(
        KeyboardShortcut shortcut,
        string actionId,
        string description,
        ShortcutContext context,
        ShortcutCategory category = ShortcutCategory.General);

    /// <summary>
    /// Registers a shortcut (v0.3.5g compatibility).
    /// </summary>
    void Register(Key key, KeyModifiers modifiers, string commandId, string description);

    /// <summary>
    /// Registers an action handler.
    /// </summary>
    void RegisterAction(ShortcutActionRegistration registration);

    /// <summary>
    /// Unregisters a shortcut binding.
    /// </summary>
    void Unregister(KeyboardShortcut shortcut);

    /// <summary>
    /// Unregisters all shortcuts for an action.
    /// </summary>
    void UnregisterAction(string actionId);

    #endregion

    #region Customization

    /// <summary>
    /// Rebinds an action to a new shortcut.
    /// </summary>
    /// <returns>True if successful, false if conflict exists.</returns>
    bool Rebind(string actionId, KeyboardShortcut newShortcut);

    /// <summary>
    /// Resets an action to its default shortcut.
    /// </summary>
    void ResetToDefault(string actionId);

    /// <summary>
    /// Resets all shortcuts to defaults.
    /// </summary>
    void ResetAllToDefaults();

    /// <summary>
    /// Enables or disables a shortcut.
    /// </summary>
    void SetEnabled(string actionId, bool enabled);

    #endregion

    #region Query

    /// <summary>
    /// Gets the handler for a specific shortcut.
    /// </summary>
    ShortcutHandler? GetHandler(KeyboardShortcut shortcut);

    /// <summary>
    /// Gets the handler for an action ID.
    /// </summary>
    ShortcutHandler? GetHandlerByActionId(string actionId);

    /// <summary>
    /// Gets all handlers for a context.
    /// </summary>
    IEnumerable<ShortcutHandler> GetHandlersForContext(ShortcutContext context);

    /// <summary>
    /// Gets all handlers in a category.
    /// </summary>
    IEnumerable<ShortcutHandler> GetHandlersByCategory(ShortcutCategory category);

    /// <summary>
    /// Gets all registered handlers.
    /// </summary>
    IEnumerable<ShortcutHandler> GetAllHandlers();

    /// <summary>
    /// Gets all shortcuts (v0.3.5g compatibility).
    /// </summary>
    IReadOnlyList<ShortcutInfo> GetAllShortcuts();

    /// <summary>
    /// Gets shortcuts that conflict with the given shortcut.
    /// </summary>
    IEnumerable<ShortcutHandler> GetConflicts(KeyboardShortcut shortcut, ShortcutContext context);

    /// <summary>
    /// Gets the shortcut for an action, if any.
    /// </summary>
    KeyboardShortcut? GetShortcutForAction(string actionId);

    #endregion

    #region Dispatch

    /// <summary>
    /// Attempts to handle a key event (v0.4.5f).
    /// </summary>
    /// <returns>True if a shortcut was triggered.</returns>
    Task<bool> TryHandleAsync(Key key, KeyModifiers modifiers, ShortcutContext context);

    /// <summary>
    /// Synchronous version for compatibility.
    /// </summary>
    bool TryHandle(Key key, KeyModifiers modifiers, ShortcutContext context, out string? actionId);

    /// <summary>
    /// Handles a key press (v0.3.5g compatibility).
    /// </summary>
    bool HandleKeyPress(KeyEventArgs e);

    #endregion

    #region Persistence

    /// <summary>
    /// Saves customized shortcuts to settings.
    /// </summary>
    Task SaveCustomizationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads customized shortcuts from settings.
    /// </summary>
    Task LoadCustomizationsAsync(CancellationToken cancellationToken = default);

    #endregion
}
