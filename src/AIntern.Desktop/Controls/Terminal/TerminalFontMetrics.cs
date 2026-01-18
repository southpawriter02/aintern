namespace AIntern.Desktop.Controls.Terminal;

using Microsoft.Extensions.Logging;
using SkiaSharp;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalFontMetrics (v0.5.2b)                                            │
// │ Manages font metrics for terminal rendering with coordinate conversion. │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Manages font metrics for terminal rendering.
/// </summary>
/// <remarks>
/// <para>
/// Handles font loading, measurement, and coordinate conversion between
/// pixel positions and terminal cell coordinates. Key responsibilities:
/// <list type="bullet">
///   <item><description>Font family resolution with cross-platform fallback chain</description></item>
///   <item><description>Character cell dimension calculation (width, height, baseline)</description></item>
///   <item><description>Pixel-to-cell and cell-to-pixel coordinate conversion</description></item>
///   <item><description>Terminal size calculation from available pixel dimensions</description></item>
///   <item><description>Text paint creation with font style variations</description></item>
/// </list>
/// </para>
/// <para>
/// Font Fallback Chain (cross-platform):
/// <list type="number">
///   <item><description>Requested font family (e.g., "Cascadia Mono")</description></item>
///   <item><description>Consolas (Windows fallback)</description></item>
///   <item><description>Monaco (macOS fallback)</description></item>
///   <item><description>Courier New (cross-platform fallback)</description></item>
///   <item><description>SKTypeface.Default (system default)</description></item>
/// </list>
/// </para>
/// <para>Added in v0.5.2b.</para>
/// </remarks>
public sealed class TerminalFontMetrics : IDisposable
{
    #region Private Fields

    /// <summary>
    /// The loaded typeface for text rendering.
    /// </summary>
    private SKTypeface? _typeface;

    /// <summary>
    /// Paint object used for text measurement.
    /// </summary>
    private SKPaint? _measurePaint;

    /// <summary>
    /// Optional logger for diagnostic output.
    /// </summary>
    private readonly ILogger? _logger;

    /// <summary>
    /// Track disposed state.
    /// </summary>
    private bool _disposed;

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the loaded typeface for text rendering.
    /// </summary>
    /// <remarks>
    /// Returns <see cref="SKTypeface.Default"/> if no typeface has been loaded.
    /// </remarks>
    public SKTypeface Typeface => _typeface ?? SKTypeface.Default;

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public float FontSize { get; private set; }

    /// <summary>
    /// Gets the width of a single character cell in pixels.
    /// </summary>
    /// <remarks>
    /// Measured using the 'M' character (em-width) to ensure consistent
    /// monospace character width. For proper terminal rendering, all
    /// characters should be rendered within this fixed width.
    /// </remarks>
    public float CharWidth { get; private set; }

    /// <summary>
    /// Gets the height of a line in pixels (ascent + descent + leading).
    /// </summary>
    public float LineHeight { get; private set; }

    /// <summary>
    /// Gets the distance from top of cell to text baseline in pixels.
    /// </summary>
    /// <remarks>
    /// Used for positioning text within cells. Characters are drawn
    /// at (x, y + Baseline) where (x, y) is the cell's top-left corner.
    /// </remarks>
    public float Baseline { get; private set; }

    /// <summary>
    /// Gets the font ascent (distance from baseline to top of character) in pixels.
    /// </summary>
    public float Ascent { get; private set; }

