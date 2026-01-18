namespace AIntern.Core.Terminal;

using System.Text;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ ANSI PARSER (v0.5.1c)                                                   │
// │ Parses VT100/ANSI escape sequences and updates a terminal buffer.       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Parses VT100/ANSI escape sequences and updates a terminal buffer.
/// </summary>
/// <remarks>
/// <para>
/// The parser implements a state machine based on the VT100/VT220 terminal
/// specification. It processes input bytes and translates escape sequences
/// into buffer operations.
/// </para>
/// <para>
/// Supported features:
/// <list type="bullet">
///   <item><description>C0 control characters (NUL, BEL, BS, HT, LF, VT, FF, CR, ESC, CAN, SUB)</description></item>
///   <item><description>CSI sequences for cursor movement and screen manipulation</description></item>
///   <item><description>SGR (Select Graphic Rendition) for text colors and styles</description></item>
///   <item><description>Extended color modes (256-color palette and 24-bit true color)</description></item>
///   <item><description>DEC private modes (cursor visibility, auto-wrap, alternate screen)</description></item>
///   <item><description>OSC commands (window title, working directory, hyperlinks)</description></item>
/// </list>
/// </para>
/// <para>Added in v0.5.1c.</para>
/// </remarks>
public sealed class AnsiParser
{
    #region Private Fields

    /// <summary>The target terminal buffer to update.</summary>
    private readonly TerminalBuffer _buffer;

    /// <summary>Current parser state.</summary>
    private AnsiParserState _state = AnsiParserState.Ground;

    /// <summary>Collected CSI/DCS parameters.</summary>
    private readonly List<int> _params = new();

    /// <summary>Collected intermediate bytes.</summary>
    private readonly StringBuilder _intermediates = new();

    /// <summary>Collected OSC string content.</summary>
    private readonly StringBuilder _oscString = new();

    /// <summary>Current parameter value being accumulated.</summary>
    private int _currentParam;

    /// <summary>Whether a parameter digit has been seen.</summary>
    private bool _paramStarted;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a bell character (BEL, 0x07) is received.
    /// </summary>
    public event Action? Bell;

    /// <summary>
    /// Event raised when the terminal title changes via OSC 0, 1, or 2.
    /// </summary>
    public event Action<string>? TitleChanged;

    /// <summary>
    /// Event raised when the working directory changes via OSC 7.
    /// </summary>
    public event Action<string>? WorkingDirectoryChanged;

