using AIntern.Core.Interfaces;
using AIntern.Desktop.Models;
using Avalonia.Input;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AIntern.Desktop.Services;

/// <summary>
/// Service for managing keyboard shortcuts and dispatching actions (v0.4.5f).
/// Enhanced with context-aware dispatch and customization support.
/// Maintains backward compatibility with v0.3.5g API.
/// </summary>
public sealed class KeyboardShortcutService : IKeyboardShortcutService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<KeyboardShortcutService>? _logger;

    private readonly ConcurrentDictionary<string, ShortcutHandler> _actionHandlers = new();
    private readonly ConcurrentDictionary<string, ShortcutActionRegistration> _actionRegistrations = new();
    private readonly Dictionary<(Key, KeyModifiers), ShortcutInfo> _legacyShortcuts = new();

    private readonly List<ShortcutHandler> _defaultShortcuts = new();

    /// <inheritdoc />
    public event EventHandler<ShortcutTriggeredEventArgs>? ShortcutTriggered;

    /// <inheritdoc />
    public event EventHandler? ShortcutsChanged;

    /// <inheritdoc />
    public event EventHandler<string>? CommandRequested;

    /// <summary>
    /// Initializes the keyboard shortcut service with default shortcuts.
    /// </summary>
    public KeyboardShortcutService(
        ISettingsService settingsService,
        ILogger<KeyboardShortcutService>? logger = null)
    {
        _settingsService = settingsService;
        _logger = logger;

        _logger?.LogDebug("[INIT] KeyboardShortcutService v0.4.5f initializing");
        RegisterDefaultShortcuts();
        _logger?.LogInformation(
            "[INIT] KeyboardShortcutService registered {Count} shortcuts",
            _actionHandlers.Count);
    }

    #region Default Shortcuts

    private void RegisterDefaultShortcuts()
    {
        // ═══════════════════════════════════════════════════════════
        // Global Shortcuts
        // ═══════════════════════════════════════════════════════════

        RegisterDefault(
            KeyboardShortcut.CtrlShift(Key.A),
            "ApplyAllChanges",
            "Apply all pending changes",
            ShortcutContext.Global,
            ShortcutCategory.CodeBlocks);

        RegisterDefault(
            KeyboardShortcut.CtrlShift(Key.U),
            "UndoLastChange",
            "Undo last file change",
            ShortcutContext.Global,
            ShortcutCategory.History);

        RegisterDefault(
            KeyboardShortcut.CtrlShift(Key.D),
            "ShowDiff",
            "Show diff for selected code block",
            ShortcutContext.Global,
            ShortcutCategory.DiffViewer);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.OemComma),
            "OpenSettings",
            "Open settings",
            ShortcutContext.Global,
            ShortcutCategory.General);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.N),
            "NewChat",
            "Start new chat",
            ShortcutContext.Global,
            ShortcutCategory.Chat);

        RegisterDefault(
            KeyboardShortcut.KeyOnly(Key.F1),
            "ShowShortcutsHelp",
            "Show keyboard shortcuts help",
            ShortcutContext.Global,
            ShortcutCategory.General);

        RegisterDefault(
            KeyboardShortcut.CtrlShift(Key.H),
            "ToggleHistory",
            "Toggle change history panel",
            ShortcutContext.Global,
            ShortcutCategory.History);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.B),
            "ToggleSidebar",
            "Toggle sidebar",
            ShortcutContext.Global,
            ShortcutCategory.Navigation);

        // ═══════════════════════════════════════════════════════════
        // Chat Input Shortcuts
        // ═══════════════════════════════════════════════════════════

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.Return),
            "SendMessage",
            "Send message",
            ShortcutContext.ChatInput,
            ShortcutCategory.Chat);

        RegisterDefault(
            KeyboardShortcut.KeyOnly(Key.Escape),
            "CancelGeneration",
            "Cancel generation",
            ShortcutContext.ChatInput,
            ShortcutCategory.Chat);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.L),
            "ClearChat",
            "Clear chat history",
            ShortcutContext.ChatInput,
            ShortcutCategory.Chat);

        // ═══════════════════════════════════════════════════════════
        // Code Block Shortcuts
        // ═══════════════════════════════════════════════════════════

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.Return),
            "ApplyBlock",
            "Apply code block",
            ShortcutContext.CodeBlock,
            ShortcutCategory.CodeBlocks);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.C),
            "CopyBlock",
            "Copy code block",
            ShortcutContext.CodeBlock,
            ShortcutCategory.CodeBlocks);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.D),
            "ShowBlockDiff",
            "Show diff for code block",
            ShortcutContext.CodeBlock,
            ShortcutCategory.CodeBlocks);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.O),
            "OpenInEditor",
            "Open target file in editor",
            ShortcutContext.CodeBlock,
            ShortcutCategory.CodeBlocks);

        RegisterDefault(
            KeyboardShortcut.CtrlShift(Key.O),
            "ApplyWithOptions",
            "Apply with options dialog",
            ShortcutContext.CodeBlock,
            ShortcutCategory.CodeBlocks);

        // ═══════════════════════════════════════════════════════════
        // Diff Viewer Shortcuts
        // ═══════════════════════════════════════════════════════════

        RegisterDefault(
            KeyboardShortcut.KeyOnly(Key.Return),
            "ApplyChanges",
            "Apply changes",
            ShortcutContext.DiffViewer,
            ShortcutCategory.DiffViewer);

        RegisterDefault(
            KeyboardShortcut.KeyOnly(Key.Escape),
            "CloseDiff",
            "Close diff viewer",
            ShortcutContext.DiffViewer,
            ShortcutCategory.DiffViewer);

        RegisterDefault(
            KeyboardShortcut.KeyOnly(Key.J),
            "NextHunk",
            "Navigate to next hunk",
            ShortcutContext.DiffViewer,
            ShortcutCategory.DiffViewer);

        RegisterDefault(
            KeyboardShortcut.KeyOnly(Key.K),
            "PreviousHunk",
            "Navigate to previous hunk",
            ShortcutContext.DiffViewer,
            ShortcutCategory.DiffViewer);

        // ═══════════════════════════════════════════════════════════
        // Navigation Shortcuts
        // ═══════════════════════════════════════════════════════════

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.Tab),
            "NextChat",
            "Switch to next chat",
            ShortcutContext.Global,
            ShortcutCategory.Navigation);

        RegisterDefault(
            KeyboardShortcut.CtrlShift(Key.Tab),
            "PreviousChat",
            "Switch to previous chat",
            ShortcutContext.Global,
            ShortcutCategory.Navigation);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.W),
            "CloseChat",
            "Close current chat",
            ShortcutContext.Global,
            ShortcutCategory.Navigation);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.P),
            "QuickOpen",
            "Quick open file",
            ShortcutContext.Global,
            ShortcutCategory.Navigation);

        // ═══════════════════════════════════════════════════════════
        // File Tree Shortcuts
        // ═══════════════════════════════════════════════════════════

        RegisterDefault(
            KeyboardShortcut.KeyOnly(Key.Return),
            "OpenFile",
            "Open selected file",
            ShortcutContext.FileTree,
            ShortcutCategory.FileOperations);

        RegisterDefault(
            KeyboardShortcut.KeyOnly(Key.Delete),
            "DeleteFile",
            "Delete selected file",
            ShortcutContext.FileTree,
            ShortcutCategory.FileOperations);

        RegisterDefault(
            KeyboardShortcut.KeyOnly(Key.F2),
            "RenameFile",
            "Rename selected file",
            ShortcutContext.FileTree,
            ShortcutCategory.FileOperations);

        // ═══════════════════════════════════════════════════════════
        // Editor Shortcuts
        // ═══════════════════════════════════════════════════════════

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.S),
            "SaveFile",
            "Save current file",
            ShortcutContext.Editor,
            ShortcutCategory.Editor);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.G),
            "GoToLine",
            "Go to line",
            ShortcutContext.Editor,
            ShortcutCategory.Editor);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.F),
            "Find",
            "Find in file",
            ShortcutContext.Editor,
            ShortcutCategory.Editor);

        RegisterDefault(
            KeyboardShortcut.Ctrl(Key.H),
            "Replace",
            "Find and replace",
            ShortcutContext.Editor,
            ShortcutCategory.Editor);

        // ═══════════════════════════════════════════════════════════
        // v0.3.5g Compatibility (workspace shortcuts)
        // ═══════════════════════════════════════════════════════════

        Register(Key.O, KeyModifiers.Control, "workspace.open", "Open Folder");
        Register(Key.O, KeyModifiers.Control | KeyModifiers.Shift, "file.open", "Open File");
        Register(Key.S, KeyModifiers.Control, "file.save", "Save");
        Register(Key.S, KeyModifiers.Control | KeyModifiers.Shift, "file.saveAll", "Save All");
        Register(Key.W, KeyModifiers.Control, "editor.closeTab", "Close Tab");
        Register(Key.Tab, KeyModifiers.Control, "editor.nextTab", "Next Tab");
        Register(Key.Tab, KeyModifiers.Control | KeyModifiers.Shift, "editor.previousTab", "Previous Tab");
        Register(Key.Enter, KeyModifiers.Control, "chat.send", "Send Message");
        Register(Key.L, KeyModifiers.Control, "chat.clear", "Clear Chat");
        Register(Key.A, KeyModifiers.Control | KeyModifiers.Shift, "context.attachSelection", "Attach Selection");
        Register(Key.E, KeyModifiers.Control | KeyModifiers.Shift, "context.attachFile", "Attach Current File");
        Register(Key.F2, KeyModifiers.None, "explorer.rename", "Rename");
    }

    private void RegisterDefault(
        KeyboardShortcut shortcut,
        string actionId,
        string description,
        ShortcutContext context,
        ShortcutCategory category)
    {
        var handler = new ShortcutHandler
        {
            Shortcut = shortcut,
            ActionId = actionId,
            Description = description,
            Context = context,
            Category = category,
            DefaultShortcut = shortcut
        };

        _defaultShortcuts.Add(handler);
        _actionHandlers[actionId] = handler;
    }

    #endregion

    #region Registration

    /// <inheritdoc />
    public void Register(
        KeyboardShortcut shortcut,
        string actionId,
        string description,
        ShortcutContext context,
        ShortcutCategory category = ShortcutCategory.General)
    {
        var handler = new ShortcutHandler
        {
            Shortcut = shortcut,
            ActionId = actionId,
            Description = description,
            Context = context,
            Category = category,
            DefaultShortcut = shortcut
        };

        _actionHandlers[actionId] = handler;
        _logger?.LogDebug(
            "[REG] Registered shortcut {Shortcut} for action {ActionId} in context {Context}",
            shortcut, actionId, context);
    }

    /// <inheritdoc />
    public void Register(Key key, KeyModifiers modifiers, string commandId, string description)
    {
        var shortcutKey = (key, modifiers);
        _legacyShortcuts[shortcutKey] = new ShortcutInfo
        {
            Key = key,
            Modifiers = modifiers,
            CommandId = commandId,
            Description = description,
            Category = GetCategory(commandId)
        };
        _logger?.LogDebug("[REG] Registered legacy shortcut: {Key}+{Mods} → {Command}", key, modifiers, commandId);
    }

    /// <inheritdoc />
    public void RegisterAction(ShortcutActionRegistration registration)
    {
        _actionRegistrations[registration.ActionId] = registration;
        _logger?.LogDebug("[REG] Registered action handler for {ActionId}", registration.ActionId);
    }

    /// <inheritdoc />
    public void Unregister(KeyboardShortcut shortcut)
    {
        var toRemove = _actionHandlers.Where(kv => kv.Value.Shortcut == shortcut).ToList();
        foreach (var (key, _) in toRemove)
        {
            _actionHandlers.TryRemove(key, out _);
        }
        ShortcutsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void UnregisterAction(string actionId)
    {
        _actionHandlers.TryRemove(actionId, out _);
        _actionRegistrations.TryRemove(actionId, out _);
        ShortcutsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string GetCategory(string commandId)
    {
        var prefix = commandId.Split('.')[0];
        return prefix switch
        {
            "workspace" => "Workspace",
            "file" => "File",
            "editor" => "Editor",
            "chat" => "Chat",
            "context" => "Context",
            "explorer" => "Explorer",
            "settings" => "Settings",
            "sidebar" => "View",
            "quickOpen" => "Navigation",
            _ => "General"
        };
    }

    #endregion

    #region Customization

    /// <inheritdoc />
    public bool Rebind(string actionId, KeyboardShortcut newShortcut)
    {
        if (!_actionHandlers.TryGetValue(actionId, out var handler))
        {
            _logger?.LogWarning("[REBIND] Cannot rebind unknown action {ActionId}", actionId);
            return false;
        }

        // Check for conflicts
        var conflicts = GetConflicts(newShortcut, handler.Context).ToList();
        if (conflicts.Any(c => c.ActionId != actionId))
        {
            _logger?.LogWarning(
                "[REBIND] Cannot rebind {ActionId} to {Shortcut} - conflicts with {ConflictingAction}",
                actionId, newShortcut, conflicts.First().ActionId);
            return false;
        }

        // Create updated handler
        var updatedHandler = handler.WithShortcut(newShortcut);
        _actionHandlers[actionId] = updatedHandler;

        ShortcutsChanged?.Invoke(this, EventArgs.Empty);
        _logger?.LogInformation(
            "[REBIND] Rebound action {ActionId} from {OldShortcut} to {NewShortcut}",
            actionId, handler.Shortcut, newShortcut);

        return true;
    }

    /// <inheritdoc />
    public void ResetToDefault(string actionId)
    {
        var defaultHandler = _defaultShortcuts.FirstOrDefault(h => h.ActionId == actionId);
        if (defaultHandler is null)
        {
            _logger?.LogWarning("[RESET] No default shortcut for action {ActionId}", actionId);
            return;
        }

        var restoredHandler = new ShortcutHandler
        {
            Shortcut = defaultHandler.Shortcut,
            ActionId = defaultHandler.ActionId,
            Description = defaultHandler.Description,
            Context = defaultHandler.Context,
            Category = defaultHandler.Category,
            DefaultShortcut = defaultHandler.Shortcut,
            IsCustom = false
        };

        _actionHandlers[actionId] = restoredHandler;
        ShortcutsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void ResetAllToDefaults()
    {
        _actionHandlers.Clear();

        foreach (var handler in _defaultShortcuts)
        {
            _actionHandlers[handler.ActionId] = new ShortcutHandler
            {
                Shortcut = handler.Shortcut,
                ActionId = handler.ActionId,
                Description = handler.Description,
                Context = handler.Context,
                Category = handler.Category,
                DefaultShortcut = handler.Shortcut,
                IsCustom = false
            };
        }

        ShortcutsChanged?.Invoke(this, EventArgs.Empty);
        _logger?.LogInformation("[RESET] Reset all shortcuts to defaults");
    }

    /// <inheritdoc />
    public void SetEnabled(string actionId, bool enabled)
    {
        if (_actionHandlers.TryGetValue(actionId, out var handler))
        {
            handler.IsEnabled = enabled;
            ShortcutsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion

    #region Query

    /// <inheritdoc />
    public ShortcutHandler? GetHandler(KeyboardShortcut shortcut)
    {
        return _actionHandlers.Values.FirstOrDefault(h => h.Shortcut == shortcut);
    }

    /// <inheritdoc />
    public ShortcutHandler? GetHandlerByActionId(string actionId)
    {
        return _actionHandlers.TryGetValue(actionId, out var handler) ? handler : null;
    }

    /// <inheritdoc />
    public IEnumerable<ShortcutHandler> GetHandlersForContext(ShortcutContext context)
    {
        return _actionHandlers.Values.Where(h => h.Context == context);
    }

    /// <inheritdoc />
    public IEnumerable<ShortcutHandler> GetHandlersByCategory(ShortcutCategory category)
    {
        return _actionHandlers.Values.Where(h => h.Category == category);
    }

    /// <inheritdoc />
    public IEnumerable<ShortcutHandler> GetAllHandlers()
    {
        return _actionHandlers.Values;
    }

    /// <inheritdoc />
    public IReadOnlyList<ShortcutInfo> GetAllShortcuts()
    {
        return _legacyShortcuts.Values
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Description)
            .ToList();
    }

    /// <inheritdoc />
    public IEnumerable<ShortcutHandler> GetConflicts(KeyboardShortcut shortcut, ShortcutContext context)
    {
        // A conflict exists if:
        // 1. Same shortcut in same context
        // 2. Same shortcut where one is Global (global conflicts with all)
        return _actionHandlers.Values.Where(h =>
            h.Shortcut == shortcut &&
            (h.Context == context ||
             h.Context == ShortcutContext.Global ||
             context == ShortcutContext.Global));
    }

    /// <inheritdoc />
    public KeyboardShortcut? GetShortcutForAction(string actionId)
    {
        return _actionHandlers.TryGetValue(actionId, out var handler)
            ? handler.Shortcut
            : null;
    }

    #endregion

    #region Dispatch

    /// <inheritdoc />
    public async Task<bool> TryHandleAsync(Key key, KeyModifiers modifiers, ShortcutContext context)
    {
        var shortcut = new KeyboardShortcut(key, modifiers);

        // Try context-specific first, then global
        var handler = _actionHandlers.Values.FirstOrDefault(h =>
            h.Shortcut == shortcut && h.Context == context);

        if (handler is null)
        {
            handler = _actionHandlers.Values.FirstOrDefault(h =>
                h.Shortcut == shortcut && h.Context == ShortcutContext.Global);
        }

        if (handler is not null && handler.IsEnabled)
        {
            _logger?.LogDebug(
                "[DISPATCH] Handling shortcut {Shortcut} as action {ActionId}",
                shortcut, handler.ActionId);

            // Raise event
            var args = new ShortcutTriggeredEventArgs
            {
                Shortcut = shortcut,
                ActionId = handler.ActionId,
                Context = context
            };
            ShortcutTriggered?.Invoke(this, args);

            if (args.Handled)
                return true;

            // Try to execute action
            if (_actionRegistrations.TryGetValue(handler.ActionId, out var registration))
            {
                if (registration.CanExecute?.Invoke() != false)
                {
                    var result = await registration.Handler(context);
                    return result;
                }
            }

            // Action registered but no handler - still mark as handled
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryHandle(Key key, KeyModifiers modifiers, ShortcutContext context, out string? actionId)
    {
        var shortcut = new KeyboardShortcut(key, modifiers);

        var handler = _actionHandlers.Values.FirstOrDefault(h =>
            h.Shortcut == shortcut && (h.Context == context || h.Context == ShortcutContext.Global));

        if (handler is not null && handler.IsEnabled)
        {
            actionId = handler.ActionId;
            return true;
        }

        actionId = null;
        return false;
    }

    /// <inheritdoc />
    public bool HandleKeyPress(KeyEventArgs e)
    {
        var key = (e.Key, e.KeyModifiers);

        if (_legacyShortcuts.TryGetValue(key, out var shortcut))
        {
            _logger?.LogDebug("[KEY] Matched legacy shortcut: {Command}", shortcut.CommandId);
            CommandRequested?.Invoke(this, shortcut.CommandId);
            return true;
        }

        return false;
    }

    #endregion

    #region Persistence

    /// <inheritdoc />
    public async Task SaveCustomizationsAsync(CancellationToken cancellationToken = default)
    {
        var customizations = _actionHandlers.Values
            .Where(h => h.IsCustom)
            .Select(h => $"{h.ActionId}|{h.Shortcut}|{h.IsEnabled}")
            .ToList();

        var settings = _settingsService.CurrentSettings;
        settings.CustomShortcuts = customizations;
        await _settingsService.SaveSettingsAsync(settings);
        _logger?.LogInformation("[PERSIST] Saved {Count} shortcut customizations", customizations.Count);
    }

    /// <inheritdoc />
    public Task LoadCustomizationsAsync(CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.CurrentSettings;
        if (settings.CustomShortcuts is null || settings.CustomShortcuts.Count == 0)
        {
            _logger?.LogDebug("[PERSIST] No customizations to load");
            return Task.CompletedTask;
        }

        foreach (var entry in settings.CustomShortcuts)
        {
            var parts = entry.Split('|');
            if (parts.Length >= 2 && _actionHandlers.TryGetValue(parts[0], out var handler))
            {
                if (KeyboardShortcut.TryParse(parts[1], out var shortcut))
                {
                    handler.Shortcut = shortcut;
                    handler.IsCustom = true;
                    if (parts.Length >= 3 && bool.TryParse(parts[2], out var isEnabled))
                    {
                        handler.IsEnabled = isEnabled;
                    }
                }
            }
        }

        _logger?.LogInformation("[PERSIST] Loaded {Count} shortcut customizations", settings.CustomShortcuts.Count);
        return Task.CompletedTask;
    }

    #endregion
}
