namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL CONFIGURATION MODEL (v0.5.3b)                                     │
// │ Shell-specific commands, syntax, and capabilities.                      │
// └─────────────────────────────────────────────────────────────────────────┘

using AIntern.Core.Interfaces;

/// <summary>
/// Configuration for a specific shell type.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3b.</para>
/// <para>
/// Contains shell-specific information including:
/// <list type="bullet">
///   <item>Command syntax (clear, cd, pwd, history, ls)</item>
///   <item>OSC escape sequence support (OSC 7, 9, 133)</item>
///   <item>Syntax elements (separators, continuations, env vars)</item>
///   <item>Profile file paths and arguments</item>
///   <item>Default environment variables</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ShellConfiguration
{
    // ─────────────────────────────────────────────────────────────────────
    // Shell Type
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the shell type this configuration applies to.
    /// </summary>
    public required ShellType Type { get; init; }

    // ─────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the command to clear the terminal screen.
    /// </summary>
    /// <remarks>
    /// Examples: "clear" (Unix), "cls" (cmd), "Clear-Host" (PowerShell).
    /// </remarks>
    public string ClearCommand { get; init; } = "clear";

    /// <summary>
    /// Gets the command to change the current directory.
    /// </summary>
    /// <remarks>
    /// Examples: "cd" (Unix), "cd /d" (cmd), "Set-Location" (PowerShell).
    /// </remarks>
    public string ChangeDirectoryCommand { get; init; } = "cd";

    /// <summary>
    /// Gets the command to print the current working directory.
    /// </summary>
    /// <remarks>
    /// Examples: "pwd" (Unix), "cd" (cmd shows current), "(Get-Location).Path" (PS).
    /// </remarks>
    public string PrintWorkingDirectoryCommand { get; init; } = "pwd";

    /// <summary>
    /// Gets the command to show command history.
    /// </summary>
    /// <remarks>
    /// Examples: "history" (Unix), "doskey /history" (cmd), "Get-History" (PS).
    /// </remarks>
    public string HistoryCommand { get; init; } = "history";

    /// <summary>
    /// Gets the command to list directory contents.
    /// </summary>
    /// <remarks>
    /// Examples: "ls" (Unix), "dir" (cmd), "Get-ChildItem" (PowerShell).
    /// </remarks>
    public string ListDirectoryCommand { get; init; } = "ls";

    // ─────────────────────────────────────────────────────────────────────
    // OSC Support Flags
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets whether this shell supports OSC 7 (CWD reporting).
    /// </summary>
    /// <remarks>
    /// <para>OSC 7 format: \x1b]7;file://hostname/path\x07</para>
    /// <para>Supported: Bash, Zsh, Fish, Nushell.</para>
    /// <para>Not supported: PowerShell (uses OSC 9), Cmd.</para>
    /// </remarks>
    public bool SupportsOsc7 { get; init; }

    /// <summary>
    /// Gets whether this shell supports OSC 9 (Windows Terminal notification).
    /// </summary>
    /// <remarks>
    /// <para>OSC 9;9 format: \x1b]9;9;"path"\x07</para>
    /// <para>Used by: PowerShell on Windows Terminal.</para>
    /// </remarks>
    public bool SupportsOsc9 { get; init; }

    /// <summary>
    /// Gets whether this shell supports OSC 133 (shell integration marks).
    /// </summary>
    /// <remarks>
    /// <para>OSC 133 marks: A=prompt, B=command, C=output, D=finished.</para>
    /// <para>Enables: Command detection, output extraction, navigation.</para>
    /// <para>Supported: Bash, Zsh, Fish.</para>
    /// </remarks>
    public bool SupportsOsc133 { get; init; }

    // ─────────────────────────────────────────────────────────────────────
    // Paths and Arguments
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the profile file paths for this shell.
    /// </summary>
    /// <remarks>
    /// <para>Example (Bash): ~/.bashrc, ~/.bash_profile, ~/.profile</para>
    /// <para>Example (Zsh): ~/.zshrc, ~/.zprofile, ~/.zshenv</para>
    /// <para>Paths use ~ for home directory.</para>
    /// </remarks>
    public string[] ProfileFiles { get; init; } = [];

    /// <summary>
    /// Gets the arguments for login shell mode.
    /// </summary>
    /// <remarks>
    /// Example: "--login" (Bash/Zsh), "-NoLogo" (PowerShell), "/k" (cmd).
    /// </remarks>
    public string LoginArguments { get; init; } = string.Empty;

    /// <summary>
    /// Gets the arguments for interactive mode.
    /// </summary>
    /// <remarks>
    /// Example: "-i" (Bash/Zsh), "-NoExit" (PowerShell).
    /// </remarks>
    public string InteractiveArguments { get; init; } = string.Empty;

    // ─────────────────────────────────────────────────────────────────────
    // Syntax Elements
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the command separator for chaining commands.
    /// </summary>
    /// <remarks>
    /// Example: ";" (Unix), "&amp;" (cmd).
    /// </remarks>
    public string CommandSeparator { get; init; } = ";";

    /// <summary>
    /// Gets the line continuation character.
    /// </summary>
    /// <remarks>
    /// Example: "\" (Unix), "`" (PowerShell), "^" (cmd).
    /// </remarks>
    public string LineContinuation { get; init; } = "\\";

    /// <summary>
    /// Gets the comment prefix character(s).
    /// </summary>
    /// <remarks>
    /// Example: "#" (Unix/PowerShell), "REM" (cmd).
    /// </remarks>
    public string CommentPrefix { get; init; } = "#";

    /// <summary>
    /// Gets the environment variable reference prefix.
    /// </summary>
    /// <remarks>
    /// Example: "$" (Unix), "$env:" (PowerShell), "%" (cmd).
    /// </remarks>
    public string EnvironmentVariablePrefix { get; init; } = "$";

    /// <summary>
    /// Gets the template for setting environment variables.
    /// </summary>
    /// <remarks>
    /// <para>Format: {0} = variable name, {1} = value.</para>
    /// <para>Example: "export {0}={1}" (Bash), "$env:{0} = \"{1}\"" (PS).</para>
    /// </remarks>
    public string SetEnvironmentVariableTemplate { get; init; } = "export {0}={1}";

    /// <summary>
    /// Gets whether quotes are escaped with backslash.
    /// </summary>
    /// <remarks>
    /// True for Unix shells, False for PowerShell/cmd.
    /// </remarks>
    public bool EscapeQuotesWithBackslash { get; init; } = true;

    // ─────────────────────────────────────────────────────────────────────
    // Environment
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the default environment variables to set for this shell.
    /// </summary>
    /// <remarks>
    /// <para>Example: TERM=xterm-256color, COLORTERM=truecolor.</para>
    /// <para>Used to ensure proper terminal capabilities.</para>
    /// </remarks>
    public Dictionary<string, string> DefaultEnvironment { get; init; } = new();

    // ─────────────────────────────────────────────────────────────────────
    // Display
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a string representation of this configuration.
    /// </summary>
    public override string ToString() => $"ShellConfiguration({Type})";
}