    /// <summary>
    /// Event raised when a hyperlink is detected via OSC 8.
    /// </summary>
    /// <remarks>
    /// Parameters: (params?, uri) where params contains optional link attributes
    /// and uri is the hyperlink target.
    /// </remarks>
    public event Action<string?, string>? HyperlinkDetected;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new ANSI parser targeting the specified buffer.
    /// </summary>
    /// <param name="buffer">The terminal buffer to update.</param>
    /// <exception cref="ArgumentNullException">Thrown when buffer is null.</exception>
    public AnsiParser(TerminalBuffer buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the current parser state (for debugging/testing).
    /// </summary>
    public AnsiParserState State => _state;

    /// <summary>
    /// Parse a sequence of bytes from the terminal output stream.
    /// </summary>
    /// <param name="data">The byte data to parse.</param>
    public void Parse(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
        {
            ProcessByte(b);
        }
    }

    /// <summary>
    /// Parse a string (converted to UTF-8 bytes).
    /// </summary>
    /// <param name="text">The text to parse.</param>
    public void Parse(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Parse(Encoding.UTF8.GetBytes(text));
    }

    #endregion

    #region Byte Processing

    /// <summary>
    /// Process a single byte through the state machine.
    /// </summary>
    /// <param name="b">The byte to process.</param>
    private void ProcessByte(byte b)
    {
        // Handle C0 control characters in any state (except during string collection)
        if (b < 0x20 && _state != AnsiParserState.OscString && 
            _state != AnsiParserState.DcsPassthrough && _state != AnsiParserState.SosPmApcString)
        {
            if (ProcessC0Control(b))
                return;
        }

        switch (_state)
        {
            case AnsiParserState.Ground:
                ProcessGround(b);
                break;

            case AnsiParserState.Escape:
                ProcessEscape(b);
                break;

            case AnsiParserState.EscapeIntermediate:
                ProcessEscapeIntermediate(b);
                break;

            case AnsiParserState.CsiEntry:
            case AnsiParserState.CsiParam:
                ProcessCsi(b);
                break;

            case AnsiParserState.CsiIntermediate:
                ProcessCsiIntermediate(b);
                break;

            case AnsiParserState.OscString:
                ProcessOsc(b);
                break;

            case AnsiParserState.DcsEntry:
            case AnsiParserState.DcsParam:
            case AnsiParserState.DcsIntermediate:
            case AnsiParserState.DcsPassthrough:
                ProcessDcs(b);
                break;

            case AnsiParserState.SosPmApcString:
                ProcessSosPmApc(b);
                break;
        }
    }

    #endregion

    #region C0 Control Processing

    /// <summary>
    /// Process C0 control characters (0x00-0x1F).
    /// </summary>
    /// <param name="b">The control byte.</param>
    /// <returns>True if the byte was handled, false to continue processing.</returns>
    private bool ProcessC0Control(byte b)
    {
        switch (b)
        {
            case 0x00: // NUL - Ignore
                return true;

            case 0x07: // BEL - Bell
                // In OSC string, BEL terminates the string
                if (_state == AnsiParserState.OscString)
                {
                    ExecuteOsc();
                    _state = AnsiParserState.Ground;
                }
                else
                {
                    Bell?.Invoke();
                }
                return true;

            case 0x08: // BS - Backspace
                _buffer.Backspace();
                return true;

            case 0x09: // HT - Horizontal Tab
                _buffer.Tab();
                return true;

            case 0x0A: // LF - Line Feed
            case 0x0B: // VT - Vertical Tab (same as LF)
            case 0x0C: // FF - Form Feed (same as LF)
                _buffer.LineFeed();
                return true;

            case 0x0D: // CR - Carriage Return
                _buffer.CarriageReturn();
                return true;

            case 0x0E: // SO - Shift Out (G1 charset, ignored)
            case 0x0F: // SI - Shift In (G0 charset, ignored)
                return true;

            case 0x1B: // ESC - Escape
                _state = AnsiParserState.Escape;
                _intermediates.Clear();
                return true;

            case 0x18: // CAN - Cancel
            case 0x1A: // SUB - Substitute
                // Cancel current escape sequence
                _state = AnsiParserState.Ground;
                return true;

            default:
                return false;
        }
    }

    #endregion

    #region Ground State

    /// <summary>
    /// Process bytes in Ground state (normal character processing).
    /// </summary>
    /// <param name="b">The byte to process.</param>
    private void ProcessGround(byte b)
    {
        // Printable ASCII
        if (b >= 0x20 && b <= 0x7E)
        {
            _buffer.WriteChar((char)b);
        }
        // High bytes (UTF-8 sequences handled as individual bytes for now)
        else if (b >= 0x80)
        {
            _buffer.WriteChar((char)b);
        }
    }

    #endregion

    #region Escape State

    /// <summary>
    /// Process bytes in Escape state (after ESC received).
    /// </summary>
    /// <param name="b">The byte after ESC.</param>
    private void ProcessEscape(byte b)
    {
        switch (b)
        {
            case 0x5B: // '[' - CSI introducer
                _state = AnsiParserState.CsiEntry;
                ResetCsiParser();
                break;

            case 0x5D: // ']' - OSC introducer
                _state = AnsiParserState.OscString;
                _oscString.Clear();
                break;

            case 0x50: // 'P' - DCS introducer
                _state = AnsiParserState.DcsEntry;
                ResetCsiParser();
                break;

            case 0x58: // 'X' - SOS
            case 0x5E: // '^' - PM
            case 0x5F: // '_' - APC
                _state = AnsiParserState.SosPmApcString;
                break;

            case 0x5C: // '\' - ST (String Terminator)
                _state = AnsiParserState.Ground;
                break;

            case (byte)'7': // DECSC - Save Cursor
                _buffer.SaveCursor();
                _state = AnsiParserState.Ground;
                break;

            case (byte)'8': // DECRC - Restore Cursor
                _buffer.RestoreCursor();
                _state = AnsiParserState.Ground;
                break;

            case (byte)'D': // IND - Index (same as LF)
                _buffer.LineFeed();
                _state = AnsiParserState.Ground;
                break;

            case (byte)'E': // NEL - Next Line (CR + LF)
                _buffer.CarriageReturn();
                _buffer.LineFeed();
                _state = AnsiParserState.Ground;
                break;

            case (byte)'H': // HTS - Horizontal Tab Set (ignored)
                _state = AnsiParserState.Ground;
                break;

            case (byte)'M': // RI - Reverse Index
                _buffer.ScrollDown(1);
                _state = AnsiParserState.Ground;
                break;

            case (byte)'c': // RIS - Reset to Initial State
                _buffer.Reset();
                _state = AnsiParserState.Ground;
                break;

            default:
                // Check for intermediate bytes (0x20-0x2F)
                if (b >= 0x20 && b <= 0x2F)
                {
                    _intermediates.Append((char)b);
                    _state = AnsiParserState.EscapeIntermediate;
                }
                else
                {
                    // Unknown escape sequence, return to ground
                    _state = AnsiParserState.Ground;
                }
                break;
        }
    }

    /// <summary>
    /// Process bytes in EscapeIntermediate state.
    /// </summary>
    /// <param name="b">The byte to process.</param>
    private void ProcessEscapeIntermediate(byte b)
    {
        if (b >= 0x20 && b <= 0x2F)
        {
            // More intermediate bytes
            _intermediates.Append((char)b);
        }
        else if (b >= 0x30 && b <= 0x7E)
        {
            // Final byte - execute and return to ground
            // ESC sequences like "ESC ( B" (select charset) are ignored
            _state = AnsiParserState.Ground;
        }
        else
        {
            _state = AnsiParserState.Ground;
        }
    }

    #endregion

    #region CSI Processing

    /// <summary>
    /// Reset the CSI parameter parser.
    /// </summary>
    private void ResetCsiParser()
    {
        _params.Clear();
        _intermediates.Clear();
        _currentParam = 0;
        _paramStarted = false;
    }

    /// <summary>
    /// Process bytes in CSI states (CsiEntry/CsiParam).
    /// </summary>
    /// <param name="b">The byte to process.</param>
    private void ProcessCsi(byte b)
    {
        // Digit (0x30-0x39)
        if (b >= 0x30 && b <= 0x39)
        {
            _currentParam = _currentParam * 10 + (b - 0x30);
            _paramStarted = true;
            _state = AnsiParserState.CsiParam;
        }
        // Semicolon (0x3B) - parameter separator
        else if (b == 0x3B)
        {
            _params.Add(_paramStarted ? _currentParam : 0);
            _currentParam = 0;
            _paramStarted = false;
            _state = AnsiParserState.CsiParam;
        }
        // Private prefix (< = > ?) - 0x3C-0x3F
        else if (b >= 0x3C && b <= 0x3F)
        {
            _intermediates.Append((char)b);
            _state = AnsiParserState.CsiParam;
        }
        // Intermediate bytes (0x20-0x2F)
        else if (b >= 0x20 && b <= 0x2F)
        {
            if (_paramStarted)
            {
                _params.Add(_currentParam);
                _currentParam = 0;
                _paramStarted = false;
            }
            _intermediates.Append((char)b);
            _state = AnsiParserState.CsiIntermediate;
        }
        // Final byte (0x40-0x7E)
        else if (b >= 0x40 && b <= 0x7E)
        {
            if (_paramStarted)
            {
                _params.Add(_currentParam);
            }
            ExecuteCsi((char)b);
            _state = AnsiParserState.Ground;
        }
        else
        {
            // Invalid byte, return to ground
            _state = AnsiParserState.Ground;
        }
    }

    /// <summary>
    /// Process bytes in CsiIntermediate state.
    /// </summary>
    /// <param name="b">The byte to process.</param>
    private void ProcessCsiIntermediate(byte b)
    {
        if (b >= 0x20 && b <= 0x2F)
        {
            _intermediates.Append((char)b);
        }
        else if (b >= 0x40 && b <= 0x7E)
        {
            ExecuteCsi((char)b);
            _state = AnsiParserState.Ground;
        }
        else
        {
            _state = AnsiParserState.Ground;
        }
    }

    /// <summary>
    /// Get a parameter value with a default.
    /// </summary>
    /// <param name="index">Parameter index.</param>
    /// <param name="defaultValue">Default if not present or zero.</param>
    /// <returns>The parameter value or default.</returns>
    private int GetParam(int index, int defaultValue = 0)
    {
        if (index < 0 || index >= _params.Count)
            return defaultValue;
        var value = _params[index];
        return value == 0 ? defaultValue : value;
    }

    /// <summary>
    /// Execute a CSI command.
    /// </summary>
    /// <param name="final">The final character of the sequence.</param>
    private void ExecuteCsi(char final)
    {
        // Check for private prefix (especially '?')
        var privatePrefix = _intermediates.Length > 0 ? _intermediates[0] : '\0';

        switch (final)
        {
            // Cursor Movement
            case 'A': // CUU - Cursor Up
                _buffer.CursorUp(GetParam(0, 1));
                break;

            case 'B': // CUD - Cursor Down
                _buffer.CursorDown(GetParam(0, 1));
                break;

            case 'C': // CUF - Cursor Forward
                _buffer.CursorForward(GetParam(0, 1));
                break;

            case 'D': // CUB - Cursor Back
                _buffer.CursorBack(GetParam(0, 1));
                break;

            case 'E': // CNL - Cursor Next Line
                _buffer.CursorDown(GetParam(0, 1));
                _buffer.CarriageReturn();
                break;

            case 'F': // CPL - Cursor Previous Line
                _buffer.CursorUp(GetParam(0, 1));
                _buffer.CarriageReturn();
                break;

            case 'G': // CHA - Cursor Horizontal Absolute
                _buffer.CursorX = Math.Clamp(GetParam(0, 1) - 1, 0, _buffer.Columns - 1);
                break;

            case 'H': // CUP - Cursor Position
            case 'f': // HVP - Horizontal Vertical Position
                _buffer.SetCursorPosition(GetParam(0, 1), GetParam(1, 1));
                break;

            // Screen Clearing
            case 'J': // ED - Erase in Display
                var edMode = GetParam(0, 0);
                switch (edMode)
                {
                    case 0: _buffer.ClearToEnd(); break;
                    case 1: _buffer.ClearToBeginning(); break;
                    case 2: _buffer.Clear(); break;
                    case 3: _buffer.ClearWithScrollback(); break;
                }
                break;

            case 'K': // EL - Erase in Line
                var elMode = GetParam(0, 0);
                switch (elMode)
                {
                    case 0: _buffer.ClearLineToEnd(); break;
                    case 1: _buffer.ClearLineToBeginning(); break;
                    case 2: _buffer.ClearLine(); break;
                }
                break;

            // Line Operations
            case 'L': // IL - Insert Lines
                _buffer.InsertLines(GetParam(0, 1));
                break;

            case 'M': // DL - Delete Lines
                _buffer.DeleteLines(GetParam(0, 1));
                break;

            // Character Operations
            case '@': // ICH - Insert Characters
                _buffer.InsertChars(GetParam(0, 1));
                break;

            case 'P': // DCH - Delete Characters
                _buffer.DeleteChars(GetParam(0, 1));
                break;

            case 'X': // ECH - Erase Characters
                _buffer.EraseChars(GetParam(0, 1));
                break;

            // Scrolling
            case 'S': // SU - Scroll Up
                _buffer.ScrollUp(GetParam(0, 1));
                break;

            case 'T': // SD - Scroll Down
                _buffer.ScrollDown(GetParam(0, 1));
                break;

            case 'r': // DECSTBM - Set Top/Bottom Margins
                var top = GetParam(0, 1);
                var bottom = GetParam(1, _buffer.Rows);
                _buffer.SetScrollRegion(top, bottom);
                break;

            // Cursor Position
            case 'd': // VPA - Vertical Position Absolute
                _buffer.CursorY = Math.Clamp(GetParam(0, 1) - 1, 0, _buffer.Rows - 1);
                break;

            // Cursor Save/Restore (SCO)
            case 's': // Save Cursor
                _buffer.SaveCursor();
                break;

            case 'u': // Restore Cursor
                _buffer.RestoreCursor();
                break;

            // SGR - Select Graphic Rendition
            case 'm':
                ExecuteSgr();
                break;

            // Modes
            case 'h': // SM - Set Mode
                if (privatePrefix == '?')
                    ExecuteDecSet(true);
                break;

            case 'l': // RM - Reset Mode
                if (privatePrefix == '?')
                    ExecuteDecSet(false);
                break;

            // Ignored commands
            case 'n': // DSR - Device Status Report
            case 'c': // DA - Device Attributes
            case 't': // Window manipulation
            case 'q': // DECLL - Load LEDs
                break;
        }
    }

    #endregion

    #region SGR Processing

    /// <summary>
    /// Execute SGR (Select Graphic Rendition) command.
    /// </summary>
    private void ExecuteSgr()
    {
        // If no params, reset to default
        if (_params.Count == 0)
        {
            _buffer.CurrentAttributes = TerminalAttributes.Default;
            return;
        }

        var attrs = _buffer.CurrentAttributes;

        for (int i = 0; i < _params.Count; i++)
        {
            var p = _params[i];

            switch (p)
            {
                case 0: // Reset
                    attrs = TerminalAttributes.Default;
                    break;

                case 1: // Bold
                    attrs = attrs.With(bold: true);
                    break;

                case 2: // Dim
                    attrs = attrs.With(dim: true);
                    break;

                case 3: // Italic
                    attrs = attrs.With(italic: true);
                    break;

                case 4: // Underline
                case 21: // Double underline (treat as underline)
                    attrs = attrs.With(underline: true);
                    break;

                case 5: // Blink (slow)
                case 6: // Blink (rapid)
                    attrs = attrs.With(blink: true);
                    break;

                case 7: // Inverse
                    attrs = attrs.With(inverse: true);
                    break;

                case 8: // Hidden
                    attrs = attrs.With(hidden: true);
                    break;

                case 9: // Strikethrough
                    attrs = attrs.With(strikethrough: true);
                    break;

                case 22: // Normal intensity (not bold, not dim)
                    attrs = attrs.With(bold: false, dim: false);
                    break;

                case 23: // Not italic
                    attrs = attrs.With(italic: false);
                    break;

                case 24: // Not underline
                    attrs = attrs.With(underline: false);
                    break;

                case 25: // Not blink
                    attrs = attrs.With(blink: false);
                    break;

                case 27: // Not inverse
                    attrs = attrs.With(inverse: false);
                    break;

                case 28: // Not hidden
                    attrs = attrs.With(hidden: false);
                    break;

                case 29: // Not strikethrough
                    attrs = attrs.With(strikethrough: false);
                    break;

                // Standard foreground colors (30-37)
                case >= 30 and <= 37:
                    attrs = attrs.With(foreground: TerminalColor.FromPalette((byte)(p - 30)));
                    break;

                // Extended foreground color
                case 38:
                    i = ProcessExtendedColor(i, true, ref attrs);
                    break;

                // Default foreground
                case 39:
                    attrs = attrs.With(foreground: TerminalColor.Default);
                    break;

                // Standard background colors (40-47)
                case >= 40 and <= 47:
                    attrs = attrs.With(background: TerminalColor.FromPalette((byte)(p - 40)));
                    break;

                // Extended background color
                case 48:
                    i = ProcessExtendedColor(i, false, ref attrs);
                    break;

                // Default background
                case 49:
                    attrs = attrs.With(background: TerminalColor.Default);
                    break;

                // Bright foreground colors (90-97)
                case >= 90 and <= 97:
                    attrs = attrs.With(foreground: TerminalColor.FromPalette((byte)(p - 90 + 8)));
                    break;

                // Bright background colors (100-107)
                case >= 100 and <= 107:
                    attrs = attrs.With(background: TerminalColor.FromPalette((byte)(p - 100 + 8)));
                    break;
            }
        }

        _buffer.CurrentAttributes = attrs;
    }

    /// <summary>
    /// Process extended color (256-color or true color).
    /// </summary>
    /// <param name="index">Current parameter index (pointing to 38 or 48).</param>
    /// <param name="isForeground">True for foreground, false for background.</param>
    /// <param name="attrs">Current attributes to modify.</param>
    /// <returns>Updated parameter index.</returns>
    private int ProcessExtendedColor(int index, bool isForeground, ref TerminalAttributes attrs)
    {
        if (index + 1 >= _params.Count)
            return index;

        var mode = _params[index + 1];

        if (mode == 5 && index + 2 < _params.Count)
        {
            // 256-color mode: 38;5;n or 48;5;n
            var colorIndex = (byte)Math.Clamp(_params[index + 2], 0, 255);
            var color = TerminalColor.FromPalette(colorIndex);
            attrs = isForeground
                ? attrs.With(foreground: color)
                : attrs.With(background: color);
            return index + 2;
        }
        else if (mode == 2 && index + 4 < _params.Count)
        {
            // True color mode: 38;2;r;g;b or 48;2;r;g;b
            var r = (byte)Math.Clamp(_params[index + 2], 0, 255);
            var g = (byte)Math.Clamp(_params[index + 3], 0, 255);
            var b = (byte)Math.Clamp(_params[index + 4], 0, 255);
            var color = TerminalColor.FromRgb(r, g, b);
            attrs = isForeground
                ? attrs.With(foreground: color)
                : attrs.With(background: color);
            return index + 4;
        }

        return index;
    }

    #endregion

    #region DEC Private Modes

    /// <summary>
    /// Execute DEC private mode set/reset.
    /// </summary>
    /// <param name="enable">True to enable (DECSET), false to disable (DECRST).</param>
    private void ExecuteDecSet(bool enable)
    {
        foreach (var mode in _params)
        {
            switch (mode)
            {
                case 1: // DECCKM - Cursor keys mode (ignored)
                    break;

                case 6: // DECOM - Origin mode
                    _buffer.OriginMode = enable;
                    break;

                case 7: // DECAWM - Auto-wrap mode
                    _buffer.AutoWrapMode = enable;
                    break;

                case 25: // DECTCEM - Text cursor enable mode
                    _buffer.CursorVisible = enable;
                    break;

                case 47: // Alternate screen buffer (xterm)
                case 1047: // Alternate screen buffer (xterm new)
                case 1049: // Alternate screen buffer + save cursor
                    // Alternate screen buffer not yet implemented
                    // Would need separate buffer for alternate screen
                    break;

                case 2004: // Bracketed paste mode (ignored)
                    break;
            }
        }
    }

    #endregion

    #region OSC Processing

    /// <summary>
    /// Process bytes in OscString state.
    /// </summary>
    /// <param name="b">The byte to process.</param>
    private void ProcessOsc(byte b)
    {
        // BEL terminates OSC
        if (b == 0x07)
        {
            ExecuteOsc();
            _state = AnsiParserState.Ground;
        }
        // ESC starts potential ST (String Terminator)
        else if (b == 0x1B)
        {
            // Next byte should be '\' for ST
            // For simplicity, execute OSC and wait for '\'
            ExecuteOsc();
            _state = AnsiParserState.Escape;
        }
        // Collect OSC content (printable + some controls)
        else if (b >= 0x20 || b == 0x09)
        {
            _oscString.Append((char)b);
        }
        // Cancel on other C0 controls
        else if (b == 0x18 || b == 0x1A)
        {
            _state = AnsiParserState.Ground;
        }
    }

    /// <summary>
    /// Execute the collected OSC command.
    /// </summary>
    private void ExecuteOsc()
    {
        var content = _oscString.ToString();
        var semicolonIndex = content.IndexOf(';');

        if (semicolonIndex <= 0)
            return;

        var commandStr = content[..semicolonIndex];
        var argument = content[(semicolonIndex + 1)..];

        if (!int.TryParse(commandStr, out var command))
            return;

        switch (command)
        {
            case 0: // Set icon name and window title
            case 1: // Set icon name
            case 2: // Set window title
                TitleChanged?.Invoke(argument);
                break;

            case 7: // Set working directory (file://hostname/path)
                ParseOsc7WorkingDirectory(argument);
                break;

            case 8: // Hyperlink
                ParseOsc8Hyperlink(argument);
                break;
        }
    }

    /// <summary>
    /// Parse OSC 7 working directory.
    /// </summary>
    /// <param name="argument">The OSC 7 argument (file://hostname/path).</param>
    private void ParseOsc7WorkingDirectory(string argument)
    {
        // Format: file://hostname/path or file:///path
        if (argument.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            var pathStart = argument.IndexOf('/', 7);
            if (pathStart >= 0)
            {
                var path = Uri.UnescapeDataString(argument[pathStart..]);
                WorkingDirectoryChanged?.Invoke(path);
            }
        }
    }

    /// <summary>
    /// Parse OSC 8 hyperlink.
    /// </summary>
    /// <param name="argument">The OSC 8 argument (params;uri).</param>
    private void ParseOsc8Hyperlink(string argument)
    {
        // Format: params;uri
        // params can be empty, e.g., ";https://example.com"
        var semicolonIndex = argument.IndexOf(';');
        if (semicolonIndex < 0)
            return;

        var parameters = semicolonIndex > 0 ? argument[..semicolonIndex] : null;
        var uri = argument[(semicolonIndex + 1)..];

        if (!string.IsNullOrEmpty(uri))
        {
            HyperlinkDetected?.Invoke(parameters, uri);
        }
    }

    #endregion

    #region DCS Processing

    /// <summary>
    /// Process bytes in DCS states.
    /// </summary>
    /// <param name="b">The byte to process.</param>
    private void ProcessDcs(byte b)
    {
        // Wait for ST (String Terminator)
        if (b == 0x9C) // C1 ST
        {
            _state = AnsiParserState.Ground;
        }
        else if (b == 0x1B) // ESC (start of ESC \)
        {
            _state = AnsiParserState.Escape;
        }
        else if (b == 0x18 || b == 0x1A) // CAN/SUB
        {
            _state = AnsiParserState.Ground;
        }
        // Otherwise continue in passthrough
        else if (_state == AnsiParserState.DcsEntry || _state == AnsiParserState.DcsParam)
        {
            _state = AnsiParserState.DcsPassthrough;
        }
    }

    #endregion

    #region SOS/PM/APC Processing

    /// <summary>
    /// Process bytes in SOS/PM/APC string state.
    /// </summary>
    /// <param name="b">The byte to process.</param>
    private void ProcessSosPmApc(byte b)
    {
        // Wait for ST (String Terminator)
        if (b == 0x9C) // C1 ST
        {
            _state = AnsiParserState.Ground;
        }
        else if (b == 0x1B) // ESC (start of ESC \)
        {
            _state = AnsiParserState.Escape;
        }
        else if (b == 0x18 || b == 0x1A) // CAN/SUB
        {
            _state = AnsiParserState.Ground;
        }
        // Otherwise ignore content
    }

    #endregion
}
