namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TERMINAL BELL STYLE (v0.5.3c)                                           │
// │ Defines bell/notification styles for the terminal.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Terminal bell notification styles.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3c.</para>
/// <para>
/// Controls how the terminal responds to the ASCII BEL character (0x07).
/// Users can customize notification behavior per-profile.
/// </para>
/// </remarks>
public enum TerminalBellStyle
{
    /// <summary>
    /// Play system audible bell sound.
    /// </summary>
    /// <remarks>
    /// Uses the operating system's default alert sound.
    /// </remarks>
    Audible,

    /// <summary>
    /// Flash the terminal window or tab (visual bell).
    /// </summary>
    /// <remarks>
    /// A brief visual flash instead of sound.
    /// Useful in quiet environments.
    /// </remarks>
    Visual,

    /// <summary>
    /// Both audible and visual notification.
    /// </summary>
    /// <remarks>
    /// Combines sound and visual flash for maximum noticeability.
    /// </remarks>
    Both,

    /// <summary>
    /// Bell is disabled, no notification.
    /// </summary>
    /// <remarks>
    /// Silently ignores bell characters.
    /// </remarks>
    None
}
