namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL PROFILE DEFAULTS (v0.5.3c)                                        │
// │ Default values for shell profile settings.                              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Default/effective values for shell profile settings.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3c.</para>
/// <para>
/// Used when a profile's nullable setting is unset (null).
/// Provides application-wide defaults that can be overridden per-profile.
/// </para>
/// </remarks>
public sealed class ShellProfileDefaults
{
    // ─────────────────────────────────────────────────────────────────────
    // Font Settings
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Default terminal font family.
    /// </summary>
    /// <remarks>
    /// Falls back through multiple fonts: Cascadia Mono → Consolas → monospace.
    /// </remarks>
    public string FontFamily { get; set; } = "Cascadia Mono, Consolas, monospace";

    /// <summary>
    /// Default terminal font size in points.
    /// </summary>
    public double FontSize { get; set; } = 14;

    // ─────────────────────────────────────────────────────────────────────
    // Appearance
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Default terminal color theme name.
    /// </summary>
    public string ThemeName { get; set; } = "Dark";

    /// <summary>
    /// Default cursor style.
    /// </summary>
    public TerminalCursorStyle CursorStyle { get; set; } = TerminalCursorStyle.Block;

    /// <summary>
    /// Default cursor blink setting.
    /// </summary>
    public bool CursorBlink { get; set; } = true;

    // ─────────────────────────────────────────────────────────────────────
    // Buffer Settings
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Default number of scrollback lines to retain.
    /// </summary>
    /// <remarks>
    /// 10,000 lines provides good history without excessive memory usage.
    /// </remarks>
    public int ScrollbackLines { get; set; } = 10000;

    // ─────────────────────────────────────────────────────────────────────
    // Behavior
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Default bell notification style.
    /// </summary>
    public TerminalBellStyle BellStyle { get; set; } = TerminalBellStyle.Audible;

    /// <summary>
    /// Default close-on-exit behavior.
    /// </summary>
    /// <remarks>
    /// OnCleanExit keeps tab open on errors for debugging.
    /// </remarks>
    public ProfileCloseOnExit CloseOnExit { get; set; } = ProfileCloseOnExit.OnCleanExit;
}
