// ============================================================================
// File: TerminalShortcutService.cs
// Path: src/AIntern.Services/TerminalShortcutService.cs
// Description: Service for managing terminal keyboard shortcuts with default
//              bindings, custom overrides, persistence, and conflict detection.
// Created: 2026-01-18
// AI Intern v0.5.5d - Keyboard Shortcuts System
// ============================================================================

namespace AIntern.Services;

using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalShortcutService (v0.5.5d)                                            │
// │ Manages terminal keyboard shortcuts with registry and customization.        │
// │                                                                              │
// │ Features:                                                                    │
// │   - Default bindings for 35+ terminal actions                               │
// │   - Custom binding overrides with persistence                               │
// │   - Conflict detection and resolution                                       │
// │   - PTY pass-through for shell shortcuts                                    │
// │   - Category-based organization for settings UI                             │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for managing and handling keyboard shortcuts.
/// </summary>
/// <remarks>
/// <para>
/// This service maintains two sets of bindings:
/// </para>
/// <list type="bullet">
///   <item><description>Default bindings: Hard-coded defaults for all actions</description></item>
///   <item><description>Active bindings: Current bindings (may differ from defaults)</description></item>
/// </list>
/// <para>
/// Custom bindings are loaded from and saved to <see cref="ISettingsService"/>.
/// </para>
/// <para>Added in v0.5.5d.</para>
/// </remarks>
public sealed class TerminalShortcutService : ITerminalShortcutService
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Constants
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Category name for Terminal Panel shortcuts.</summary>
    private const string CategoryTerminalPanel = "Terminal Panel";

    /// <summary>Category name for Terminal Input shortcuts (PTY pass-through).</summary>
    private const string CategoryTerminalInput = "Terminal Input";

    /// <summary>Category name for Terminal Search shortcuts.</summary>
    private const string CategoryTerminalSearch = "Terminal Search";

    /// <summary>Category name for Terminal Selection shortcuts.</summary>
    private const string CategoryTerminalSelection = "Terminal Selection";

    /// <summary>Category name for Terminal Scroll shortcuts.</summary>
    private const string CategoryTerminalScroll = "Terminal Scroll";

    /// <summary>Category name for Command Block shortcuts.</summary>
    private const string CategoryCommandBlocks = "Command Blocks";

    // ═══════════════════════════════════════════════════════════════════════════
    // Fields
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logger for diagnostic output.</summary>
    private readonly ILogger<TerminalShortcutService> _logger;

    /// <summary>Settings service for persistence.</summary>
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// Key combination → Binding lookup for fast key event handling.
    /// Key: (KeyName, Modifiers)
    /// </summary>
    private readonly Dictionary<(string Key, KeyModifierFlags Modifiers), KeyBinding> _bindings = new();

    /// <summary>
    /// Default bindings by action. Immutable after initialization.
    /// </summary>
    private readonly Dictionary<TerminalShortcutAction, KeyBinding> _defaultBindings = new();

    /// <summary>
    /// Current active bindings by action. May differ from defaults.
    /// </summary>
    private readonly Dictionary<TerminalShortcutAction, KeyBinding> _actionBindings = new();

    // ═══════════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public event EventHandler? BindingsChanged;

    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new <see cref="TerminalShortcutService"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="settingsService">Settings service for persistence.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public TerminalShortcutService(
        ILogger<TerminalShortcutService> logger,
        ISettingsService settingsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // ─────────────────────────────────────────────────────────────────────
        // Initialize Default Bindings
        // ─────────────────────────────────────────────────────────────────────
        _logger.LogDebug("Initializing keyboard shortcut service");
        InitializeDefaultBindings();

        // ─────────────────────────────────────────────────────────────────────
        // Load Custom Bindings
        // ─────────────────────────────────────────────────────────────────────
        LoadCustomBindings();

        _logger.LogInformation(
            "Keyboard shortcut service initialized: {Total} bindings, {Custom} custom",
            _actionBindings.Count,
            _settingsService.CurrentSettings?.CustomKeyBindings?.Count ?? 0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Default Binding Initialization
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes all default keyboard bindings.
    /// </summary>
    private void InitializeDefaultBindings()
    {
        _logger.LogDebug("Registering default keyboard bindings");

        // ─────────────────────────────────────────────────────────────────────
        // Terminal Panel Shortcuts
        // ─────────────────────────────────────────────────────────────────────
        Register(TerminalShortcutAction.ToggleTerminal,
            "OemTilde", KeyModifierFlags.Control,
            "Toggle Terminal", CategoryTerminalPanel);

        Register(TerminalShortcutAction.NewTerminal,
            "OemTilde", KeyModifierFlags.Control | KeyModifierFlags.Shift,
            "New Terminal", CategoryTerminalPanel);

        Register(TerminalShortcutAction.CloseTerminal,
            "W", KeyModifierFlags.Control | KeyModifierFlags.Shift,
            "Close Terminal", CategoryTerminalPanel);

        Register(TerminalShortcutAction.PreviousTerminalTab,
            "PageUp", KeyModifierFlags.Control,
            "Previous Tab", CategoryTerminalPanel);

        Register(TerminalShortcutAction.NextTerminalTab,
            "PageDown", KeyModifierFlags.Control,
            "Next Tab", CategoryTerminalPanel);

        Register(TerminalShortcutAction.MaximizeTerminal,
            "M", KeyModifierFlags.Control | KeyModifierFlags.Shift,
            "Maximize Terminal", CategoryTerminalPanel);

        // Terminal tabs 1-9
        RegisterTerminalTabBindings();

        // ─────────────────────────────────────────────────────────────────────
        // Terminal Input (PTY Pass-Through)
        // These shortcuts pass to the shell instead of being handled by app
        // ─────────────────────────────────────────────────────────────────────
        Register(TerminalShortcutAction.SendInterrupt,
            "C", KeyModifierFlags.Control,
            "Interrupt (SIGINT)", CategoryTerminalInput, passToPty: true);

        Register(TerminalShortcutAction.SendSuspend,
            "Z", KeyModifierFlags.Control,
            "Suspend (SIGTSTP)", CategoryTerminalInput, passToPty: true);

        Register(TerminalShortcutAction.SendEof,
            "D", KeyModifierFlags.Control,
            "Send EOF", CategoryTerminalInput, passToPty: true);

        Register(TerminalShortcutAction.ClearTerminal,
            "L", KeyModifierFlags.Control,
            "Clear Terminal", CategoryTerminalInput, passToPty: true);

        Register(TerminalShortcutAction.ClearLineBefore,
            "U", KeyModifierFlags.Control,
            "Clear Line Before Cursor", CategoryTerminalInput, passToPty: true);

        Register(TerminalShortcutAction.ClearLineAfter,
            "K", KeyModifierFlags.Control,
            "Clear Line After Cursor", CategoryTerminalInput, passToPty: true);

        Register(TerminalShortcutAction.DeleteWordBefore,
            "W", KeyModifierFlags.Control,
            "Delete Word Before", CategoryTerminalInput, passToPty: true);

        Register(TerminalShortcutAction.MoveToLineStart,
            "A", KeyModifierFlags.Control,
            "Move to Line Start", CategoryTerminalInput, passToPty: true);

        Register(TerminalShortcutAction.MoveToLineEnd,
            "E", KeyModifierFlags.Control,
            "Move to Line End", CategoryTerminalInput, passToPty: true);

        // ─────────────────────────────────────────────────────────────────────
        // Terminal Search
        // ─────────────────────────────────────────────────────────────────────
        Register(TerminalShortcutAction.OpenSearch,
            "F", KeyModifierFlags.Control,
            "Open Search", CategoryTerminalSearch);

        Register(TerminalShortcutAction.CloseSearch,
            "Escape", KeyModifierFlags.None,
            "Close Search", CategoryTerminalSearch);

        Register(TerminalShortcutAction.NextSearchResult,
            "F3", KeyModifierFlags.None,
            "Next Result", CategoryTerminalSearch);

        Register(TerminalShortcutAction.PreviousSearchResult,
            "F3", KeyModifierFlags.Shift,
            "Previous Result", CategoryTerminalSearch);

        Register(TerminalShortcutAction.ToggleSearchCaseSensitive,
            "C", KeyModifierFlags.Alt,
            "Toggle Case Sensitivity", CategoryTerminalSearch);

        Register(TerminalShortcutAction.ToggleSearchRegex,
            "R", KeyModifierFlags.Alt,
            "Toggle Regex", CategoryTerminalSearch);

        // ─────────────────────────────────────────────────────────────────────
        // Terminal Selection
        // Use Ctrl+Shift to avoid conflict with PTY shortcuts
        // ─────────────────────────────────────────────────────────────────────
        Register(TerminalShortcutAction.Copy,
            "C", KeyModifierFlags.Control | KeyModifierFlags.Shift,
            "Copy", CategoryTerminalSelection);

        Register(TerminalShortcutAction.Paste,
            "V", KeyModifierFlags.Control | KeyModifierFlags.Shift,
            "Paste", CategoryTerminalSelection);

        Register(TerminalShortcutAction.SelectAll,
            "A", KeyModifierFlags.Control | KeyModifierFlags.Shift,
            "Select All", CategoryTerminalSelection);

        // ─────────────────────────────────────────────────────────────────────
        // Terminal Scroll
        // ─────────────────────────────────────────────────────────────────────
        Register(TerminalShortcutAction.ScrollPageUp,
            "PageUp", KeyModifierFlags.Shift,
            "Scroll Page Up", CategoryTerminalScroll);

        Register(TerminalShortcutAction.ScrollPageDown,
            "PageDown", KeyModifierFlags.Shift,
            "Scroll Page Down", CategoryTerminalScroll);

        Register(TerminalShortcutAction.ScrollToTop,
            "Home", KeyModifierFlags.Shift,
            "Scroll to Top", CategoryTerminalScroll);

        Register(TerminalShortcutAction.ScrollToBottom,
            "End", KeyModifierFlags.Shift,
            "Scroll to Bottom", CategoryTerminalScroll);

        Register(TerminalShortcutAction.ScrollLineUp,
            "Up", KeyModifierFlags.Control | KeyModifierFlags.Shift,
            "Scroll Line Up", CategoryTerminalScroll);

        Register(TerminalShortcutAction.ScrollLineDown,
            "Down", KeyModifierFlags.Control | KeyModifierFlags.Shift,
            "Scroll Line Down", CategoryTerminalScroll);

        // ─────────────────────────────────────────────────────────────────────
        // Command Blocks
        // ─────────────────────────────────────────────────────────────────────
        Register(TerminalShortcutAction.ExecuteCommand,
            "Return", KeyModifierFlags.Control,
            "Execute Command", CategoryCommandBlocks);

        Register(TerminalShortcutAction.SendToTerminal,
            "Return", KeyModifierFlags.Control | KeyModifierFlags.Shift,
            "Send to Terminal", CategoryCommandBlocks);

        _logger.LogDebug("Registered {Count} default bindings", _defaultBindings.Count);
    }

    /// <summary>
    /// Registers terminal tab switch bindings (1-9).
    /// </summary>
    private void RegisterTerminalTabBindings()
    {
        var tabActions = new[]
        {
            TerminalShortcutAction.SwitchToTerminal1,
            TerminalShortcutAction.SwitchToTerminal2,
            TerminalShortcutAction.SwitchToTerminal3,
            TerminalShortcutAction.SwitchToTerminal4,
            TerminalShortcutAction.SwitchToTerminal5,
            TerminalShortcutAction.SwitchToTerminal6,
            TerminalShortcutAction.SwitchToTerminal7,
            TerminalShortcutAction.SwitchToTerminal8,
            TerminalShortcutAction.SwitchToTerminal9
        };

        for (int i = 0; i < tabActions.Length; i++)
        {
            var key = $"D{i + 1}"; // D1, D2, D3, etc.
            Register(tabActions[i],
                key, KeyModifierFlags.Control | KeyModifierFlags.Shift,
                $"Switch to Terminal {i + 1}", CategoryTerminalPanel);
        }
    }

    /// <summary>
    /// Registers a default binding.
    /// </summary>
    private void Register(
        TerminalShortcutAction action,
        string key,
        KeyModifierFlags modifiers,
        string description,
        string category,
        bool passToPty = false,
        bool isCustomizable = true)
    {
        var binding = new KeyBinding
        {
            Action = action,
            Key = key,
            Modifiers = modifiers,
            Description = description,
            Category = category,
            PassToPty = passToPty,
            IsCustomizable = isCustomizable
        };

        _defaultBindings[action] = binding;
        _bindings[(key, modifiers)] = binding;
        _actionBindings[action] = binding;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Custom Binding Management
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads custom bindings from settings.
    /// </summary>
    private void LoadCustomBindings()
    {
        var customBindings = _settingsService.CurrentSettings?.CustomKeyBindings;
        if (customBindings == null || customBindings.Count == 0)
        {
            _logger.LogDebug("No custom bindings to load");
            return;
        }

        _logger.LogDebug("Loading {Count} custom bindings", customBindings.Count);

        foreach (var (actionName, keyCombo) in customBindings)
        {
            // ─────────────────────────────────────────────────────────────────
            // Parse Action Name
            // ─────────────────────────────────────────────────────────────────
            if (!Enum.TryParse<TerminalShortcutAction>(actionName, out var action))
            {
                _logger.LogWarning("Unknown action in custom bindings: {Action}", actionName);
                continue;
            }

            try
            {
                // ─────────────────────────────────────────────────────────────
                // Parse Key Combination
                // ─────────────────────────────────────────────────────────────
                var (key, modifiers) = KeyBinding.Parse(keyCombo);

                // ─────────────────────────────────────────────────────────────
                // Remove Old Binding
                // ─────────────────────────────────────────────────────────────
                if (_actionBindings.TryGetValue(action, out var oldBinding))
                {
                    _bindings.Remove((oldBinding.Key, oldBinding.Modifiers));
                }

                // ─────────────────────────────────────────────────────────────
                // Add New Binding
                // ─────────────────────────────────────────────────────────────
                var defaultBinding = _defaultBindings[action];
                var newBinding = defaultBinding.WithKey(key, modifiers);

                _bindings[(key, modifiers)] = newBinding;
                _actionBindings[action] = newBinding;

                _logger.LogDebug(
                    "Loaded custom binding: {Action} = {Key}",
                    action, newBinding.DisplayString);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to parse custom binding: {Action}={KeyCombo}",
                    actionName, keyCombo);
            }
        }
    }

    /// <summary>
    /// Saves custom bindings to settings.
    /// </summary>
    private async Task SaveCustomBindingsAsync()
    {
        var customBindings = new Dictionary<string, string>();

        foreach (var (action, binding) in _actionBindings)
        {
            var defaultBinding = _defaultBindings[action];

            // Only save if different from default
            if (binding.Key != defaultBinding.Key || binding.Modifiers != defaultBinding.Modifiers)
            {
                customBindings[action.ToString()] = binding.SerializedString;
            }
        }

        var settings = _settingsService.CurrentSettings;
        settings.CustomKeyBindings = customBindings.Count > 0 ? customBindings : null;

        await _settingsService.SaveSettingsAsync(settings);

        _logger.LogDebug("Saved {Count} custom bindings", customBindings.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ITerminalShortcutService Implementation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetAllBindings() =>
        _actionBindings.Values.ToList();

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindingsByCategory(string category) =>
        _actionBindings.Values
            .Where(b => b.Category == category)
            .OrderBy(b => b.Description)
            .ToList();

    /// <inheritdoc />
    public IReadOnlyList<string> GetCategories() =>
        _actionBindings.Values
            .Select(b => b.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

    /// <inheritdoc />
    public KeyBinding? GetBinding(TerminalShortcutAction action) =>
        _actionBindings.GetValueOrDefault(action);

    /// <inheritdoc />
    public bool TryGetAction(string key, KeyModifierFlags modifiers, out TerminalShortcutAction action)
    {
        if (_bindings.TryGetValue((key, modifiers), out var binding))
        {
            action = binding.Action;
            _logger.LogTrace(
                "Key matched: {Key}+{Modifiers} → {Action}",
                key, modifiers, action);
            return true;
        }

        action = default;
        return false;
    }

    /// <inheritdoc />
    public KeyBinding? GetBindingByKey(string key, KeyModifierFlags modifiers) =>
        _bindings.GetValueOrDefault((key, modifiers));

    /// <inheritdoc />
    public bool UpdateBinding(TerminalShortcutAction action, string key, KeyModifierFlags modifiers)
    {
        // ─────────────────────────────────────────────────────────────────────
        // Validate Action Exists
        // ─────────────────────────────────────────────────────────────────────
        if (!_defaultBindings.TryGetValue(action, out var defaultBinding))
        {
            _logger.LogWarning("Attempted to update unknown action: {Action}", action);
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Validate Customizable
        // ─────────────────────────────────────────────────────────────────────
        if (!defaultBinding.IsCustomizable)
        {
            _logger.LogWarning("Attempted to update non-customizable binding: {Action}", action);
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Check for Conflicts
        // ─────────────────────────────────────────────────────────────────────
        if (HasConflict(key, modifiers, action))
        {
            var conflict = GetConflictingBinding(key, modifiers, action);
            _logger.LogWarning(
                "Binding conflict: {Action} conflicts with {Conflict}",
                action, conflict?.Action);
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Remove Old Binding
        // ─────────────────────────────────────────────────────────────────────
        if (_actionBindings.TryGetValue(action, out var oldBinding))
        {
            _bindings.Remove((oldBinding.Key, oldBinding.Modifiers));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Add New Binding
        // ─────────────────────────────────────────────────────────────────────
        var newBinding = defaultBinding.WithKey(key, modifiers);
        _bindings[(key, modifiers)] = newBinding;
        _actionBindings[action] = newBinding;

        // ─────────────────────────────────────────────────────────────────────
        // Persist and Notify
        // ─────────────────────────────────────────────────────────────────────
        // Fire and forget save - UI responsiveness is more important
        _ = SaveCustomBindingsAsync();
        BindingsChanged?.Invoke(this, EventArgs.Empty);

        _logger.LogInformation(
            "Updated binding: {Action} = {Key}",
            action, newBinding.DisplayString);

        return true;
    }

    /// <inheritdoc />
    public void ResetBinding(TerminalShortcutAction action)
    {
        if (!_defaultBindings.TryGetValue(action, out var defaultBinding))
        {
            _logger.LogDebug("Cannot reset unknown action: {Action}", action);
            return;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Remove Current Binding
        // ─────────────────────────────────────────────────────────────────────
        if (_actionBindings.TryGetValue(action, out var currentBinding))
        {
            _bindings.Remove((currentBinding.Key, currentBinding.Modifiers));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Restore Default
        // ─────────────────────────────────────────────────────────────────────
        _bindings[(defaultBinding.Key, defaultBinding.Modifiers)] = defaultBinding;
        _actionBindings[action] = defaultBinding;

        // ─────────────────────────────────────────────────────────────────────
        // Persist and Notify
        // ─────────────────────────────────────────────────────────────────────
        _ = SaveCustomBindingsAsync();
        BindingsChanged?.Invoke(this, EventArgs.Empty);

        _logger.LogInformation(
            "Reset binding: {Action} = {Key}",
            action, defaultBinding.DisplayString);
    }

    /// <inheritdoc />
    public void ResetAllBindings()
    {
        _logger.LogInformation("Resetting all bindings to defaults");

        _bindings.Clear();
        _actionBindings.Clear();

        foreach (var (action, binding) in _defaultBindings)
        {
            _bindings[(binding.Key, binding.Modifiers)] = binding;
            _actionBindings[action] = binding;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Persist and Notify
        // ─────────────────────────────────────────────────────────────────────
        _ = SaveCustomBindingsAsync();
        BindingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public bool HasConflict(string key, KeyModifierFlags modifiers, TerminalShortcutAction? exclude = null) =>
        GetConflictingBinding(key, modifiers, exclude) != null;

    /// <inheritdoc />
    public KeyBinding? GetConflictingBinding(string key, KeyModifierFlags modifiers, TerminalShortcutAction? exclude = null)
    {
        if (_bindings.TryGetValue((key, modifiers), out var binding))
        {
            if (exclude == null || binding.Action != exclude)
            {
                return binding;
            }
        }
        return null;
    }
}
