// ============================================================================
// File: SearchHighlightStyle.cs
// Path: src/AIntern.Core/Models/Terminal/SearchHighlightStyle.cs
// Description: Visual styling configuration for search result highlights.
//              Colors are specified as hex strings for cross-platform compatibility.
//              Provides theme presets for dark, light, and high-contrast modes.
// Created: 2026-01-18
// AI Intern v0.5.5a - Terminal Search Models
// ============================================================================

namespace AIntern.Core.Models.Terminal;

using System.Text.RegularExpressions;

/// <summary>
/// Visual styling configuration for search result highlights.
/// Colors are specified as hex strings for cross-platform compatibility.
/// </summary>
/// <remarks>
/// <para>
/// This class defines the visual appearance of search results in the terminal:
/// </para>
/// <list type="bullet">
///   <item><description>Non-current matches use MatchBackground/MatchForeground</description></item>
///   <item><description>The current/focused match uses CurrentMatchBackground/Foreground/Border</description></item>
///   <item><description>MatchOpacity controls visibility of non-current matches</description></item>
/// </list>
/// <para>
/// Color Format:
/// All colors are specified as hex strings (e.g., "#FFFF00" for yellow).
/// Supported formats:
/// <list type="bullet">
///   <item><description>#RGB - Short hex (expanded to #RRGGBB)</description></item>
///   <item><description>#RRGGBB - Standard hex</description></item>
///   <item><description>#AARRGGBB - Hex with alpha channel</description></item>
/// </list>
/// </para>
/// <para>
/// Example Usage:
/// <code>
/// var style = SearchHighlightStyle.ForTheme(isDarkTheme: true);
/// // Or customize:
/// var custom = new SearchHighlightStyle
/// {
///     MatchBackground = "#FFFF00",
///     CurrentMatchBackground = "#FF8C00"
/// };
/// </code>
/// </para>
/// </remarks>
public sealed class SearchHighlightStyle
{
    #region Constants

    /// <summary>
    /// Regex pattern for validating hex color strings.
    /// </summary>
    private static readonly Regex HexColorPattern = new(
        @"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$",
        RegexOptions.Compiled);

    #endregion

    #region Match Colors

    /// <summary>
    /// Gets or sets the background color for matched text (hex, e.g., "#FFFF00").
    /// Default: Yellow
    /// </summary>
    /// <remarks>
    /// Applied to all matches except the currently focused one.
    /// Should contrast well with terminal background while being non-distracting.
    /// </remarks>
    public string MatchBackground { get; set; } = "#FFFF00";

    /// <summary>
    /// Gets or sets the foreground color for matched text (hex).
    /// Default: Black
    /// </summary>
    /// <remarks>
    /// Applied to the text of all matches except the current one.
    /// Should be readable against the MatchBackground color.
    /// </remarks>
    public string MatchForeground { get; set; } = "#000000";

    #endregion

    #region Current Match Colors

    /// <summary>
    /// Gets or sets the background color for the currently focused match (hex).
    /// Default: Orange (#FF8C00)
    /// </summary>
    /// <remarks>
    /// Applied only to the currently focused/selected match.
    /// Should be more prominent than the regular match background.
    /// </remarks>
    public string CurrentMatchBackground { get; set; } = "#FF8C00";

    /// <summary>
    /// Gets or sets the foreground color for the currently focused match (hex).
    /// Default: Black
    /// </summary>
    /// <remarks>
    /// Applied to the text of the current match.
    /// Should be readable against the CurrentMatchBackground color.
    /// </remarks>
    public string CurrentMatchForeground { get; set; } = "#000000";

    /// <summary>
    /// Gets or sets the border color for the currently focused match (hex).
    /// Default: OrangeRed (#FF4500)
    /// </summary>
    /// <remarks>
    /// Optional border around the current match for extra visibility.
    /// Only rendered if CurrentMatchBorderThickness is greater than 0.
    /// </remarks>
    public string CurrentMatchBorder { get; set; } = "#FF4500";

    /// <summary>
    /// Gets or sets the border thickness for the current match in pixels.
    /// Default: 1
    /// </summary>
    /// <remarks>
    /// Set to 0 to disable the border. Typical values are 1-2 pixels.
    /// </remarks>
    public int CurrentMatchBorderThickness
    {
        get => _currentMatchBorderThickness;
        set => _currentMatchBorderThickness = Math.Clamp(value, 0, 5);
    }
    private int _currentMatchBorderThickness = 1;

    #endregion

    #region Styling Options

    /// <summary>
    /// Gets or sets the opacity for non-current matches (0.0-1.0).
    /// Default: 0.7
    /// </summary>
    /// <remarks>
    /// Lower values make non-current matches less prominent.
    /// This helps the current match stand out more clearly.
    /// </remarks>
    public double MatchOpacity
    {
        get => _matchOpacity;
        set => _matchOpacity = Math.Clamp(value, 0.0, 1.0);
    }
    private double _matchOpacity = 0.7;

