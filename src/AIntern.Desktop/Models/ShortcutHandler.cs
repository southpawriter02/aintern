namespace AIntern.Desktop.Models;

/// <summary>
/// Associates a keyboard shortcut with an action (v0.4.5f).
/// </summary>
public sealed class ShortcutHandler
{
    /// <summary>
    /// The keyboard shortcut that triggers this action.
    /// </summary>
    public KeyboardShortcut Shortcut { get; set; }

    /// <summary>
    /// Unique identifier for the action (e.g., "ApplyAllChanges").
    /// </summary>
    public required string ActionId { get; init; }

    /// <summary>
    /// Human-readable description of the action.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Context where this shortcut is active.
    /// </summary>
    public ShortcutContext Context { get; init; } = ShortcutContext.Global;

    /// <summary>
    /// Category for grouping in settings UI.
    /// </summary>
    public ShortcutCategory Category { get; init; } = ShortcutCategory.General;

    /// <summary>
    /// Whether this shortcut is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether this shortcut has been customized from the default.
    /// </summary>
    public bool IsCustom { get; set; }

    /// <summary>
    /// The default shortcut (before customization).
    /// </summary>
    public KeyboardShortcut? DefaultShortcut { get; init; }

    /// <summary>
    /// Gets the display string for the shortcut.
    /// </summary>
    public string DisplayString => Shortcut.ToDisplayString();

    /// <summary>
    /// Creates a clone with a new shortcut.
    /// </summary>
    public ShortcutHandler WithShortcut(KeyboardShortcut newShortcut) => new()
    {
        Shortcut = newShortcut,
        ActionId = ActionId,
        Description = Description,
        Context = Context,
        Category = Category,
        IsEnabled = IsEnabled,
        IsCustom = true,
        DefaultShortcut = DefaultShortcut ?? Shortcut
    };
}

/// <summary>
/// Delegate for shortcut action handlers.
/// </summary>
/// <param name="context">The context where the shortcut was triggered.</param>
/// <returns>True if the action was handled, false to allow bubbling.</returns>
public delegate Task<bool> ShortcutActionHandler(ShortcutContext context);

/// <summary>
/// Registration for a shortcut action (v0.4.5f).
/// </summary>
public sealed class ShortcutActionRegistration
{
    /// <summary>
    /// Unique action identifier.
    /// </summary>
    public required string ActionId { get; init; }

    /// <summary>
    /// The handler to execute.
    /// </summary>
    public required ShortcutActionHandler Handler { get; init; }

    /// <summary>
    /// Optional predicate to check if action can execute.
    /// </summary>
    public Func<bool>? CanExecute { get; init; }

    /// <summary>
    /// Category for settings display.
    /// </summary>
    public ShortcutCategory Category { get; init; } = ShortcutCategory.General;
}

/// <summary>
/// Event args for shortcut triggered events (v0.4.5f).
/// </summary>
public sealed class ShortcutTriggeredEventArgs : EventArgs
{
    /// <summary>
    /// The shortcut that was triggered.
    /// </summary>
    public required KeyboardShortcut Shortcut { get; init; }

    /// <summary>
    /// The action ID that was triggered.
    /// </summary>
    public required string ActionId { get; init; }

    /// <summary>
    /// The context where the shortcut was triggered.
    /// </summary>
    public required ShortcutContext Context { get; init; }

    /// <summary>
    /// Whether the shortcut was handled.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Persisted shortcut customization (v0.4.5f).
/// </summary>
public sealed class ShortcutCustomization
{
    /// <summary>
    /// The action ID being customized.
    /// </summary>
    public required string ActionId { get; init; }

    /// <summary>
    /// The shortcut string (e.g., "Ctrl+A").
    /// </summary>
    public required string ShortcutString { get; init; }

    /// <summary>
    /// Whether the shortcut is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}
