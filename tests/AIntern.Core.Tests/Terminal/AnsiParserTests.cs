using AIntern.Core.Models.Terminal;
using AIntern.Core.Terminal;
using Xunit;

namespace AIntern.Core.Tests.Terminal;

/// <summary>
/// Unit tests for <see cref="AnsiParser"/>.
/// </summary>
public class AnsiParserTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidBuffer_CreatesParser()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act
        var parser = new AnsiParser(buffer);

        // Assert
        Assert.NotNull(parser);
        Assert.Equal(AnsiParserState.Ground, parser.State);
    }

    [Fact]
    public void Constructor_WithNullBuffer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AnsiParser(null!));
    }

    #endregion

    #region C0 Control Character Tests

    [Fact]
    public void Parse_LineFeed_MovesCursorDown()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorY = 0;

        // Act
        parser.Parse("\n");

        // Assert
        Assert.Equal(1, buffer.CursorY);
    }

    [Fact]
    public void Parse_CarriageReturn_MovesCursorToColumnZero()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorX = 10;

        // Act
        parser.Parse("\r");

        // Assert
        Assert.Equal(0, buffer.CursorX);
    }

    [Fact]
    public void Parse_Tab_MovesToNextTabStop()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorX = 0;

        // Act
        parser.Parse("\t");

        // Assert
        Assert.Equal(8, buffer.CursorX);
    }

    [Fact]
    public void Parse_Backspace_MovesCursorLeft()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorX = 5;

        // Act
        parser.Parse("\x08");

        // Assert
        Assert.Equal(4, buffer.CursorX);
    }

    [Fact]
    public void Parse_Bell_RaisesEvent()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        var bellReceived = false;
        parser.Bell += () => bellReceived = true;

        // Act
        parser.Parse("\x07");

        // Assert
        Assert.True(bellReceived);
    }

    #endregion

    #region Printable Text Tests

    [Fact]
    public void Parse_PrintableText_WritesToBuffer()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act
        parser.Parse("Hello");

        // Assert
        var line = buffer.GetLine(0);
        Assert.NotNull(line);
        Assert.Equal("Hello", line.GetText().TrimEnd());
    }

    [Fact]
    public void Parse_TextWithNewlines_MultipleLines()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act
        parser.Parse("Line1\r\nLine2");

        // Assert
        var line0 = buffer.GetLine(0);
        var line1 = buffer.GetLine(1);
        Assert.NotNull(line0);
        Assert.NotNull(line1);
        Assert.Equal("Line1", line0.GetText().TrimEnd());
        Assert.Equal("Line2", line1.GetText().TrimEnd());
    }

    #endregion

    #region CSI Cursor Movement Tests

    [Fact]
    public void Parse_CsiCursorUp_MovesCursorUp()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorY = 10;

        // Act - ESC[3A - Cursor Up 3
        parser.Parse("\x1b[3A");

        // Assert
        Assert.Equal(7, buffer.CursorY);
    }

    [Fact]
    public void Parse_CsiCursorDown_MovesCursorDown()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorY = 0;

        // Act - ESC[5B - Cursor Down 5
        parser.Parse("\x1b[5B");

        // Assert
        Assert.Equal(5, buffer.CursorY);
    }

    [Fact]
    public void Parse_CsiCursorForward_MovesCursorRight()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorX = 0;

        // Act - ESC[10C - Cursor Forward 10
        parser.Parse("\x1b[10C");

        // Assert
        Assert.Equal(10, buffer.CursorX);
    }

    [Fact]
    public void Parse_CsiCursorBack_MovesCursorLeft()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorX = 20;

        // Act - ESC[7D - Cursor Back 7
        parser.Parse("\x1b[7D");

        // Assert
        Assert.Equal(13, buffer.CursorX);
    }

    [Fact]
    public void Parse_CsiCursorPosition_SetsCursorPosition()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC[5;10H - Cursor Position row 5, col 10
        parser.Parse("\x1b[5;10H");

        // Assert (1-indexed input, 0-indexed internal)
        Assert.Equal(4, buffer.CursorY);
        Assert.Equal(9, buffer.CursorX);
    }

    [Fact]
    public void Parse_CsiCursorPositionDefaults_GoesToHome()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorX = 10;
        buffer.CursorY = 10;

        // Act - ESC[H - Cursor Position (defaults to 1,1)
        parser.Parse("\x1b[H");

        // Assert
        Assert.Equal(0, buffer.CursorY);
        Assert.Equal(0, buffer.CursorX);
    }

    #endregion

    #region CSI Screen Clearing Tests

    [Fact]
    public void Parse_CsiClearToEnd_ClearsFromCursorToEnd()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        parser.Parse("XXXXXXXXXX");
        buffer.CursorX = 5;
        buffer.CursorY = 0;

        // Act - ESC[0J - Clear from cursor to end
        parser.Parse("\x1b[0J");

        // Assert
        var line = buffer.GetLine(0);
        Assert.NotNull(line);
        var text = line.GetText().TrimEnd();
        Assert.Equal("XXXXX", text);
    }

    [Fact]
    public void Parse_CsiClearScreen_ClearsEntireScreen()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        parser.Parse("Some text here");

        // Act - ESC[2J - Clear entire screen
        parser.Parse("\x1b[2J");

        // Assert
        var line = buffer.GetLine(0);
        Assert.NotNull(line);
        Assert.True(string.IsNullOrWhiteSpace(line.GetText()));
    }

    [Fact]
    public void Parse_CsiClearLine_ClearsCurrentLine()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        parser.Parse("Line content");
        buffer.CursorY = 0;

        // Act - ESC[2K - Clear entire line
        parser.Parse("\x1b[2K");

        // Assert
        var line = buffer.GetLine(0);
        Assert.NotNull(line);
        Assert.True(string.IsNullOrWhiteSpace(line.GetText()));
    }

    #endregion

    #region SGR Color Tests

    [Fact]
    public void Parse_SgrReset_ResetsAttributes()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CurrentAttributes = buffer.CurrentAttributes.With(bold: true);

        // Act - ESC[0m - Reset
        parser.Parse("\x1b[0m");

        // Assert
        Assert.Equal(TerminalAttributes.Default, buffer.CurrentAttributes);
    }

    [Fact]
    public void Parse_SgrBold_SetsBoldAttribute()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC[1m - Bold
        parser.Parse("\x1b[1m");

        // Assert
        Assert.True(buffer.CurrentAttributes.Bold);
    }

    [Fact]
    public void Parse_SgrItalic_SetsItalicAttribute()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC[3m - Italic
        parser.Parse("\x1b[3m");

        // Assert
        Assert.True(buffer.CurrentAttributes.Italic);
    }

    [Fact]
    public void Parse_SgrUnderline_SetsUnderlineAttribute()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC[4m - Underline
        parser.Parse("\x1b[4m");

        // Assert
        Assert.True(buffer.CurrentAttributes.Underline);
    }

    [Fact]
    public void Parse_SgrForegroundRed_SetsRedForeground()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC[31m - Red foreground
        parser.Parse("\x1b[31m");

        // Assert
        Assert.Equal(TerminalColor.FromPalette(1), buffer.CurrentAttributes.Foreground);
    }

    [Fact]
    public void Parse_SgrBackgroundBlue_SetsBlueBackground()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC[44m - Blue background
        parser.Parse("\x1b[44m");

        // Assert
        Assert.Equal(TerminalColor.FromPalette(4), buffer.CurrentAttributes.Background);
    }

    [Fact]
    public void Parse_SgrMultiple_SetsMultipleAttributes()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC[1;31;44m - Bold + Red FG + Blue BG
        parser.Parse("\x1b[1;31;44m");

        // Assert
        Assert.True(buffer.CurrentAttributes.Bold);
        Assert.Equal(TerminalColor.FromPalette(1), buffer.CurrentAttributes.Foreground);
        Assert.Equal(TerminalColor.FromPalette(4), buffer.CurrentAttributes.Background);
    }

    [Fact]
    public void Parse_Sgr256Color_SetsPaletteColor()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC[38;5;196m - 256-color foreground (bright red)
        parser.Parse("\x1b[38;5;196m");

        // Assert
        Assert.Equal(TerminalColor.FromPalette(196), buffer.CurrentAttributes.Foreground);
    }

    [Fact]
    public void Parse_SgrTrueColor_SetsRgbColor()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC[38;2;255;128;64m - True color foreground
        parser.Parse("\x1b[38;2;255;128;64m");

        // Assert
        Assert.Equal(TerminalColor.FromRgb(255, 128, 64), buffer.CurrentAttributes.Foreground);
    }

    #endregion

    #region DEC Mode Tests

    [Fact]
    public void Parse_DecSetCursorVisible_ShowsCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorVisible = false;

        // Act - ESC[?25h - Show cursor
        parser.Parse("\x1b[?25h");

        // Assert
        Assert.True(buffer.CursorVisible);
    }

    [Fact]
    public void Parse_DecResetCursorVisible_HidesCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorVisible = true;

        // Act - ESC[?25l - Hide cursor
        parser.Parse("\x1b[?25l");

        // Assert
        Assert.False(buffer.CursorVisible);
    }

    [Fact]
    public void Parse_DecSetAutoWrap_EnablesAutoWrap()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.AutoWrapMode = false;

        // Act - ESC[?7h - Enable auto-wrap
        parser.Parse("\x1b[?7h");

        // Assert
        Assert.True(buffer.AutoWrapMode);
    }

    #endregion

    #region OSC Tests

    [Fact]
    public void Parse_OscTitle_RaisesTitleChangedEvent()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        string? receivedTitle = null;
        parser.TitleChanged += title => receivedTitle = title;

        // Act - ESC]0;My Terminal\x07
        parser.Parse("\x1b]0;My Terminal\x07");

        // Assert
        Assert.Equal("My Terminal", receivedTitle);
    }

    [Fact]
    public void Parse_OscWorkingDirectory_RaisesEvent()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        string? receivedPath = null;
        parser.WorkingDirectoryChanged += path => receivedPath = path;

        // Act - ESC]7;file:///home/user\x07
        parser.Parse("\x1b]7;file:///home/user\x07");

        // Assert
        Assert.Equal("/home/user", receivedPath);
    }

    [Fact]
    public void Parse_OscHyperlink_RaisesEvent()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        string? receivedUri = null;
        string? receivedParams = null;
        parser.HyperlinkDetected += (p, uri) =>
        {
            receivedParams = p;
            receivedUri = uri;
        };

        // Act - ESC]8;;https://example.com\x07
        parser.Parse("\x1b]8;;https://example.com\x07");

        // Assert
        Assert.Equal("https://example.com", receivedUri);
        Assert.Null(receivedParams);
    }

    #endregion

    #region Escape Sequence Tests

    [Fact]
    public void Parse_CsiSaveCursor_SavesCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorX = 15;
        buffer.CursorY = 10;

        // Act - ESC[s - Save cursor (SCO)
        parser.Parse("\x1b[s");

        // Assert
        Assert.Equal((15, 10), buffer.SavedCursor);
    }

    [Fact]
    public void Parse_CsiRestoreCursor_RestoresCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.SavedCursor = (20, 5);
        buffer.CursorX = 0;
        buffer.CursorY = 0;

        // Act - ESC[u - Restore cursor (SCO)
        parser.Parse("\x1b[u");

        // Assert
        Assert.Equal(20, buffer.CursorX);
        Assert.Equal(5, buffer.CursorY);
    }

    [Fact]
    public void Parse_CsiClearAll_ResetsScreen()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        parser.Parse("Some text");
        buffer.CursorX = 40;
        buffer.CursorY = 12;

        // Act - ESC[2J - Clear screen + ESC[H - Home cursor
        parser.Parse("\x1b[2J\x1b[H");

        // Assert
        Assert.Equal(0, buffer.CursorX);
        Assert.Equal(0, buffer.CursorY);
    }

    #endregion

    #region Cancel/Invalid Sequence Tests

    [Fact]
    public void Parse_CancelInEscape_ReturnsToGround()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC then CAN
        parser.Parse("\x1b");
        Assert.Equal(AnsiParserState.Escape, parser.State);
        parser.Parse("\x18");

        // Assert
        Assert.Equal(AnsiParserState.Ground, parser.State);
    }

    [Fact]
    public void Parse_MalformedCsi_HandledGracefully()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);

        // Act - ESC[ followed by invalid byte, then normal text
        parser.Parse("\x1b[\xffHello");

        // Assert - Should not crash and return to ground
        Assert.Equal(AnsiParserState.Ground, parser.State);
    }

    #endregion

    #region Line/Character Operations Tests

    [Fact]
    public void Parse_CsiInsertLines_CallsBufferMethod()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        buffer.CursorY = 5;
        var originalRows = buffer.Rows;

        // Act - ESC[1L - Insert 1 line (should not throw)
        parser.Parse("\x1b[1L");

        // Assert - Buffer should still be valid
        Assert.Equal(originalRows, buffer.Rows);
        Assert.Equal(5, buffer.CursorY); // Cursor position unchanged
    }

    [Fact]
    public void Parse_CsiDeleteLines_DeletesLines()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        parser.Parse("Line1\r\nLine2\r\nLine3");
        buffer.CursorY = 1;

        // Act - ESC[1M - Delete 1 line
        parser.Parse("\x1b[1M");

        // Assert - Line3 should move up
        var line1 = buffer.GetLine(1);
        Assert.NotNull(line1);
        Assert.Equal("Line3", line1.GetText().TrimEnd());
    }

    [Fact]
    public void Parse_CsiEraseChars_ErasesWithoutShifting()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var parser = new AnsiParser(buffer);
        parser.Parse("ABCDEFGH");
        buffer.CursorX = 2;
        buffer.CursorY = 0;

        // Act - ESC[3X - Erase 3 characters
        parser.Parse("\x1b[3X");

        // Assert - Characters erased but not shifted
        var line = buffer.GetLine(0);
        Assert.NotNull(line);
        var text = line.GetText();
        Assert.Equal("AB", text[..2]);
    }

    #endregion
}
