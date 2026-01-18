// ============================================================================
// File: KeyBinding.cs
// Path: src/AIntern.Core/Models/Terminal/KeyBinding.cs
// Description: Represents a keyboard shortcut binding with key, modifiers,
//              and metadata. Uses platform-independent key representation.
// Created: 2026-01-18
// AI Intern v0.5.5d - Keyboard Shortcuts System
// ============================================================================

namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ KeyBinding (v0.5.5d)                                                         │
// │ Represents a keyboard shortcut binding with platform-independent keys.      │
// │                                                                              │
// │ Platform Independence:                                                       │
// │   Keys are stored as strings for serialization and platform neutrality.     │
// │   The Desktop layer handles conversion to Avalonia.Input types.             │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a keyboard shortcut binding with key, modifiers, and metadata.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a platform-independent representation of keyboard shortcuts.
/// Keys are stored as strings (e.g., "F3", "C", "PageUp") and modifiers as flags.
/// </para>
/// <para>
/// The Desktop layer handles conversion to Avalonia.Input.Key and KeyModifiers
/// for actual keyboard event handling.
/// </para>
/// <para>Added in v0.5.5d.</para>
/// </remarks>
public sealed class KeyBinding
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the action this binding triggers.
    /// </summary>
    public TerminalShortcutAction Action { get; init; }

    /// <summary>
    /// Gets the primary key for this binding (e.g., "C", "F3", "PageUp").
    /// </summary>
    /// <remarks>
    /// Key names follow Avalonia's Key enum naming for easy conversion.
    /// Special keys use their full names (PageUp, PageDown, OemTilde, etc.).
    /// </remarks>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the required modifier keys for this binding.
    /// </summary>
    public KeyModifierFlags Modifiers { get; init; } = KeyModifierFlags.None;

    /// <summary>
    /// Gets the human-readable description of the shortcut action.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the category for grouping in settings UI.
    /// </summary>
    /// <remarks>
    /// Categories include: "Terminal Panel", "Terminal Input", "Terminal Search",
    /// "Terminal Selection", "Terminal Scroll", "Command Blocks".
    /// </remarks>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether this binding can be customized by the user.
    /// Some system shortcuts are not customizable.
    /// </summary>
    public bool IsCustomizable { get; init; } = true;

    /// <summary>
    /// Gets whether this shortcut should be passed through to the PTY
    /// instead of being handled by the application.
    /// </summary>
    /// <remarks>
    /// Set to <c>true</c> for shell shortcuts like Ctrl+C (SIGINT),
    /// Ctrl+Z (SIGTSTP), Ctrl+D (EOF), etc. The application should
    /// NOT handle these; they should pass directly to the PTY process.
    /// </remarks>
    public bool PassToPty { get; init; }

    // ═══════════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the formatted shortcut string for display.
    /// Platform-aware: uses ⌘ on macOS, Ctrl on Windows/Linux.
    /// </summary>
    public string DisplayString => FormatKeyBinding(usePlatformSymbols: true);

    /// <summary>
    /// Gets the shortcut in a parseable format (e.g., "Ctrl+Shift+C").
    /// Always uses text-based modifiers for serialization.
    /// </summary>
    public string SerializedString => FormatKeyBinding(usePlatformSymbols: false);

    /// <summary>
    /// Gets whether this binding has any modifiers.
    /// </summary>
    public bool HasModifiers => Modifiers != KeyModifierFlags.None;

    // ═══════════════════════════════════════════════════════════════════════════
    // Methods
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a copy of this binding with a new key/modifier combination.
    /// </summary>
    /// <param name="key">The new key.</param>
    /// <param name="modifiers">The new modifiers.</param>
    /// <returns>A new KeyBinding with updated key/modifiers.</returns>
    public KeyBinding WithKey(string key, KeyModifierFlags modifiers)
    {
        return new KeyBinding
        {
            Action = Action,
            Key = key,
            Modifiers = modifiers,
            Description = Description,
            Category = Category,
            IsCustomizable = IsCustomizable,
            PassToPty = PassToPty
        };
    }

    /// <summary>
    /// Checks if this binding matches the given key and modifiers.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="modifiers">The modifiers to check.</param>
    /// <returns>True if this binding matches.</returns>
    public bool Matches(string key, KeyModifierFlags modifiers)
    {
        return string.Equals(Key, key, StringComparison.OrdinalIgnoreCase) &&
               Modifiers == modifiers;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Formatting
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Formats the key binding for display or serialization.
    /// </summary>
    /// <param name="usePlatformSymbols">True to use platform-specific symbols.</param>
    /// <returns>Formatted key binding string.</returns>
    private string FormatKeyBinding(bool usePlatformSymbols)
    {
        var parts = new List<string>();

        // Detect macOS for platform-specific formatting
        var isMac = OperatingSystem.IsMacOS() && usePlatformSymbols;

        // ─────────────────────────────────────────────────────────────────────
        // Add Modifiers in Standard Order: Ctrl, Alt, Shift, Meta
        // ─────────────────────────────────────────────────────────────────────
        if (Modifiers.HasFlag(KeyModifierFlags.Control))
            parts.Add(isMac ? "⌘" : "Ctrl");

        if (Modifiers.HasFlag(KeyModifierFlags.Alt))
            parts.Add(isMac ? "⌥" : "Alt");

        if (Modifiers.HasFlag(KeyModifierFlags.Shift))
            parts.Add(isMac ? "⇧" : "Shift");

        if (Modifiers.HasFlag(KeyModifierFlags.Meta))
            parts.Add(isMac ? "⌃" : "Meta");

        // ─────────────────────────────────────────────────────────────────────
        // Add Key with User-Friendly Formatting
        // ─────────────────────────────────────────────────────────────────────
        parts.Add(FormatKeyName(Key));

        return string.Join("+", parts);
    }

    /// <summary>
    /// Formats a key name for user-friendly display.
    /// </summary>
    private static string FormatKeyName(string key) => key switch
    {
        "OemTilde" => "`",
        "OemMinus" => "-",
        "OemPlus" => "=",
        "OemOpenBrackets" => "[",
        "OemCloseBrackets" => "]",
        "OemPipe" => "\\",
        "OemSemicolon" => ";",
        "OemQuotes" => "'",
        "OemComma" => ",",
        "OemPeriod" => ".",
        "OemQuestion" => "/",
        "PageUp" => "PgUp",
        "PageDown" => "PgDn",
        "Escape" => "Esc",
        "Delete" => "Del",
        "Insert" => "Ins",
        "Back" => "Backspace",
        "Return" => "Enter",
        "Up" => "↑",
        "Down" => "↓",
        "Left" => "←",
        "Right" => "→",
        "D0" => "0",
        "D1" => "1",
        "D2" => "2",
        "D3" => "3",
        "D4" => "4",
        "D5" => "5",
        "D6" => "6",
        "D7" => "7",
        "D8" => "8",
        "D9" => "9",
        _ => key
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // Parsing
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parses a serialized string like "Ctrl+Shift+C" into key and modifiers.
    /// </summary>
    /// <param name="serialized">The serialized key binding string.</param>
    /// <returns>Tuple of (key, modifiers).</returns>
    /// <exception cref="ArgumentException">Thrown if the string cannot be parsed.</exception>
    public static (string key, KeyModifierFlags modifiers) Parse(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
            throw new ArgumentException("Serialized string cannot be empty", nameof(serialized));

        var parts = serialized.Split('+', StringSplitOptions.RemoveEmptyEntries);
        var modifiers = KeyModifierFlags.None;
        var key = string.Empty;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            switch (trimmed.ToLowerInvariant())
            {
                // ─────────────────────────────────────────────────────────────
                // Control Modifiers
                // ─────────────────────────────────────────────────────────────
                case "ctrl":
                case "control":
                case "⌘":
                    modifiers |= KeyModifierFlags.Control;
                    break;

                // ─────────────────────────────────────────────────────────────
                // Alt Modifiers
                // ─────────────────────────────────────────────────────────────
                case "alt":
                case "⌥":
                    modifiers |= KeyModifierFlags.Alt;
                    break;

                // ─────────────────────────────────────────────────────────────
                // Shift Modifiers
                // ─────────────────────────────────────────────────────────────
                case "shift":
                case "⇧":
                    modifiers |= KeyModifierFlags.Shift;
                    break;

                // ─────────────────────────────────────────────────────────────
                // Meta Modifiers
                // ─────────────────────────────────────────────────────────────
                case "meta":
                case "⌃":
                    modifiers |= KeyModifierFlags.Meta;
                    break;

                // ─────────────────────────────────────────────────────────────
                // Key Name (everything else)
                // ─────────────────────────────────────────────────────────────
                default:
                    key = ParseKeyName(trimmed);
                    break;
            }
        }

        return (key, modifiers);
    }

    /// <summary>
    /// Parses a user-friendly key name back to internal format.
    /// </summary>
    private static string ParseKeyName(string keyString) => keyString.ToLowerInvariant() switch
    {
        "`" => "OemTilde",
        "-" => "OemMinus",
        "=" => "OemPlus",
        "[" => "OemOpenBrackets",
        "]" => "OemCloseBrackets",
        "\\" => "OemPipe",
        ";" => "OemSemicolon",
        "'" => "OemQuotes",
        "," => "OemComma",
        "." => "OemPeriod",
        "/" => "OemQuestion",
        "pgup" or "pageup" => "PageUp",
        "pgdn" or "pagedown" => "PageDown",
        "esc" or "escape" => "Escape",
        "del" or "delete" => "Delete",
        "ins" or "insert" => "Insert",
        "backspace" or "back" => "Back",
        "enter" or "return" => "Return",
        "↑" or "up" => "Up",
        "↓" or "down" => "Down",
        "←" or "left" => "Left",
        "→" or "right" => "Right",
        "0" => "D0",
        "1" => "D1",
        "2" => "D2",
        "3" => "D3",
        "4" => "D4",
        "5" => "D5",
        "6" => "D6",
        "7" => "D7",
        "8" => "D8",
        "9" => "D9",
        // Preserve original casing for standard keys
        _ => char.ToUpper(keyString[0]) + keyString.Substring(1).ToLower()
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // Object Overrides
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a string representation of this key binding.
    /// </summary>
    public override string ToString() => $"{DisplayString} → {Description}";

    /// <summary>
    /// Determines if this binding equals another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is KeyBinding other &&
               Action == other.Action &&
               Key == other.Key &&
               Modifiers == other.Modifiers;
    }

    /// <summary>
    /// Returns a hash code for this binding.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(Action, Key, Modifiers);
}

// ═══════════════════════════════════════════════════════════════════════════════
// KeyModifierFlags Enum
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Flags representing keyboard modifier keys.
/// Platform-independent representation of modifiers.
/// </summary>
/// <remarks>
/// These flags correspond to Avalonia.Input.KeyModifiers but are
/// defined here for platform independence in the Core layer.
/// </remarks>
[Flags]
public enum KeyModifierFlags
{
    /// <summary>No modifiers.</summary>
    None = 0,

    /// <summary>Alt key (⌥ on macOS).</summary>
    Alt = 1,

    /// <summary>Control key (⌘ on macOS).</summary>
    Control = 2,

    /// <summary>Shift key (⇧ on macOS).</summary>
    Shift = 4,

    /// <summary>Meta key (⌃ on macOS, Windows key on Windows).</summary>
    Meta = 8
}
