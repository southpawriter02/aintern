using AIntern.Core.Interfaces;
using AIntern.Desktop.Models;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Services;

/// <summary>
/// Manages keyboard event handling and context detection for the main window (v0.4.5f).
/// </summary>
public sealed class ShortcutManager : IDisposable
{
    private readonly Window _window;
    private readonly IKeyboardShortcutService _shortcutService;
    private readonly Func<string, Task> _actionDispatcher;
    private readonly ILogger<ShortcutManager>? _logger;

    private bool _isDisposed;

    /// <summary>
    /// Initializes the shortcut manager with the target window.
    /// </summary>
    public ShortcutManager(
        Window window,
        IKeyboardShortcutService shortcutService,
        Func<string, Task> actionDispatcher,
        ILogger<ShortcutManager>? logger = null)
    {
        _window = window;
        _shortcutService = shortcutService;
        _actionDispatcher = actionDispatcher;
        _logger = logger;

        _window.KeyDown += OnKeyDown;
        _logger?.LogDebug("[INIT] ShortcutManager attached to window");
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Skip if already handled
        if (e.Handled)
            return;

        // Skip modifier-only key presses
        if (IsModifierKey(e.Key))
            return;

        var context = DetermineContext();

        _logger?.LogTrace(
            "[KEY] KeyDown: {Key}+{Mods} in context {Context}",
            e.Key, e.KeyModifiers, context);

        if (_shortcutService.TryHandle(e.Key, e.KeyModifiers, context, out var actionId))
        {
            e.Handled = true;
            _logger?.LogDebug("[KEY] Dispatching action {ActionId}", actionId);
            await _actionDispatcher(actionId!);
        }
    }

    /// <summary>
    /// Determines the current shortcut context based on focused element.
    /// </summary>
    public ShortcutContext DetermineContext()
    {
        var focused = TopLevel.GetTopLevel(_window)?.FocusManager?.GetFocusedElement();

        // Check for modal dialogs first
        if (IsModalOpen())
            return ShortcutContext.Modal;

        // Check specific control types
        if (focused is TextBox textBox)
        {
            if (textBox.Name == "ChatInputTextBox" || IsInChatInput(textBox))
                return ShortcutContext.ChatInput;
        }

        // Check parent containers by name pattern (avoids tight coupling to control types)
        if (focused is Control visual)
        {
            if (IsInControlByName(visual, "DiffViewer", "DiffPanel"))
                return ShortcutContext.DiffViewer;

            if (IsInControlByName(visual, "CodeBlock"))
                return ShortcutContext.CodeBlock;

            if (IsInControlByName(visual, "ChatPanel", "ChatView", "ChatMessageList"))
                return ShortcutContext.ChatView;

            if (IsInControlByName(visual, "FileTree", "FileExplorer"))
                return ShortcutContext.FileTree;

            if (IsInControlByName(visual, "ChangeHistory", "HistoryPanel"))
                return ShortcutContext.ChangeHistory;

            if (IsInControlByName(visual, "Settings", "SettingsPanel"))
                return ShortcutContext.Settings;

            if (IsInControlByName(visual, "EditorPanel", "TextEditor"))
                return ShortcutContext.Editor;
        }

        return ShortcutContext.Global;
    }

    private bool IsModalOpen()
    {
        // Check if any modal overlay or dialog is visible
        var overlays = _window.GetVisualDescendants()
            .OfType<Control>()
            .Where(c => c.Name?.Contains("Modal") == true ||
                        c.Name?.Contains("Dialog") == true ||
                        c.Name?.Contains("Overlay") == true);

        return overlays.Any(o => o.IsVisible);
    }

    private static bool IsInChatInput(Control control)
    {
        return control.FindAncestorOfType<Control>() is { } parent &&
               (parent.Name?.Contains("ChatInput") == true);
    }

    private static bool IsInControlByName(Control visual, params string[] namePatterns)
    {
        Control? current = visual;
        while (current != null)
        {
            if (current.Name != null)
            {
                foreach (var pattern in namePatterns)
                {
                    if (current.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            current = current.Parent as Control;
        }
        return false;
    }

    private static bool IsModifierKey(Key key)
    {
        return key switch
        {
            Key.LeftCtrl or Key.RightCtrl => true,
            Key.LeftShift or Key.RightShift => true,
            Key.LeftAlt or Key.RightAlt => true,
            Key.LWin or Key.RWin => true,
            _ => false
        };
    }

    /// <summary>
    /// Disposes of the shortcut manager and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _window.KeyDown -= OnKeyDown;
        _isDisposed = true;
        _logger?.LogDebug("[DISPOSE] ShortcutManager disposed");
    }
}
