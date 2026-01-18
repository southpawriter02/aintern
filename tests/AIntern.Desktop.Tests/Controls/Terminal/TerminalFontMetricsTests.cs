namespace AIntern.Desktop.Tests.Controls.Terminal;

using AIntern.Desktop.Controls.Terminal;
using Xunit;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalFontMetricsTests (v0.5.2b)                                       │
// │ Unit tests for TerminalFontMetrics font handling and coordinate math.   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="TerminalFontMetrics"/>.
/// </summary>
/// <remarks>
/// Tests cover:
/// <list type="bullet">
///   <item><description>Font loading and fallback behavior</description></item>
///   <item><description>Metrics calculation (CharWidth, LineHeight, Baseline)</description></item>
///   <item><description>Terminal size calculation from pixel dimensions</description></item>
///   <item><description>Pixel-to-cell and cell-to-pixel coordinate conversion</description></item>
///   <item><description>Edge cases and defaults</description></item>
/// </list>
/// </remarks>
public sealed class TerminalFontMetricsTests : IDisposable
{
    #region Test Fixture

    private readonly TerminalFontMetrics _metrics = new();

    public void Dispose()
    {
        _metrics.Dispose();
    }

    #endregion

    #region Initial State Tests

    /// <summary>
    /// Verifies that a new TerminalFontMetrics is not valid until Update is called.
    /// </summary>
    [Fact]
    public void InitialState_IsValid_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_metrics.IsValid);
    }

    /// <summary>
    /// Verifies that initial CharWidth is zero.
    /// </summary>
    [Fact]
    public void InitialState_CharWidth_IsZero()
    {
        // Act & Assert
        Assert.Equal(0, _metrics.CharWidth);
    }

    /// <summary>
    /// Verifies that initial LineHeight is zero.
    /// </summary>
    [Fact]
    public void InitialState_LineHeight_IsZero()
    {
        // Act & Assert
        Assert.Equal(0, _metrics.LineHeight);
    }

    #endregion

    #region Update Method Tests

    /// <summary>
    /// Verifies that Update with a valid font makes IsValid true.
    /// </summary>
    [Fact]
    public void Update_WithValidFont_SetsIsValidToTrue()
    {
        // Act
        _metrics.Update("Courier New", 14f);

        // Assert
        Assert.True(_metrics.IsValid);
    }

    /// <summary>
    /// Verifies that FontSize is set correctly after Update.
    /// </summary>
    [Fact]
    public void Update_SetsFontSize()
    {
        // Arrange
        const float expectedSize = 16f;

        // Act
        _metrics.Update("Courier New", expectedSize);

        // Assert
        Assert.Equal(expectedSize, _metrics.FontSize);
    }

    /// <summary>
    /// Verifies that CharWidth is positive after Update.
    /// </summary>
    [Fact]
    public void Update_SetsPositiveCharWidth()
    {
        // Act
        _metrics.Update("Courier New", 14f);

        // Assert
        Assert.True(_metrics.CharWidth > 0, "CharWidth should be positive");
    }

    /// <summary>
    /// Verifies that LineHeight is positive after Update.
    /// </summary>
    [Fact]
    public void Update_SetsPositiveLineHeight()
    {
        // Act
        _metrics.Update("Courier New", 14f);

        // Assert
        Assert.True(_metrics.LineHeight > 0, "LineHeight should be positive");
    }

    /// <summary>
    /// Verifies that Baseline is positive after Update.
    /// </summary>
    [Fact]
    public void Update_SetsPositiveBaseline()
    {
        // Act
        _metrics.Update("Courier New", 14f);

        // Assert
        Assert.True(_metrics.Baseline > 0, "Baseline should be positive");
    }

    /// <summary>
    /// Verifies that Ascent is positive after Update.
    /// </summary>
    [Fact]
    public void Update_SetsPositiveAscent()
    {
        // Act
        _metrics.Update("Courier New", 14f);

        // Assert
        Assert.True(_metrics.Ascent > 0, "Ascent should be positive");
    }

    /// <summary>
    /// Verifies that Descent is positive after Update.
    /// </summary>
    [Fact]
    public void Update_SetsPositiveDescent()
    {
        // Act
        _metrics.Update("Courier New", 14f);

        // Assert
        Assert.True(_metrics.Descent >= 0, "Descent should be non-negative");
    }

    /// <summary>
    /// Verifies that a non-existent font falls back to system font and still works.
    /// </summary>
    [Fact]
    public void Update_WithNonExistentFont_FallsBackAndStillWorks()
    {
        // Act
        _metrics.Update("NonExistentFontThatDoesNotExist12345", 14f);

        // Assert
        Assert.True(_metrics.IsValid);
        Assert.True(_metrics.CharWidth > 0);
    }

    /// <summary>
    /// Verifies that calling Update multiple times updates the metrics.
    /// </summary>
    [Fact]
    public void Update_CalledMultipleTimes_UpdatesMetrics()
    {
        // Act
        _metrics.Update("Courier New", 12f);
        var firstCharWidth = _metrics.CharWidth;

        _metrics.Update("Courier New", 24f);
        var secondCharWidth = _metrics.CharWidth;

        // Assert
        Assert.NotEqual(firstCharWidth, secondCharWidth);
        Assert.True(secondCharWidth > firstCharWidth, "Larger font should have larger CharWidth");
    }

    #endregion

    #region CalculateTerminalSize Tests

    /// <summary>
    /// Verifies that CalculateTerminalSize returns default (80, 24) when metrics are invalid.
    /// </summary>
    [Fact]
    public void CalculateTerminalSize_WhenInvalid_ReturnsDefault()
    {
        // Act (metrics not initialized)
        var (cols, rows) = _metrics.CalculateTerminalSize(800, 600);

        // Assert
        Assert.Equal(80, cols);
        Assert.Equal(24, rows);
    }

    /// <summary>
    /// Verifies that CalculateTerminalSize returns valid dimensions when metrics are valid.
    /// </summary>
    [Fact]
    public void CalculateTerminalSize_WhenValid_ReturnsValidDimensions()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (cols, rows) = _metrics.CalculateTerminalSize(800, 600);

        // Assert
        Assert.True(cols > 0, "Columns should be positive");
        Assert.True(rows > 0, "Rows should be positive");
    }

    /// <summary>
    /// Verifies that CalculateTerminalSize returns default for zero width.
    /// </summary>
    [Fact]
    public void CalculateTerminalSize_ZeroWidth_ReturnsDefault()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (cols, rows) = _metrics.CalculateTerminalSize(0, 600);

        // Assert
        Assert.Equal(80, cols);
        Assert.Equal(24, rows);
    }

    /// <summary>
    /// Verifies that CalculateTerminalSize returns default for zero height.
    /// </summary>
    [Fact]
    public void CalculateTerminalSize_ZeroHeight_ReturnsDefault()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (cols, rows) = _metrics.CalculateTerminalSize(800, 0);

        // Assert
        Assert.Equal(80, cols);
        Assert.Equal(24, rows);
    }

    /// <summary>
    /// Verifies that CalculateTerminalSize returns default for negative dimensions.
    /// </summary>
    [Fact]
    public void CalculateTerminalSize_NegativeDimensions_ReturnsDefault()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (cols, rows) = _metrics.CalculateTerminalSize(-100, -100);

        // Assert
        Assert.Equal(80, cols);
        Assert.Equal(24, rows);
    }

    /// <summary>
    /// Verifies that larger dimensions produce more columns and rows.
    /// </summary>
    [Fact]
    public void CalculateTerminalSize_LargerDimensions_ProducesMoreCells()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (smallCols, smallRows) = _metrics.CalculateTerminalSize(400, 300);
        var (largeCols, largeRows) = _metrics.CalculateTerminalSize(800, 600);

        // Assert
        Assert.True(largeCols > smallCols, "Larger width should produce more columns");
        Assert.True(largeRows > smallRows, "Larger height should produce more rows");
    }

    /// <summary>
    /// Verifies that CalculateTerminalSize returns at least (1, 1) for small valid dimensions.
    /// </summary>
    [Fact]
    public void CalculateTerminalSize_VerySmallDimensions_ReturnsAtLeastOne()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (cols, rows) = _metrics.CalculateTerminalSize(1, 1);

        // Assert
        Assert.True(cols >= 1, "Columns should be at least 1");
        Assert.True(rows >= 1, "Rows should be at least 1");
    }

    #endregion

    #region PixelToCell Tests

    /// <summary>
    /// Verifies that PixelToCell returns (0, 0) when metrics are invalid.
    /// </summary>
    [Fact]
    public void PixelToCell_WhenInvalid_ReturnsZero()
    {
        // Act (metrics not initialized)
        var (col, row) = _metrics.PixelToCell(100, 100);

        // Assert
        Assert.Equal(0, col);
        Assert.Equal(0, row);
    }

    /// <summary>
    /// Verifies that PixelToCell returns (0, 0) for origin.
    /// </summary>
    [Fact]
    public void PixelToCell_AtOrigin_ReturnsZero()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (col, row) = _metrics.PixelToCell(0, 0);

        // Assert
        Assert.Equal(0, col);
        Assert.Equal(0, row);
    }

    /// <summary>
    /// Verifies that PixelToCell clamps negative coordinates to zero.
    /// </summary>
    [Fact]
    public void PixelToCell_NegativeCoordinates_ClampsToZero()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (col, row) = _metrics.PixelToCell(-50, -50);

        // Assert
        Assert.Equal(0, col);
        Assert.Equal(0, row);
    }

    /// <summary>
    /// Verifies that PixelToCell correctly converts coordinates within first cell.
    /// </summary>
    [Fact]
    public void PixelToCell_WithinFirstCell_ReturnsZero()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);
        var halfCharWidth = _metrics.CharWidth / 2;
        var halfLineHeight = _metrics.LineHeight / 2;

        // Act
        var (col, row) = _metrics.PixelToCell(halfCharWidth, halfLineHeight);

        // Assert
        Assert.Equal(0, col);
        Assert.Equal(0, row);
    }

    /// <summary>
    /// Verifies that PixelToCell correctly converts coordinates to second column.
    /// </summary>
    [Fact]
    public void PixelToCell_SecondColumn_ReturnsOne()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);
        var x = _metrics.CharWidth + 1; // Just past first cell

        // Act
        var (col, _) = _metrics.PixelToCell(x, 0);

        // Assert
        Assert.Equal(1, col);
    }

    /// <summary>
    /// Verifies that PixelToCell correctly converts coordinates to second row.
    /// </summary>
    [Fact]
    public void PixelToCell_SecondRow_ReturnsOne()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);
        var y = _metrics.LineHeight + 1; // Just past first row

        // Act
        var (_, row) = _metrics.PixelToCell(0, y);

        // Assert
        Assert.Equal(1, row);
    }

    #endregion

    #region CellToPixel Tests

    /// <summary>
    /// Verifies that CellToPixel returns (0, 0) for cell (0, 0).
    /// </summary>
    [Fact]
    public void CellToPixel_AtOrigin_ReturnsZero()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (x, y) = _metrics.CellToPixel(0, 0);

        // Assert
        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    /// <summary>
    /// Verifies that CellToPixel returns correct position for cell (1, 0).
    /// </summary>
    [Fact]
    public void CellToPixel_SecondColumn_ReturnsCharWidth()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (x, _) = _metrics.CellToPixel(1, 0);

        // Assert
        Assert.Equal(_metrics.CharWidth, x);
    }

    /// <summary>
    /// Verifies that CellToPixel returns correct position for cell (0, 1).
    /// </summary>
    [Fact]
    public void CellToPixel_SecondRow_ReturnsLineHeight()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (_, y) = _metrics.CellToPixel(0, 1);

        // Assert
        Assert.Equal(_metrics.LineHeight, y);
    }

    /// <summary>
    /// Verifies that CellToPixel correctly scales for larger coordinates.
    /// </summary>
    [Fact]
    public void CellToPixel_LargerCoordinates_ScalesCorrectly()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);
        const int col = 10;
        const int row = 5;

        // Act
        var (x, y) = _metrics.CellToPixel(col, row);

        // Assert
        Assert.Equal(col * _metrics.CharWidth, x);
        Assert.Equal(row * _metrics.LineHeight, y);
    }

    #endregion

    #region Roundtrip Tests

    /// <summary>
    /// Verifies that PixelToCell and CellToPixel are inverse operations.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 3)]
    [InlineData(10, 10)]
    [InlineData(79, 23)]
    public void CellToPixel_ThenPixelToCell_Roundtrips(int col, int row)
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        var (x, y) = _metrics.CellToPixel(col, row);
        var (resultCol, resultRow) = _metrics.PixelToCell(x, y);

        // Assert
        Assert.Equal(col, resultCol);
        Assert.Equal(row, resultRow);
    }

    #endregion

    #region CreateTextPaint Tests

    /// <summary>
    /// Verifies that CreateTextPaint returns a non-null paint after Update.
    /// </summary>
    [Fact]
    public void CreateTextPaint_AfterUpdate_ReturnsNonNull()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        using var paint = _metrics.CreateTextPaint();

        // Assert
        Assert.NotNull(paint);
    }

    /// <summary>
    /// Verifies that CreateTextPaint with bold returns a paint with the correct size.
    /// </summary>
    [Fact]
    public void CreateTextPaint_WithBold_HasCorrectSize()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        using var paint = _metrics.CreateTextPaint(bold: true);

        // Assert
        Assert.Equal(_metrics.FontSize, paint.TextSize);
    }

    /// <summary>
    /// Verifies that CreateTextPaint with italic returns a paint with the correct size.
    /// </summary>
    [Fact]
    public void CreateTextPaint_WithItalic_HasCorrectSize()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        using var paint = _metrics.CreateTextPaint(italic: true);

        // Assert
        Assert.Equal(_metrics.FontSize, paint.TextSize);
    }

    /// <summary>
    /// Verifies that CreateTextPaint with bold and italic returns a paint with the correct size.
    /// </summary>
    [Fact]
    public void CreateTextPaint_WithBoldAndItalic_HasCorrectSize()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act
        using var paint = _metrics.CreateTextPaint(bold: true, italic: true);

        // Assert
        Assert.Equal(_metrics.FontSize, paint.TextSize);
    }

    #endregion

    #region Typeface Property Tests

    /// <summary>
    /// Verifies that Typeface returns a non-null value even before Update.
    /// </summary>
    [Fact]
    public void Typeface_BeforeUpdate_ReturnsNonNull()
    {
        // Act & Assert
        Assert.NotNull(_metrics.Typeface);
    }

    /// <summary>
    /// Verifies that Typeface returns a non-null value after Update.
    /// </summary>
    [Fact]
    public void Typeface_AfterUpdate_ReturnsNonNull()
    {
        // Arrange
        _metrics.Update("Courier New", 14f);

        // Act & Assert
        Assert.NotNull(_metrics.Typeface);
    }

    #endregion

    #region Disposal Tests

    /// <summary>
    /// Verifies that Dispose can be called multiple times without error.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var metrics = new TerminalFontMetrics();
        metrics.Update("Courier New", 14f);

        // Act & Assert (should not throw)
        metrics.Dispose();
        metrics.Dispose();
    }

    #endregion
}
