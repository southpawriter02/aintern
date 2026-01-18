namespace AIntern.Core.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ ANSI PARSER STATE (v0.5.1c)                                             │
// │ State machine states for ANSI escape sequence parsing.                  │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// State machine states for ANSI escape sequence parsing.
/// </summary>
/// <remarks>
/// <para>
/// The parser operates as a state machine, transitioning between states based on
/// input bytes. The <see cref="Ground"/> state is the default for normal character
/// processing. Escape sequences begin with ESC (0x1B) and transition through
/// various states based on the sequence type.
/// </para>
/// <para>
/// State transitions:
/// <list type="bullet">
/// <item>ESC → <see cref="Escape"/></item>
/// <item>ESC [ → <see cref="CsiEntry"/> (CSI)</item>
/// <item>ESC ] → <see cref="OscString"/> (OSC)</item>
/// <item>ESC P → <see cref="DcsEntry"/> (DCS)</item>
/// <item>ESC X/^/_ → <see cref="SosPmApcString"/></item>
/// <item>CAN/SUB → <see cref="Ground"/> (cancel)</item>
/// </list>
/// </para>
/// <para>Added in v0.5.1c.</para>
/// </remarks>
public enum AnsiParserState
{
    /// <summary>
    /// Normal character processing state.
    /// </summary>
    /// <remarks>
    /// Printable characters are written to the buffer.
    /// C0 control codes are handled directly.
    /// ESC initiates escape sequence processing.
    /// </remarks>
    Ground,

    /// <summary>
    /// Received ESC (0x1B), waiting for next byte to determine sequence type.
    /// </summary>
    /// <remarks>
    /// The parser transitions here upon receiving ESC and examines the next
    /// byte to determine the escape sequence type (CSI, OSC, DCS, etc.).
    /// </remarks>
    Escape,

    /// <summary>
    /// ESC followed by one or more intermediate bytes (0x20-0x2F).
    /// </summary>
    /// <remarks>
    /// Used for escape sequences like ESC ( B (select ASCII charset).
    /// Intermediate bytes are collected until a final byte is received.
    /// </remarks>
    EscapeIntermediate,

    /// <summary>
    /// Control Sequence Introducer entry (ESC [).
    /// </summary>
    /// <remarks>
    /// CSI sequences control cursor movement, screen clearing, styling, etc.
    /// This state handles the initial entry before parameters are collected.
    /// </remarks>
    CsiEntry,

    /// <summary>
    /// Collecting CSI parameters (digits and semicolons).
    /// </summary>
    /// <remarks>
    /// Parameters are numeric values separated by semicolons.
    /// Example: in "ESC[1;2;3m", the parameters are 1, 2, 3.
    /// </remarks>
    CsiParam,

    /// <summary>
    /// CSI intermediate bytes (0x20-0x2F between params and final).
    /// </summary>
    /// <remarks>
    /// Some CSI sequences include intermediate bytes between parameters
    /// and the final byte. These are collected for later processing.
    /// </remarks>
    CsiIntermediate,

    /// <summary>
    /// Operating System Command string collection (ESC ]).
    /// </summary>
    /// <remarks>
    /// OSC sequences set terminal properties like window title (OSC 0/1/2),
    /// working directory (OSC 7), and hyperlinks (OSC 8).
    /// Terminated by BEL (0x07) or ST (ESC \).
    /// </remarks>
    OscString,

    /// <summary>
    /// Device Control String entry (ESC P).
    /// </summary>
    /// <remarks>
    /// DCS sequences are used for device-specific control operations.
    /// Most are passed through or ignored in terminal emulation.
    /// </remarks>
    DcsEntry,

    /// <summary>
    /// DCS collecting parameters.
    /// </summary>
    DcsParam,

    /// <summary>
    /// DCS intermediate bytes.
    /// </summary>
    DcsIntermediate,

    /// <summary>
    /// DCS passthrough mode - collecting data until ST.
    /// </summary>
    DcsPassthrough,

    /// <summary>
    /// SOS (Start of String), PM (Privacy Message), or APC (Application Program Command).
    /// </summary>
    /// <remarks>
    /// These are rarely used escape sequences that collect string data
    /// until terminated by ST (String Terminator). Content is typically ignored.
    /// </remarks>
    SosPmApcString
}
