// ============================================================================
// File: TerminalSearchOptions.cs
// Path: src/AIntern.Core/Models/Terminal/TerminalSearchOptions.cs
// Description: Options for configuring terminal search behavior including
//              result limits, debouncing, regex timeouts, and default settings.
//              Provides preset configurations for common use cases.
// Created: 2026-01-18
// AI Intern v0.5.5a - Terminal Search Models
// ============================================================================

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Options for configuring terminal search behavior.
/// </summary>
/// <remarks>
/// <para>
/// This class provides configuration for the terminal search system, including:
/// </para>
/// <list type="bullet">
///   <item><description>Result limits to prevent excessive memory usage</description></item>
///   <item><description>Debouncing for incremental search performance</description></item>
///   <item><description>Regex timeouts to prevent catastrophic backtracking</description></item>
///   <item><description>Default search option values</description></item>
/// </list>
/// <para>
/// Use the preset factory methods for common scenarios:
/// <code>
/// var options = TerminalSearchOptions.Default;      // Standard usage
/// var large = TerminalSearchOptions.LargeBuffer;    // Very large terminals
/// var quick = TerminalSearchOptions.QuickFind;      // Fast incremental search
/// </code>
/// </para>
/// </remarks>
public sealed class TerminalSearchOptions
{
    #region Constants

    /// <summary>
    /// Minimum allowed debounce delay in milliseconds.
    /// </summary>
    public const int MinDebounceDelayMs = 0;

    /// <summary>
    /// Maximum allowed debounce delay in milliseconds.
    /// </summary>
    public const int MaxDebounceDelayMs = 2000;

    /// <summary>
    /// Minimum regex timeout in milliseconds.
    /// </summary>
    public const int MinRegexTimeoutMs = 100;

    /// <summary>
    /// Maximum regex timeout in milliseconds.
    /// </summary>
    public const int MaxRegexTimeoutMs = 30000;

    /// <summary>
    /// Default maximum result count.
    /// </summary>
    public const int DefaultMaxResults = 10000;

    /// <summary>
    /// Default debounce delay in milliseconds.
    /// </summary>
    public const int DefaultDebounceDelayMs = 150;

    /// <summary>
    /// Default regex timeout in milliseconds.
    /// </summary>
    public const int DefaultRegexTimeoutMs = 5000;

    #endregion

    #region Result Limiting Properties

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// Set to 0 for unlimited results.
    /// Default: 10000
    /// </summary>
    /// <remarks>
    /// Limiting results prevents memory issues when searching buffers with
    /// many matches. For very large buffers, consider increasing this value
    /// using the <see cref="LargeBuffer"/> preset.
    /// </remarks>
    public int MaxResults { get; set; } = DefaultMaxResults;

    #endregion

    #region Timing Properties

    /// <summary>
    /// Gets or sets the debounce delay for incremental search in milliseconds.
    /// Default: 150ms
    /// </summary>
    /// <remarks>
    /// Debouncing prevents searches from firing on every keystroke.
    /// Lower values provide more responsive feedback but higher CPU usage.
    /// Higher values reduce CPU usage but feel less responsive.
    /// </remarks>
    public int DebounceDelayMs
    {
        get => _debounceDelayMs;
        set => _debounceDelayMs = Math.Clamp(value, MinDebounceDelayMs, MaxDebounceDelayMs);
    }
    private int _debounceDelayMs = DefaultDebounceDelayMs;

    /// <summary>
    /// Gets or sets the timeout for regex matching in milliseconds.
    /// Default: 5000ms
    /// </summary>
    /// <remarks>
    /// <para>
    /// This timeout prevents catastrophic backtracking in complex regex patterns.
    /// If a regex match takes longer than this timeout, the search is cancelled
    /// and an error is returned.
    /// </para>
    /// <para>
    /// Consider increasing this for very complex patterns or large buffers,
    /// but be aware this may cause the UI to appear unresponsive.
    /// </para>
    /// </remarks>
    public int RegexTimeoutMs
    {
        get => _regexTimeoutMs;
        set => _regexTimeoutMs = Math.Clamp(value, MinRegexTimeoutMs, MaxRegexTimeoutMs);
    }
    private int _regexTimeoutMs = DefaultRegexTimeoutMs;

