namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TERMINAL CURSOR STYLE (v0.5.3c)                                         │
// │ Defines cursor display styles for the terminal.                         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Terminal cursor display styles.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3c.</para>
/// <para>
/// Defines the visual appearance of the cursor in terminal sessions.
/// Users can customize this per-profile or use the application default.
/// </para>
/// </remarks>
public enum TerminalCursorStyle
{
    /// <summary>
    /// Solid block cursor (classic terminal style).
    /// </summary>
    /// <remarks>
    /// The cursor fills the entire character cell.
    /// </remarks>
    Block,

    /// <summary>
    /// Horizontal underline cursor.
    /// </summary>
    /// <remarks>
    /// A thin line at the bottom of the character cell.
    /// </remarks>
    Underline,

    /// <summary>
    /// Vertical bar cursor (I-beam style).
    /// </summary>
    /// <remarks>
    /// A thin line at the left edge of the character cell.
    /// Similar to text editor cursors.
    /// </remarks>
    Bar
}
