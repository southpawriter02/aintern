namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL CONFIGURATION SERVICE INTERFACE (v0.5.3b)                         │
// │ Shell-specific configuration retrieval and command formatting.          │
// └─────────────────────────────────────────────────────────────────────────┘

using AIntern.Core.Models.Terminal;

/// <summary>
/// Service for retrieving shell-specific configuration and formatting commands.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3b.</para>
/// <para>
/// Provides access to shell-specific configuration including:
/// <list type="bullet">
///   <item>Command syntax for common operations</item>
///   <item>OSC escape sequence generation</item>
///   <item>Environment variable defaults</item>
///   <item>Shell integration scripts</item>
/// </list>
/// </para>
/// </remarks>
public interface IShellConfigurationService
{
    // ─────────────────────────────────────────────────────────────────────
    // Configuration Retrieval
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the configuration for a specific shell type.
    /// </summary>
    /// <param name="shellType">The shell type.</param>
    /// <returns>The shell configuration.</returns>
    /// <remarks>
    /// Returns a pre-built configuration with shell-specific commands,
    /// OSC support flags, syntax elements, and defaults.
    /// </remarks>
    ShellConfiguration GetConfiguration(ShellType shellType);

    /// <summary>
    /// Gets the configuration for a shell by its executable path.
    /// </summary>
    /// <param name="shellPath">Path to the shell executable.</param>
    /// <returns>The shell configuration.</returns>
    /// <remarks>
    /// Uses <see cref="IShellDetectionService.DetectShellType"/> to
    /// determine the shell type, then returns the appropriate config.
    /// </remarks>
    ShellConfiguration GetConfiguration(string shellPath);

    // ─────────────────────────────────────────────────────────────────────
    // Command Formatting
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Formats a change directory command with proper escaping.
    /// </summary>
    /// <param name="shellType">The shell type.</param>
    /// <param name="path">The target directory path.</param>
    /// <returns>The formatted CD command.</returns>
    /// <remarks>
    /// <para>Handles shell-specific path escaping:</para>
    /// <list type="bullet">
    ///   <item>Bash/Zsh: Escapes spaces and special characters</item>
    ///   <item>PowerShell: Wraps in quotes if needed</item>
    ///   <item>Cmd: Uses /d flag and quotes</item>
    /// </list>
    /// </remarks>
    string FormatChangeDirectoryCommand(ShellType shellType, string path);

    // ─────────────────────────────────────────────────────────────────────
    // OSC Escape Sequences
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the OSC escape sequence to report current working directory.
    /// </summary>
    /// <param name="shellType">The shell type.</param>
    /// <param name="directory">The directory path to report.</param>
    /// <returns>The escape sequence, or null if not supported.</returns>
    /// <remarks>
    /// <para>Generates OSC 7 or OSC 9 sequence based on shell support:</para>
    /// <list type="bullet">
    ///   <item>OSC 7: \x1b]7;file://hostname/path\x07</item>
    ///   <item>OSC 9;9: \x1b]9;9;"path"\x07 (Windows Terminal)</item>
    /// </list>
    /// </remarks>
    string? GetCwdReportingEscapeSequence(ShellType shellType, string directory);

    // ─────────────────────────────────────────────────────────────────────
    // Environment
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the default environment variables for a shell type.
    /// </summary>
    /// <param name="shellType">The shell type.</param>
    /// <returns>Dictionary of environment variable name/value pairs.</returns>
    /// <remarks>
    /// Returns a copy of the default environment to avoid mutation.
    /// Typically includes TERM and COLORTERM settings.
    /// </remarks>
    Dictionary<string, string> GetDefaultEnvironment(ShellType shellType);

    // ─────────────────────────────────────────────────────────────────────
    // Shell Integration
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a shell integration script for CWD reporting.
    /// </summary>
    /// <param name="shellType">The shell type.</param>
    /// <returns>The integration script, or null if not supported.</returns>
    /// <remarks>
    /// <para>Generates shell-specific scripts that hook into directory
    /// change events to report the CWD via OSC sequences:</para>
    /// <list type="bullet">
    ///   <item>Bash: PROMPT_COMMAND hook</item>
    ///   <item>Zsh: chpwd hook via add-zsh-hook</item>
    ///   <item>Fish: --on-variable PWD function</item>
    ///   <item>PowerShell: prompt function override</item>
    /// </list>
    /// </remarks>
    string? GenerateShellIntegrationScript(ShellType shellType);
}