    /// <summary>
    /// Gets or sets whether to use a box/outline style instead of solid background.
    /// Default: false
    /// </summary>
    /// <remarks>
    /// When true, matches are highlighted with an outline/box rather than
    /// a filled background. This can be less visually intrusive.
    /// </remarks>
    public bool UseOutlineStyle { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to underline matched text.
    /// Default: false
    /// </summary>
    /// <remarks>
    /// Can be combined with background highlighting for extra emphasis.
    /// </remarks>
    public bool UnderlineMatches { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to bold matched text.
    /// Default: false
    /// </summary>
    /// <remarks>
    /// Can make matches more visible, especially in outline mode.
    /// </remarks>
    public bool BoldMatches { get; set; } = false;

    #endregion

    #region Theme Presets

    /// <summary>
    /// Gets the default yellow highlighting style.
    /// </summary>
    /// <remarks>
    /// Classic yellow highlighter style that works reasonably well
    /// in most terminal color schemes.
    /// </remarks>
    public static SearchHighlightStyle Default => new();

    /// <summary>
    /// Gets a dark theme compatible highlighting style.
    /// </summary>
    /// <remarks>
    /// Designed for dark terminal backgrounds with colors that
    /// provide good contrast without being too bright.
    /// </remarks>
    public static SearchHighlightStyle Dark => new()
    {
        MatchBackground = "#3A3D41",
        MatchForeground = "#FFD700",
        CurrentMatchBackground = "#515C6B",
        CurrentMatchForeground = "#FFFFFF",
        CurrentMatchBorder = "#007ACC",
        MatchOpacity = 0.9
    };

    /// <summary>
    /// Gets a light theme compatible highlighting style.
    /// </summary>
    /// <remarks>
    /// Designed for light terminal backgrounds with softer colors
    /// that don't overwhelm the content.
    /// </remarks>
    public static SearchHighlightStyle Light => new()
    {
        MatchBackground = "#FFFACD",
        MatchForeground = "#000000",
        CurrentMatchBackground = "#FFD700",
        CurrentMatchForeground = "#000000",
        CurrentMatchBorder = "#FF8C00",
        MatchOpacity = 0.8
    };

    /// <summary>
    /// Gets a high contrast/accessibility highlighting style.
    /// </summary>
    /// <remarks>
    /// Designed for maximum visibility and accessibility with
    /// strong contrasts and thicker borders.
    /// </remarks>
    public static SearchHighlightStyle HighContrast => new()
    {
        MatchBackground = "#00FF00",
        MatchForeground = "#000000",
        CurrentMatchBackground = "#FF00FF",
        CurrentMatchForeground = "#FFFFFF",
        CurrentMatchBorder = "#FFFFFF",
        CurrentMatchBorderThickness = 2,
        MatchOpacity = 1.0
    };

    /// <summary>
    /// Gets a subtle highlighting style with minimal visual impact.
    /// </summary>
    /// <remarks>
    /// Uses underlines and outlines instead of solid backgrounds.
    /// Less distracting for users who find highlighting too prominent.
    /// </remarks>
    public static SearchHighlightStyle Subtle => new()
    {
        MatchBackground = "#FFFFFF00", // Transparent
        MatchForeground = "#FFD700",
        CurrentMatchBackground = "#40FFFFFF", // Semi-transparent white
        CurrentMatchForeground = "#FFFFFF",
        CurrentMatchBorder = "#FFD700",
        UseOutlineStyle = true,
        UnderlineMatches = true,
        MatchOpacity = 1.0
    };

    /// <summary>
    /// Gets a Solarized Dark theme compatible style.
    /// </summary>
    public static SearchHighlightStyle SolarizedDark => new()
    {
        MatchBackground = "#073642",
        MatchForeground = "#B58900",
        CurrentMatchBackground = "#586E75",
        CurrentMatchForeground = "#FDF6E3",
        CurrentMatchBorder = "#268BD2",
        MatchOpacity = 0.9
    };

    /// <summary>
    /// Gets a Solarized Light theme compatible style.
    /// </summary>
    public static SearchHighlightStyle SolarizedLight => new()
    {
        MatchBackground = "#EEE8D5",
        MatchForeground = "#B58900",
        CurrentMatchBackground = "#93A1A1",
        CurrentMatchForeground = "#002B36",
        CurrentMatchBorder = "#268BD2",
        MatchOpacity = 0.85
    };

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a style appropriate for the given theme.
    /// </summary>
    /// <param name="isDarkTheme">True for dark theme, false for light theme.</param>
    /// <returns>A highlight style matching the theme.</returns>
    public static SearchHighlightStyle ForTheme(bool isDarkTheme)
    {
        return isDarkTheme ? Dark : Light;
    }

    /// <summary>
    /// Creates a style for a specific terminal theme.
    /// </summary>
    /// <param name="themeName">The theme name (case-insensitive).</param>
    /// <returns>A highlight style for the theme, or Default if not recognized.</returns>
    public static SearchHighlightStyle ForTheme(string themeName)
    {
        return themeName?.ToLowerInvariant() switch
        {
            "dark" => Dark,
            "light" => Light,
            "highcontrast" or "high-contrast" or "high_contrast" => HighContrast,
            "subtle" => Subtle,
            "solarized-dark" or "solarizeddark" or "solarized_dark" => SolarizedDark,
            "solarized-light" or "solarizedlight" or "solarized_light" => SolarizedLight,
            _ => Default
        };
    }

    #endregion

    #region Validation Methods

    /// <summary>
    /// Validates a hex color string.
    /// </summary>
    /// <param name="hexColor">The hex color to validate.</param>
    /// <returns>True if valid hex color format.</returns>
    public static bool IsValidHexColor(string? hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
        {
            return false;
        }

        return HexColorPattern.IsMatch(hexColor);
    }

    /// <summary>
    /// Validates all colors in this style.
    /// </summary>
    /// <returns>List of invalid color property names, empty if all valid.</returns>
    public IReadOnlyList<string> ValidateColors()
    {
        var invalid = new List<string>();

        if (!IsValidHexColor(MatchBackground))
            invalid.Add(nameof(MatchBackground));
        if (!IsValidHexColor(MatchForeground))
            invalid.Add(nameof(MatchForeground));
        if (!IsValidHexColor(CurrentMatchBackground))
            invalid.Add(nameof(CurrentMatchBackground));
        if (!IsValidHexColor(CurrentMatchForeground))
            invalid.Add(nameof(CurrentMatchForeground));
        if (!IsValidHexColor(CurrentMatchBorder))
            invalid.Add(nameof(CurrentMatchBorder));

        return invalid;
    }

    /// <summary>
    /// Gets whether all colors in this style are valid.
    /// </summary>
    public bool AreColorsValid => ValidateColors().Count == 0;

    #endregion

    #region Methods

    /// <summary>
    /// Creates a copy of this style.
    /// </summary>
    /// <returns>A new SearchHighlightStyle with the same values.</returns>
    public SearchHighlightStyle Clone()
    {
        return new SearchHighlightStyle
        {
            MatchBackground = MatchBackground,
            MatchForeground = MatchForeground,
            CurrentMatchBackground = CurrentMatchBackground,
            CurrentMatchForeground = CurrentMatchForeground,
            CurrentMatchBorder = CurrentMatchBorder,
            _currentMatchBorderThickness = _currentMatchBorderThickness,
            _matchOpacity = _matchOpacity,
            UseOutlineStyle = UseOutlineStyle,
            UnderlineMatches = UnderlineMatches,
            BoldMatches = BoldMatches
        };
    }

    /// <summary>
    /// Creates a darkened version of this style (for hover states, etc.).
    /// </summary>
    /// <param name="factor">Darkening factor (0.0-1.0). Default: 0.2</param>
    /// <returns>A new style with darkened colors.</returns>
    public SearchHighlightStyle Darken(double factor = 0.2)
    {
        var clone = Clone();
        clone.MatchBackground = DarkenColor(MatchBackground, factor);
        clone.CurrentMatchBackground = DarkenColor(CurrentMatchBackground, factor);
        return clone;
    }

    /// <summary>
    /// Creates a lightened version of this style.
    /// </summary>
    /// <param name="factor">Lightening factor (0.0-1.0). Default: 0.2</param>
    /// <returns>A new style with lightened colors.</returns>
    public SearchHighlightStyle Lighten(double factor = 0.2)
    {
        var clone = Clone();
        clone.MatchBackground = LightenColor(MatchBackground, factor);
        clone.CurrentMatchBackground = LightenColor(CurrentMatchBackground, factor);
        return clone;
    }

    /// <summary>
    /// Darkens a hex color by a factor.
    /// </summary>
    private static string DarkenColor(string hex, double factor)
    {
        if (!IsValidHexColor(hex) || hex.Length != 7)
        {
            return hex;
        }

        var r = Convert.ToInt32(hex.Substring(1, 2), 16);
        var g = Convert.ToInt32(hex.Substring(3, 2), 16);
        var b = Convert.ToInt32(hex.Substring(5, 2), 16);

        r = (int)(r * (1 - factor));
        g = (int)(g * (1 - factor));
        b = (int)(b * (1 - factor));

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Lightens a hex color by a factor.
    /// </summary>
    private static string LightenColor(string hex, double factor)
    {
        if (!IsValidHexColor(hex) || hex.Length != 7)
        {
            return hex;
        }

        var r = Convert.ToInt32(hex.Substring(1, 2), 16);
        var g = Convert.ToInt32(hex.Substring(3, 2), 16);
        var b = Convert.ToInt32(hex.Substring(5, 2), 16);

        r = Math.Min(255, (int)(r + (255 - r) * factor));
        g = Math.Min(255, (int)(g + (255 - g) * factor));
        b = Math.Min(255, (int)(b + (255 - b) * factor));

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    #endregion

    #region Object Overrides

    /// <summary>
    /// Returns a string representation of this style.
    /// </summary>
    public override string ToString()
    {
        return $"SearchHighlightStyle(Match={MatchBackground}, Current={CurrentMatchBackground}, " +
               $"Opacity={MatchOpacity:F2}, Outline={UseOutlineStyle})";
    }

    #endregion
}