    /// <summary>
    /// Gets the regex timeout as a TimeSpan.
    /// </summary>
    public TimeSpan RegexTimeout => TimeSpan.FromMilliseconds(RegexTimeoutMs);

    #endregion

    #region Query Properties

    /// <summary>
    /// Gets or sets the minimum query length before search starts.
    /// Default: 1
    /// </summary>
    /// <remarks>
    /// Setting this higher than 1 can improve performance for large buffers
    /// by avoiding searches for very short (likely meaningless) queries.
    /// </remarks>
    public int MinQueryLength
    {
        get => _minQueryLength;
        set => _minQueryLength = Math.Max(1, value);
    }
    private int _minQueryLength = 1;

    #endregion

    #region Default Settings Properties

    /// <summary>
    /// Gets or sets the default case sensitivity setting.
    /// Default: false (case-insensitive)
    /// </summary>
    /// <remarks>
    /// This is the initial value for case sensitivity when a new search starts.
    /// Users can toggle this per-search in the UI.
    /// </remarks>
    public bool DefaultCaseSensitive { get; set; } = false;

    /// <summary>
    /// Gets or sets the default regex mode setting.
    /// Default: false (plain text search)
    /// </summary>
    /// <remarks>
    /// This is the initial value for regex mode when a new search starts.
    /// Users can toggle this per-search in the UI.
    /// </remarks>
    public bool DefaultUseRegex { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to search in scrollback buffer by default.
    /// Default: true
    /// </summary>
    /// <remarks>
    /// When true, searches include all content in the terminal buffer,
    /// including lines that have scrolled off screen. When false, only
    /// currently visible lines are searched.
    /// </remarks>
    public bool DefaultIncludeScrollback { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to wrap around when reaching end of results by default.
    /// Default: true
    /// </summary>
    /// <remarks>
    /// When true, navigating past the last result wraps to the first result.
    /// When false, navigation stops at the boundaries.
    /// </remarks>
    public bool DefaultWrapAround { get; set; } = true;

    /// <summary>
    /// Gets or sets the default search direction.
    /// Default: Forward
    /// </summary>
    public SearchDirection DefaultDirection { get; set; } = SearchDirection.Forward;

    #endregion

    #region Highlight Properties

    /// <summary>
    /// Gets or sets whether to highlight all matches or only the current one.
    /// Default: true (highlight all)
    /// </summary>
    /// <remarks>
    /// When true, all matches are highlighted with the match colors, and the
    /// current match uses the current match colors. When false, only the
    /// current match is highlighted.
    /// </remarks>
    public bool HighlightAllMatches { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to auto-scroll to the current match.
    /// Default: true
    /// </summary>
    /// <remarks>
    /// When true, the terminal view automatically scrolls to ensure the
    /// current match is visible when navigating through results.
    /// </remarks>
    public bool AutoScrollToMatch { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of context lines to show around matches.
    /// Default: 2
    /// </summary>
    /// <remarks>
    /// When auto-scrolling, this many lines are kept visible above and
    /// below the current match for context.
    /// </remarks>
    public int ContextLines { get; set; } = 2;

    #endregion

    #region Presets

    /// <summary>
    /// Gets default options for most use cases.
    /// </summary>
    /// <remarks>
    /// Provides balanced settings suitable for typical terminal buffers:
    /// <list type="bullet">
    ///   <item><description>Max 10,000 results</description></item>
    ///   <item><description>150ms debounce delay</description></item>
    ///   <item><description>1 character minimum query</description></item>
    ///   <item><description>5 second regex timeout</description></item>
    /// </list>
    /// </remarks>
    public static TerminalSearchOptions Default => new();

    /// <summary>
    /// Gets options optimized for large terminals/buffers.
    /// </summary>
    /// <remarks>
    /// Provides settings for terminals with large scrollback:
    /// <list type="bullet">
    ///   <item><description>Max 50,000 results</description></item>
    ///   <item><description>300ms debounce delay (reduced CPU usage)</description></item>
    ///   <item><description>2 character minimum query (avoid short matches)</description></item>
    ///   <item><description>10 second regex timeout</description></item>
    /// </list>
    /// </remarks>
    public static TerminalSearchOptions LargeBuffer => new()
    {
        MaxResults = 50000,
        DebounceDelayMs = 300,
        MinQueryLength = 2,
        RegexTimeoutMs = 10000
    };

    /// <summary>
    /// Gets options for quick find (smaller debounce for faster feedback).
    /// </summary>
    /// <remarks>
    /// Provides settings for responsive incremental search:
    /// <list type="bullet">
    ///   <item><description>Max 1,000 results (faster processing)</description></item>
    ///   <item><description>50ms debounce delay (very responsive)</description></item>
    ///   <item><description>1 character minimum query</description></item>
    ///   <item><description>2 second regex timeout</description></item>
    /// </list>
    /// </remarks>
    public static TerminalSearchOptions QuickFind => new()
    {
        MaxResults = 1000,
        DebounceDelayMs = 50,
        MinQueryLength = 1,
        RegexTimeoutMs = 2000
    };

    /// <summary>
    /// Gets options for regex-focused searching.
    /// </summary>
    /// <remarks>
    /// Provides settings optimized for regex patterns:
    /// <list type="bullet">
    ///   <item><description>Default to regex mode enabled</description></item>
    ///   <item><description>Longer regex timeout (10 seconds)</description></item>
    ///   <item><description>Longer debounce (300ms) to allow pattern entry</description></item>
    /// </list>
    /// </remarks>
    public static TerminalSearchOptions RegexFocused => new()
    {
        DefaultUseRegex = true,
        RegexTimeoutMs = 10000,
        DebounceDelayMs = 300,
        MinQueryLength = 2
    };

    #endregion

    #region Methods

    /// <summary>
    /// Creates a copy of these options.
    /// </summary>
    /// <returns>A new TerminalSearchOptions instance with the same values.</returns>
    public TerminalSearchOptions Clone()
    {
        return new TerminalSearchOptions
        {
            MaxResults = MaxResults,
            _debounceDelayMs = _debounceDelayMs,
            _minQueryLength = _minQueryLength,
            DefaultCaseSensitive = DefaultCaseSensitive,
            DefaultUseRegex = DefaultUseRegex,
            _regexTimeoutMs = _regexTimeoutMs,
            DefaultIncludeScrollback = DefaultIncludeScrollback,
            DefaultWrapAround = DefaultWrapAround,
            DefaultDirection = DefaultDirection,
            HighlightAllMatches = HighlightAllMatches,
            AutoScrollToMatch = AutoScrollToMatch,
            ContextLines = ContextLines
        };
    }

    /// <summary>
    /// Creates an initial search state based on these options.
    /// </summary>
    /// <returns>A new TerminalSearchState with defaults from this options.</returns>
    public TerminalSearchState CreateInitialState()
    {
        return new TerminalSearchState
        {
            CaseSensitive = DefaultCaseSensitive,
            UseRegex = DefaultUseRegex,
            WrapAround = DefaultWrapAround,
            IncludeScrollback = DefaultIncludeScrollback,
            Direction = DefaultDirection
        };
    }

    /// <summary>
    /// Validates these options and returns any issues found.
    /// </summary>
    /// <returns>List of validation issue messages, empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var issues = new List<string>();

        if (MaxResults < 0)
        {
            issues.Add("MaxResults cannot be negative");
        }

        if (ContextLines < 0)
        {
            issues.Add("ContextLines cannot be negative");
        }

        return issues;
    }

    /// <summary>
    /// Gets whether these options are valid.
    /// </summary>
    public bool IsValid => Validate().Count == 0;

    #endregion

    #region Object Overrides

    /// <summary>
    /// Returns a string representation of these options.
    /// </summary>
    public override string ToString()
    {
        return $"TerminalSearchOptions(Max={MaxResults}, Debounce={DebounceDelayMs}ms, " +
               $"MinQuery={MinQueryLength}, RegexTimeout={RegexTimeoutMs}ms)";
    }

    #endregion
}
