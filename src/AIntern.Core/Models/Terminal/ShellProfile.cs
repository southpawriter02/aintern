namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL PROFILE (v0.5.3c)                                                 │
// │ User-configurable shell profile for terminal sessions.                  │
// └─────────────────────────────────────────────────────────────────────────┘

using System.Text.Json.Serialization;
using AIntern.Core.Interfaces;

/// <summary>
/// User-configurable shell profile for terminal sessions.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3c.</para>
/// <para>
/// Profiles allow customization of:
/// <list type="bullet">
///   <item>Shell executable and arguments</item>
///   <item>Starting directory and startup commands</item>
///   <item>Environment variables</item>
///   <item>Appearance (font, theme, cursor)</item>
///   <item>Behavior (close-on-exit, bell style)</item>
/// </list>
/// </para>
/// <para>
/// Nullable appearance properties indicate "use application default".
/// When null, the value from <see cref="ShellProfileDefaults"/> is used.
/// </para>
/// </remarks>
public sealed class ShellProfile
{
    // ─────────────────────────────────────────────────────────────────────
    // Identity
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unique identifier for this profile.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Display name for this profile (shown in tabs and menus).
    /// </summary>
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Optional path to an icon file for this profile.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Optional accent color for this profile's tab (hex format, e.g., "#FF5733").
    /// </summary>
    public string? AccentColor { get; set; }

    // ─────────────────────────────────────────────────────────────────────
    // Shell Configuration
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Full path to the shell executable.
    /// </summary>
    public string ShellPath { get; set; } = string.Empty;

    /// <summary>
    /// Type of shell. Auto-detected if not explicitly specified.
    /// </summary>
    public ShellType ShellType { get; set; } = ShellType.Unknown;

    /// <summary>
    /// Command-line arguments passed to the shell on startup.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Starting directory for new sessions.
    /// </summary>
    /// <remarks>
    /// When null, inherits from the current workspace.
    /// </remarks>
    public string? StartingDirectory { get; set; }

    /// <summary>
    /// Command to run after shell initialization.
    /// </summary>
    /// <remarks>
    /// Example: "cd ~/projects &amp;&amp; clear"
    /// </remarks>
    public string? StartupCommand { get; set; }

    /// <summary>
    /// Custom environment variables for this profile.
    /// </summary>
    /// <remarks>
    /// Merged with shell-specific defaults from <see cref="ShellConfiguration"/>.
    /// </remarks>
    public Dictionary<string, string> Environment { get; set; } = new();

    // ─────────────────────────────────────────────────────────────────────
    // Appearance Overrides (null = use application default)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Font family override for this profile.
    /// </summary>
    /// <remarks>
    /// When null, uses AppSettings.TerminalFontFamily.
    /// </remarks>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Font size override for this profile.
    /// </summary>
    /// <remarks>
    /// When null, uses AppSettings.TerminalFontSize.
    /// </remarks>
    public double? FontSize { get; set; }

    /// <summary>
    /// Theme name override for this profile.
    /// </summary>
    /// <remarks>
    /// When null, uses AppSettings.TerminalTheme.
    /// </remarks>
    public string? ThemeName { get; set; }

    /// <summary>
    /// Cursor style override for this profile.
    /// </summary>
    /// <remarks>
    /// When null, uses AppSettings.TerminalCursorStyle.
    /// </remarks>
    public TerminalCursorStyle? CursorStyle { get; set; }

    /// <summary>
    /// Cursor blink override for this profile.
    /// </summary>
    /// <remarks>
    /// When null, uses AppSettings.TerminalCursorBlink.
    /// </remarks>
    public bool? CursorBlink { get; set; }

    /// <summary>
    /// Scrollback lines override for this profile.
    /// </summary>
    /// <remarks>
    /// When null, uses AppSettings.TerminalScrollbackLines.
    /// </remarks>
    public int? ScrollbackLines { get; set; }

    // ─────────────────────────────────────────────────────────────────────
    // Behavior
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tab close behavior when shell process exits.
    /// </summary>
    public ProfileCloseOnExit CloseOnExit { get; set; } = ProfileCloseOnExit.OnCleanExit;

    /// <summary>
    /// Bell notification style for this profile.
    /// </summary>
    public TerminalBellStyle BellStyle { get; set; } = TerminalBellStyle.Audible;

    /// <summary>
    /// Custom tab title format.
    /// </summary>
    /// <remarks>
    /// <para>Supports placeholders:</para>
    /// <list type="bullet">
    ///   <item>{name} - Profile name</item>
    ///   <item>{cwd} - Current working directory (basename)</item>
    ///   <item>{cwdFull} - Full path</item>
    ///   <item>{process} - Running process name</item>
    ///   <item>{shell} - Shell executable name</item>
    ///   <item>{user} - Current username</item>
    ///   <item>{host} - Machine hostname</item>
    /// </list>
    /// <para>When null, uses profile name or detected process.</para>
    /// </remarks>
    public string? TabTitleFormat { get; set; }

    // ─────────────────────────────────────────────────────────────────────
    // Profile Metadata
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Whether this is the default profile for new terminals.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this profile is hidden from the shell selector dropdown.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether this is a built-in system profile (read-only, cannot be deleted).
    /// </summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>
    /// Sort order in the shell selector (lower = higher priority).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Timestamp when this profile was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this profile was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    // ─────────────────────────────────────────────────────────────────────
    // Validation
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates that the profile has the minimum required configuration.
    /// </summary>
    /// <remarks>
    /// A profile is valid if it has both a non-empty ShellPath and Name.
    /// </remarks>
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(ShellPath) &&
                           !string.IsNullOrWhiteSpace(Name);

    // ─────────────────────────────────────────────────────────────────────
    // Clone Method
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a deep copy of this profile with a new ID.
    /// </summary>
    /// <remarks>
    /// <para>The copy has the following modifications:</para>
    /// <list type="bullet">
    ///   <item>New unique ID</item>
    ///   <item>Name appended with " (Copy)"</item>
    ///   <item>IsDefault = false</item>
    ///   <item>IsBuiltIn = false</item>
    ///   <item>SortOrder incremented by 1</item>
    ///   <item>New timestamps</item>
    /// </list>
    /// <para>Environment dictionary is deep-copied.</para>
    /// </remarks>
    /// <returns>A new ShellProfile instance with copied settings.</returns>
    public ShellProfile Clone()
    {
        return new ShellProfile
        {
            // New identity (Id is auto-generated)
            Name = $"{Name} (Copy)",
            IconPath = IconPath,
            AccentColor = AccentColor,

            // Shell configuration (copied)
            ShellPath = ShellPath,
            ShellType = ShellType,
            Arguments = Arguments,
            StartingDirectory = StartingDirectory,
            StartupCommand = StartupCommand,
            Environment = new Dictionary<string, string>(Environment),

            // Appearance (copied)
            FontFamily = FontFamily,
            FontSize = FontSize,
            ThemeName = ThemeName,
            CursorStyle = CursorStyle,
            CursorBlink = CursorBlink,
            ScrollbackLines = ScrollbackLines,

            // Behavior (copied)
            CloseOnExit = CloseOnExit,
            BellStyle = BellStyle,
            TabTitleFormat = TabTitleFormat,

            // Metadata (modified for copy)
            IsDefault = false,           // Copies are never default
            IsHidden = IsHidden,         // Preserve hidden state
            IsBuiltIn = false,           // Copies are user profiles
            SortOrder = SortOrder + 1    // Place after original
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // Display
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a string representation of this profile.
    /// </summary>
    public override string ToString() => IsDefault ? $"{Name} (Default)" : Name;
}