    /// <summary>
    /// Gets the font descent (distance from baseline to bottom of character) in pixels.
    /// </summary>
    public float Descent { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the metrics have been calculated and are valid for rendering.
    /// </summary>
    /// <remarks>
    /// Returns <c>true</c> if both the typeface is loaded and character width is positive.
    /// </remarks>
    public bool IsValid => _typeface != null && CharWidth > 0;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalFontMetrics"/> class.
    /// </summary>
    public TerminalFontMetrics() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalFontMetrics"/> class
    /// with optional logging support.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public TerminalFontMetrics(ILogger? logger)
    {
        _logger = logger;
        _logger?.LogDebug("[TerminalFontMetrics] Created new instance");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Updates the font and recalculates all metrics.
    /// </summary>
    /// <param name="fontFamily">The font family name to load.</param>
    /// <param name="fontSize">The font size in points.</param>
    /// <remarks>
    /// <para>
    /// If the requested font family is not found, falls back through:
    /// Consolas → Monaco → Courier New → System Default
    /// </para>
    /// <para>
    /// After calling this method, check <see cref="IsValid"/> to ensure
    /// metrics were successfully calculated.
    /// </para>
    /// </remarks>
    public void Update(string fontFamily, float fontSize)
    {
        _logger?.LogDebug(
            "[TerminalFontMetrics] Updating font: family={FontFamily}, size={FontSize}",
            fontFamily, fontSize);

        FontSize = fontSize;

        // ─────────────────────────────────────────────────────────────────
        // Font Loading with Fallback Chain
        // First try the requested font, then fall back to common monospace fonts
        // ─────────────────────────────────────────────────────────────────
        _typeface?.Dispose();
        _typeface = TryLoadFont(fontFamily);

        // If requested font not found, try fallback chain
        if (_typeface == null || _typeface.FamilyName != fontFamily)
        {
            _logger?.LogDebug(
                "[TerminalFontMetrics] Font '{FontFamily}' not found, trying fallbacks",
                fontFamily);

            _typeface?.Dispose();
            _typeface = TryLoadFont("Consolas")    // Windows
                     ?? TryLoadFont("Monaco")       // macOS
                     ?? TryLoadFont("Courier New")  // Cross-platform
                     ?? SKTypeface.Default;         // System default

            _logger?.LogDebug(
                "[TerminalFontMetrics] Using fallback font: {FallbackFont}",
                _typeface?.FamilyName ?? "Default");
        }

        CalculateMetrics();

        _logger?.LogInformation(
            "[TerminalFontMetrics] Metrics calculated: font={FontName}, size={FontSize}, " +
            "charWidth={CharWidth:F2}, lineHeight={LineHeight:F2}, baseline={Baseline:F2}",
            _typeface?.FamilyName ?? "Default", FontSize, CharWidth, LineHeight, Baseline);
    }

    /// <summary>
    /// Calculates the terminal size in columns and rows for given pixel dimensions.
    /// </summary>
    /// <param name="width">Available width in pixels.</param>
    /// <param name="height">Available height in pixels.</param>
    /// <returns>A tuple of (columns, rows) for the terminal grid.</returns>
    /// <remarks>
    /// <para>
    /// Returns (80, 24) as the default terminal size if:
    /// <list type="bullet">
    ///   <item><description>Metrics are invalid (<see cref="IsValid"/> is false)</description></item>
    ///   <item><description>Width or height is non-positive</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The returned dimensions are guaranteed to be at least (1, 1) if metrics are valid.
    /// </para>
    /// </remarks>
    public (int Columns, int Rows) CalculateTerminalSize(double width, double height)
    {
        // Default terminal size for invalid metrics or dimensions
        if (!IsValid || width <= 0 || height <= 0)
        {
            _logger?.LogDebug(
                "[TerminalFontMetrics] Using default size (80x24): valid={IsValid}, " +
                "width={Width:F2}, height={Height:F2}",
                IsValid, width, height);
            return (80, 24);
        }

        // Calculate columns and rows, ensuring at least 1 of each
        var cols = Math.Max(1, (int)(width / CharWidth));
        var rows = Math.Max(1, (int)(height / LineHeight));

        _logger?.LogDebug(
            "[TerminalFontMetrics] Calculated terminal size: " +
            "pixels=({Width:F2}×{Height:F2}) → cells=({Cols}×{Rows})",
            width, height, cols, rows);

        return (cols, rows);
    }

    /// <summary>
    /// Converts pixel coordinates to terminal cell coordinates.
    /// </summary>
    /// <param name="x">X position in pixels.</param>
    /// <param name="y">Y position in pixels.</param>
    /// <returns>A tuple of (column, row) in the terminal grid.</returns>
    /// <remarks>
    /// <para>
    /// Used for mouse input handling to determine which cell was clicked.
    /// Returns (0, 0) if metrics are invalid.
    /// </para>
    /// <para>
    /// The returned coordinates are guaranteed to be non-negative.
    /// </para>
    /// </remarks>
    public (int Column, int Row) PixelToCell(double x, double y)
    {
        if (!IsValid)
            return (0, 0);

        // Floor division to get cell indices
        var col = Math.Max(0, (int)(x / CharWidth));
        var row = Math.Max(0, (int)(y / LineHeight));

        return (col, row);
    }

    /// <summary>
    /// Converts terminal cell coordinates to pixel position.
    /// </summary>
    /// <param name="column">Column index in the terminal grid.</param>
    /// <param name="row">Row index in the terminal grid.</param>
    /// <returns>A tuple of (x, y) pixel coordinates for the top-left corner of the cell.</returns>
    public (float X, float Y) CellToPixel(int column, int row)
    {
        return (column * CharWidth, row * LineHeight);
    }

    /// <summary>
    /// Creates an SKPaint configured for rendering text with the specified attributes.
    /// </summary>
    /// <param name="bold">Whether to use bold font style.</param>
    /// <param name="italic">Whether to use italic font style.</param>
    /// <returns>A new <see cref="SKPaint"/> instance configured for text rendering.</returns>
    /// <remarks>
    /// <para>
    /// The caller is responsible for disposing the returned <see cref="SKPaint"/>.
    /// </para>
    /// <para>
    /// For bold and italic text, a matching styled typeface is loaded if available.
    /// If the styled variant is not available, the base typeface is used with
    /// simulated styling (e.g., <see cref="SKPaint.FakeBoldText"/>).
    /// </para>
    /// </remarks>
    public SKPaint CreateTextPaint(bool bold = false, bool italic = false)
    {
        // Determine the font style based on bold and italic flags
        var style = (bold, italic) switch
        {
            (true, true) => SKFontStyle.BoldItalic,
            (true, false) => SKFontStyle.Bold,
            (false, true) => SKFontStyle.Italic,
            _ => SKFontStyle.Normal
        };

        // Try to load a styled version of the typeface
        var typeface = _typeface;
        if (style != SKFontStyle.Normal && _typeface != null)
        {
            // Attempt to load the styled variant
            var styledTypeface = SKTypeface.FromFamilyName(
                _typeface.FamilyName,
                style);

            // Only use if we actually got the styled version
            if (styledTypeface != null)
            {
                typeface = styledTypeface;
            }
        }

        return new SKPaint
        {
            Typeface = typeface,
            TextSize = FontSize,
            IsAntialias = true
        };
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Tries to load a font by family name.
    /// </summary>
    /// <param name="fontFamily">The font family name to load.</param>
    /// <returns>The loaded typeface, or null if not found.</returns>
    private SKTypeface? TryLoadFont(string fontFamily)
    {
        try
        {
            return SKTypeface.FromFamilyName(fontFamily, SKFontStyle.Normal);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "[TerminalFontMetrics] Failed to load font: {FontFamily}",
                fontFamily);
            return null;
        }
    }

    /// <summary>
    /// Calculates font metrics using the current typeface and font size.
    /// </summary>
    private void CalculateMetrics()
    {
        // ─────────────────────────────────────────────────────────────────
        // Create measurement paint with current font settings
        // ─────────────────────────────────────────────────────────────────
        _measurePaint?.Dispose();
        _measurePaint = new SKPaint
        {
            Typeface = _typeface,
            TextSize = FontSize,
            IsAntialias = true
        };

        // ─────────────────────────────────────────────────────────────────
        // Measure character width using 'M' (em-width)
        // For monospace fonts, all characters should have the same width,
        // but we use 'M' as it's traditionally the widest character
        // ─────────────────────────────────────────────────────────────────
        CharWidth = _measurePaint.MeasureText("M");

        // ─────────────────────────────────────────────────────────────────
        // Extract font metrics for line height and baseline calculation
        // ─────────────────────────────────────────────────────────────────
        var metrics = _measurePaint.FontMetrics;

        // Ascent is negative in SkiaSharp (distance above baseline)
        // We need the absolute value for calculations
        Ascent = -metrics.Ascent;
        Descent = metrics.Descent;

        // Line height calculation:
        // - Ascent + Descent gives the character height
        // - Add leading (inter-line spacing) if available, otherwise use minimum of 2px
        // - This ensures adequate vertical spacing between lines
        var leading = metrics.Leading > 0 ? metrics.Leading : 2;
        LineHeight = Ascent + Descent + leading;

        // Baseline is the distance from the top of the cell to where text is drawn
        // This equals the ascent (characters sit on the baseline)
        Baseline = Ascent;
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes of the font resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _logger?.LogDebug("[TerminalFontMetrics] Disposing resources");

        _measurePaint?.Dispose();
        _measurePaint = null;

        _typeface?.Dispose();
        _typeface = null;

        _disposed = true;
    }

    #endregion
}
