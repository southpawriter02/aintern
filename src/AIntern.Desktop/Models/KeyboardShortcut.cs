using System.Runtime.InteropServices;
using Avalonia.Input;

namespace AIntern.Desktop.Models;

/// <summary>
/// Represents a keyboard shortcut combination (key + modifiers).
/// Immutable value type with equality semantics (v0.4.5f).
/// </summary>
public readonly struct KeyboardShortcut : IEquatable<KeyboardShortcut>
{
    /// <summary>
    /// The primary key for the shortcut.
    /// </summary>
    public Key Key { get; }

    /// <summary>
    /// Modifier keys (Ctrl, Shift, Alt, Meta/Cmd).
    /// </summary>
    public KeyModifiers Modifiers { get; }

    /// <summary>
    /// Creates a new keyboard shortcut.
    /// </summary>
    public KeyboardShortcut(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Key = key;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Returns true if this shortcut has any modifiers.
    /// </summary>
    public bool HasModifiers => Modifiers != KeyModifiers.None;

    /// <summary>
    /// Returns true if this shortcut uses the Control modifier.
    /// </summary>
    public bool HasControl => Modifiers.HasFlag(KeyModifiers.Control);

    /// <summary>
    /// Returns true if this shortcut uses the Shift modifier.
    /// </summary>
    public bool HasShift => Modifiers.HasFlag(KeyModifiers.Shift);

    /// <summary>
    /// Returns true if this shortcut uses the Alt modifier.
    /// </summary>
    public bool HasAlt => Modifiers.HasFlag(KeyModifiers.Alt);

    /// <summary>
    /// Returns true if this shortcut uses the Meta/Command modifier.
    /// </summary>
    public bool HasMeta => Modifiers.HasFlag(KeyModifiers.Meta);

    /// <summary>
    /// Returns true if this is a valid, usable shortcut.
    /// </summary>
    public bool IsValid => Key != Key.None;

    /// <summary>
    /// Empty/invalid shortcut constant.
    /// </summary>
    public static readonly KeyboardShortcut None = new(Key.None, KeyModifiers.None);

    #region String Conversion

    /// <summary>
    /// Returns a string representation (e.g., "Ctrl+Shift+A").
    /// </summary>
    public override string ToString()
    {
        if (!IsValid)
            return string.Empty;

        var parts = new List<string>(4);

        if (HasControl) parts.Add("Ctrl");
        if (HasShift) parts.Add("Shift");
        if (HasAlt) parts.Add("Alt");
        if (HasMeta) parts.Add("Meta");

        parts.Add(FormatKey(Key));

        return string.Join("+", parts);
    }

    /// <summary>
    /// Returns a platform-aware display string.
    /// On macOS: "⌘⇧A", on Windows/Linux: "Ctrl+Shift+A"
    /// </summary>
    public string ToDisplayString()
    {
        if (!IsValid)
            return string.Empty;

        var parts = new List<string>(4);
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        if (isMac)
        {
            // macOS order: Ctrl, Option (Alt), Shift, Cmd
            if (HasControl) parts.Add("\u2303"); // ⌃
            if (HasAlt) parts.Add("\u2325");     // ⌥
            if (HasShift) parts.Add("\u21E7");   // ⇧
            if (HasMeta) parts.Add("\u2318");    // ⌘
        }
        else
        {
            if (HasControl) parts.Add("Ctrl");
            if (HasAlt) parts.Add("Alt");
            if (HasShift) parts.Add("Shift");
            if (HasMeta) parts.Add("Win");
        }

        parts.Add(FormatKeyForDisplay(Key, isMac));

        return isMac ? string.Concat(parts) : string.Join("+", parts);
    }

    /// <summary>
    /// Parses a shortcut string like "Ctrl+Shift+A".
    /// </summary>
    public static KeyboardShortcut Parse(string? shortcutString)
    {
        if (string.IsNullOrWhiteSpace(shortcutString))
            return None;

        var parts = shortcutString.Split('+', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return None;

        var modifiers = KeyModifiers.None;
        Key key = Key.None;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var lower = trimmed.ToLowerInvariant();

            switch (lower)
            {
                case "ctrl":
                case "control":
                    modifiers |= KeyModifiers.Control;
                    break;
                case "shift":
                    modifiers |= KeyModifiers.Shift;
                    break;
                case "alt":
                case "option":
                    modifiers |= KeyModifiers.Alt;
                    break;
                case "meta":
                case "cmd":
                case "command":
                case "win":
                case "windows":
                    modifiers |= KeyModifiers.Meta;
                    break;
                default:
                    // Try to parse as a key
                    if (Enum.TryParse<Key>(trimmed, ignoreCase: true, out var parsedKey))
                    {
                        key = parsedKey;
                    }
                    break;
            }
        }

        return new KeyboardShortcut(key, modifiers);
    }

    /// <summary>
    /// Tries to parse a shortcut string.
    /// </summary>
    public static bool TryParse(string? shortcutString, out KeyboardShortcut shortcut)
    {
        shortcut = Parse(shortcutString);
        return shortcut.IsValid;
    }

    private static string FormatKey(Key key) => key switch
    {
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.OemPlus => "+",
        Key.OemMinus => "-",
        Key.OemOpenBrackets => "[",
        Key.OemCloseBrackets => "]",
        Key.OemSemicolon => ";",
        Key.OemQuotes => "'",
        Key.OemBackslash => "\\",
        Key.OemPipe => "|",
        Key.OemQuestion => "/",
        Key.OemTilde => "`",
        Key.Back => "Backspace",
        Key.Return => "Enter",
        Key.Escape => "Esc",
        Key.Space => "Space",
        Key.Prior => "PageUp",
        Key.Next => "PageDown",
        _ => key.ToString()
    };

    private static string FormatKeyForDisplay(Key key, bool isMac) => key switch
    {
        Key.Back => isMac ? "\u232B" : "Backspace",     // ⌫
        Key.Return => isMac ? "\u21A9" : "Enter",       // ↩
        Key.Escape => isMac ? "\u238B" : "Esc",         // ⎋
        Key.Space => isMac ? "\u2423" : "Space",        // ␣
        Key.Tab => isMac ? "\u21E5" : "Tab",            // ⇥
        Key.Up => isMac ? "\u2191" : "Up",              // ↑
        Key.Down => isMac ? "\u2193" : "Down",          // ↓
        Key.Left => isMac ? "\u2190" : "Left",          // ←
        Key.Right => isMac ? "\u2192" : "Right",        // →
        Key.Delete => isMac ? "\u2326" : "Del",         // ⌦
        Key.Home => isMac ? "\u2196" : "Home",          // ↖
        Key.End => isMac ? "\u2198" : "End",            // ↘
        Key.Prior => isMac ? "\u21DE" : "PgUp",         // ⇞
        Key.Next => isMac ? "\u21DF" : "PgDn",          // ⇟
        _ => FormatKey(key)
    };

    #endregion

    #region Equality

    public bool Equals(KeyboardShortcut other) =>
        Key == other.Key && Modifiers == other.Modifiers;

    public override bool Equals(object? obj) =>
        obj is KeyboardShortcut other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Key, Modifiers);

    public static bool operator ==(KeyboardShortcut left, KeyboardShortcut right) =>
        left.Equals(right);

    public static bool operator !=(KeyboardShortcut left, KeyboardShortcut right) =>
        !left.Equals(right);

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a Ctrl+Key shortcut.
    /// </summary>
    public static KeyboardShortcut Ctrl(Key key) =>
        new(key, KeyModifiers.Control);

    /// <summary>
    /// Creates a Ctrl+Shift+Key shortcut.
    /// </summary>
    public static KeyboardShortcut CtrlShift(Key key) =>
        new(key, KeyModifiers.Control | KeyModifiers.Shift);

    /// <summary>
    /// Creates a Ctrl+Alt+Key shortcut.
    /// </summary>
    public static KeyboardShortcut CtrlAlt(Key key) =>
        new(key, KeyModifiers.Control | KeyModifiers.Alt);

    /// <summary>
    /// Creates a Shift+Key shortcut.
    /// </summary>
    public static KeyboardShortcut Shift(Key key) =>
        new(key, KeyModifiers.Shift);

    /// <summary>
    /// Creates an Alt+Key shortcut.
    /// </summary>
    public static KeyboardShortcut Alt(Key key) =>
        new(key, KeyModifiers.Alt);

    /// <summary>
    /// Creates a key-only shortcut (no modifiers).
    /// </summary>
    public static KeyboardShortcut KeyOnly(Key key) =>
        new(key, KeyModifiers.None);

    #endregion
}
