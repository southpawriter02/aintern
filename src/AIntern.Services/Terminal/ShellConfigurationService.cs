using System.Net;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using Microsoft.Extensions.Logging;

namespace AIntern.Services.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL CONFIGURATION SERVICE (v0.5.3b)                                   │
// │ Shell-specific configuration retrieval and command formatting.          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Provides shell-specific configuration and command formatting.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3b.</para>
/// <para>
/// Contains pre-built configurations for common shells including:
/// Bash, Zsh, Fish, PowerShell, Cmd, Sh, Nushell, and WSL.
/// </para>
/// <para>
/// Features:
/// <list type="bullet">
///   <item>Shell-specific command syntax</item>
///   <item>OSC escape sequence generation (OSC 7, 9)</item>
///   <item>Path escaping per shell type</item>
///   <item>Shell integration script generation</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ShellConfigurationService : IShellConfigurationService
{
    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    private readonly ILogger<ShellConfigurationService> _logger;
    private readonly IShellDetectionService _shellDetection;
    private readonly Dictionary<ShellType, ShellConfiguration> _configurations;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new shell configuration service.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="shellDetection">Shell detection service for type detection.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> or <paramref name="shellDetection"/> is null.
    /// </exception>
    public ShellConfigurationService(
        ILogger<ShellConfigurationService> logger,
        IShellDetectionService shellDetection)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _shellDetection = shellDetection ?? throw new ArgumentNullException(nameof(shellDetection));
        _configurations = InitializeConfigurations();

        _logger.LogDebug("ShellConfigurationService initialized with {Count} configurations",
            _configurations.Count);
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellConfigurationService Implementation - Configuration Retrieval
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ShellConfiguration GetConfiguration(ShellType shellType)
    {
        _logger.LogDebug("Getting configuration for shell type: {Type}", shellType);

        if (_configurations.TryGetValue(shellType, out var config))
        {
            _logger.LogDebug("Found configuration for {Type}", shellType);
            return config;
        }

        // Fallback to Unknown configuration
        _logger.LogWarning("No specific configuration for {Type}, using Unknown fallback", shellType);
        return _configurations[ShellType.Unknown];
    }

    /// <inheritdoc />
    public ShellConfiguration GetConfiguration(string shellPath)
    {
        if (string.IsNullOrWhiteSpace(shellPath))
        {
            _logger.LogDebug("GetConfiguration called with null/empty path, using Unknown");
            return _configurations[ShellType.Unknown];
        }

        var shellType = _shellDetection.DetectShellType(shellPath);
        _logger.LogDebug("Detected shell type {Type} for path: {Path}", shellType, shellPath);

        return GetConfiguration(shellType);
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellConfigurationService Implementation - Command Formatting
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public string FormatChangeDirectoryCommand(ShellType shellType, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogDebug("FormatChangeDirectoryCommand called with empty path");
            return string.Empty;
        }

        var config = GetConfiguration(shellType);
        var escapedPath = EscapePath(shellType, path);

        var command = $"{config.ChangeDirectoryCommand} {escapedPath}";
        _logger.LogDebug("Formatted CD command for {Type}: {Command}", shellType, command);

        return command;
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellConfigurationService Implementation - OSC Escape Sequences
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public string? GetCwdReportingEscapeSequence(ShellType shellType, string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            _logger.LogDebug("GetCwdReportingEscapeSequence called with empty directory");
            return null;
        }

        var config = GetConfiguration(shellType);

        // Check for OSC 7 support (preferred)
        if (config.SupportsOsc7)
        {
            // OSC 7 format: \x1b]7;file://hostname/path\x07
            var hostname = Environment.MachineName;
            var encodedPath = Uri.EscapeDataString(directory).Replace("%2F", "/");

            var sequence = $"\x1b]7;file://{hostname}{encodedPath}\x07";
            _logger.LogDebug("Generated OSC 7 sequence for {Type}: length={Length}",
                shellType, sequence.Length);

            return sequence;
        }

        // Check for OSC 9 support (Windows Terminal style)
        if (config.SupportsOsc9)
        {
            // OSC 9;9 format: \x1b]9;9;"path"\x07
            var sequence = $"\x1b]9;9;\"{directory}\"\x07";
            _logger.LogDebug("Generated OSC 9 sequence for {Type}: length={Length}",
                shellType, sequence.Length);

            return sequence;
        }

        _logger.LogDebug("Shell type {Type} does not support CWD reporting", shellType);
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellConfigurationService Implementation - Environment
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Dictionary<string, string> GetDefaultEnvironment(ShellType shellType)
    {
        var config = GetConfiguration(shellType);

        // Return a copy to prevent mutation
        var env = new Dictionary<string, string>(config.DefaultEnvironment);
        _logger.LogDebug("Getting default environment for {Type}: {Count} variables",
            shellType, env.Count);

        return env;
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellConfigurationService Implementation - Shell Integration
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public string? GenerateShellIntegrationScript(ShellType shellType)
    {
        _logger.LogDebug("Generating shell integration script for {Type}", shellType);

        var script = shellType switch
        {
            // Bash: Use PROMPT_COMMAND hook
            ShellType.Bash => """
                # AIntern Shell Integration (Bash)
                __aintern_prompt_command() {
                    printf '\e]7;file://%s%s\a' "$HOSTNAME" "$PWD"
                }
                PROMPT_COMMAND="__aintern_prompt_command${PROMPT_COMMAND:+;$PROMPT_COMMAND}"
                """,

            // Zsh: Use chpwd hook
            ShellType.Zsh => """
                # AIntern Shell Integration (Zsh)
                __aintern_chpwd() {
                    printf '\e]7;file://%s%s\a' "$HOST" "$PWD"
                }
                autoload -Uz add-zsh-hook
                add-zsh-hook chpwd __aintern_chpwd
                __aintern_chpwd  # Report initial directory
                """,

            // Fish: Use PWD variable hook
            ShellType.Fish => """
                # AIntern Shell Integration (Fish)
                function __aintern_pwd_hook --on-variable PWD
                    printf '\e]7;file://%s%s\a' (hostname) $PWD
                end
                __aintern_pwd_hook  # Report initial directory
                """,

            // PowerShell: Override prompt function
            ShellType.PowerShell or ShellType.PowerShellCore => """
                # AIntern Shell Integration (PowerShell)
                function global:prompt {
                    $loc = (Get-Location).Path
                    Write-Host "`e]9;9;`"$loc`"`a" -NoNewline
                    return "PS $loc> "
                }
                """,

            // Nushell: Uses OSC 7, but requires config file modification
            ShellType.Nushell => """
                # AIntern Shell Integration (Nushell)
                # Add to config.nu:
                # $env.config.hooks.pre_prompt = { |_| print -n $"(ansi osc)7;file://($env.HOSTNAME)($env.PWD)(ansi st)" }
                """,

            _ => null
        };

        if (script != null)
        {
            _logger.LogDebug("Generated integration script for {Type}: {Length} chars",
                shellType, script.Length);
        }
        else
        {
            _logger.LogDebug("No integration script available for {Type}", shellType);
        }

        return script;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Path Escaping
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Escapes a path for use in shell commands.
    /// </summary>
    /// <param name="shellType">The target shell type.</param>
    /// <param name="path">The path to escape.</param>
    /// <returns>The escaped path.</returns>
    private static string EscapePath(ShellType shellType, string path)
    {
        // Check if path needs escaping
        var needsEscaping = path.Contains(' ') ||
                            path.Contains('\'') ||
                            path.Contains('"') ||
                            path.Contains('$') ||
                            path.Contains('!') ||
                            path.Contains('`') ||
                            path.Contains('(') ||
                            path.Contains(')');

        if (!needsEscaping)
        {
            return path;
        }

        return shellType switch
        {
            // Unix shells: Use single quotes, escape existing single quotes
            ShellType.Bash or ShellType.Zsh or ShellType.Fish or ShellType.Sh =>
                $"'{path.Replace("'", "'\\''")}'",

            // PowerShell: Use single quotes, double existing single quotes
            ShellType.PowerShell or ShellType.PowerShellCore =>
                $"'{path.Replace("'", "''")}'",

            // Cmd: Use double quotes
            ShellType.Cmd =>
                $"\"{path}\"",

            // Nushell: Use double quotes
            ShellType.Nushell =>
                $"\"{path.Replace("\"", "\\\"")}\"",

            // Default: Quote with double quotes
            _ => $"\"{path}\""
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Configuration Initialization
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the shell configuration dictionary.
    /// </summary>
    /// <returns>Dictionary of shell configurations.</returns>
    private static Dictionary<ShellType, ShellConfiguration> InitializeConfigurations()
    {
        var defaultEnv = new Dictionary<string, string>
        {
            ["TERM"] = "xterm-256color",
            ["COLORTERM"] = "truecolor"
        };

        return new Dictionary<ShellType, ShellConfiguration>
        {
            // ─────────────────────────────────────────────────────────────
            // Unknown / Fallback
            // ─────────────────────────────────────────────────────────────
            [ShellType.Unknown] = new ShellConfiguration
            {
                Type = ShellType.Unknown,
                ClearCommand = "clear",
                ChangeDirectoryCommand = "cd",
                PrintWorkingDirectoryCommand = "pwd",
                HistoryCommand = "history",
                ListDirectoryCommand = "ls",
                SupportsOsc7 = false,
                SupportsOsc9 = false,
                SupportsOsc133 = false,
                ProfileFiles = [],
                LoginArguments = string.Empty,
                InteractiveArguments = string.Empty,
                CommandSeparator = ";",
                LineContinuation = "\\",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$",
                SetEnvironmentVariableTemplate = "export {0}={1}",
                EscapeQuotesWithBackslash = true,
                DefaultEnvironment = new Dictionary<string, string>(defaultEnv)
            },

            // ─────────────────────────────────────────────────────────────
            // Bash
            // ─────────────────────────────────────────────────────────────
            [ShellType.Bash] = new ShellConfiguration
            {
                Type = ShellType.Bash,
                ClearCommand = "clear",
                ChangeDirectoryCommand = "cd",
                PrintWorkingDirectoryCommand = "pwd",
                HistoryCommand = "history",
                ListDirectoryCommand = "ls -la",
                SupportsOsc7 = true,
                SupportsOsc9 = false,
                SupportsOsc133 = true,
                ProfileFiles = ["~/.bashrc", "~/.bash_profile", "~/.profile"],
                LoginArguments = "--login",
                InteractiveArguments = "-i",
                CommandSeparator = ";",
                LineContinuation = "\\",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$",
                SetEnvironmentVariableTemplate = "export {0}={1}",
                EscapeQuotesWithBackslash = true,
                DefaultEnvironment = new Dictionary<string, string>(defaultEnv)
            },

            // ─────────────────────────────────────────────────────────────
            // Zsh
            // ─────────────────────────────────────────────────────────────
            [ShellType.Zsh] = new ShellConfiguration
            {
                Type = ShellType.Zsh,
                ClearCommand = "clear",
                ChangeDirectoryCommand = "cd",
                PrintWorkingDirectoryCommand = "pwd",
                HistoryCommand = "history",
                ListDirectoryCommand = "ls -la",
                SupportsOsc7 = true,
                SupportsOsc9 = false,
                SupportsOsc133 = true,
                ProfileFiles = ["~/.zshrc", "~/.zprofile", "~/.zshenv"],
                LoginArguments = "--login",
                InteractiveArguments = "-i",
                CommandSeparator = ";",
                LineContinuation = "\\",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$",
                SetEnvironmentVariableTemplate = "export {0}={1}",
                EscapeQuotesWithBackslash = true,
                DefaultEnvironment = new Dictionary<string, string>(defaultEnv)
            },

            // ─────────────────────────────────────────────────────────────
            // Sh
            // ─────────────────────────────────────────────────────────────
            [ShellType.Sh] = new ShellConfiguration
            {
                Type = ShellType.Sh,
                ClearCommand = "clear",
                ChangeDirectoryCommand = "cd",
                PrintWorkingDirectoryCommand = "pwd",
                HistoryCommand = "history",
                ListDirectoryCommand = "ls -la",
                SupportsOsc7 = false,
                SupportsOsc9 = false,
                SupportsOsc133 = false,
                ProfileFiles = ["~/.profile"],
                LoginArguments = string.Empty,
                InteractiveArguments = "-i",
                CommandSeparator = ";",
                LineContinuation = "\\",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$",
                SetEnvironmentVariableTemplate = "export {0}={1}",
                EscapeQuotesWithBackslash = true,
                DefaultEnvironment = new Dictionary<string, string>(defaultEnv)
            },

            // ─────────────────────────────────────────────────────────────
            // Fish
            // ─────────────────────────────────────────────────────────────
            [ShellType.Fish] = new ShellConfiguration
            {
                Type = ShellType.Fish,
                ClearCommand = "clear",
                ChangeDirectoryCommand = "cd",
                PrintWorkingDirectoryCommand = "pwd",
                HistoryCommand = "history",
                ListDirectoryCommand = "ls -la",
                SupportsOsc7 = true,
                SupportsOsc9 = false,
                SupportsOsc133 = true,
                ProfileFiles = ["~/.config/fish/config.fish"],
                LoginArguments = "--login",
                InteractiveArguments = "-i",
                CommandSeparator = "; and",
                LineContinuation = "\\",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$",
                SetEnvironmentVariableTemplate = "set -x {0} {1}",
                EscapeQuotesWithBackslash = true,
                DefaultEnvironment = new Dictionary<string, string>(defaultEnv)
            },

            // ─────────────────────────────────────────────────────────────
            // PowerShell (Windows)
            // ─────────────────────────────────────────────────────────────
            [ShellType.PowerShell] = new ShellConfiguration
            {
                Type = ShellType.PowerShell,
                ClearCommand = "Clear-Host",
                ChangeDirectoryCommand = "Set-Location",
                PrintWorkingDirectoryCommand = "(Get-Location).Path",
                HistoryCommand = "Get-History",
                ListDirectoryCommand = "Get-ChildItem",
                SupportsOsc7 = false,
                SupportsOsc9 = true,
                SupportsOsc133 = false,
                ProfileFiles = [
                    "~/Documents/PowerShell/Microsoft.PowerShell_profile.ps1",
                    "~/Documents/WindowsPowerShell/Microsoft.PowerShell_profile.ps1"
                ],
                LoginArguments = "-NoLogo",
                InteractiveArguments = "-NoExit",
                CommandSeparator = ";",
                LineContinuation = "`",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$env:",
                SetEnvironmentVariableTemplate = "$env:{0} = \"{1}\"",
                EscapeQuotesWithBackslash = false,
                DefaultEnvironment = new Dictionary<string, string>()
            },

            // ─────────────────────────────────────────────────────────────
            // PowerShell Core (Cross-platform)
            // ─────────────────────────────────────────────────────────────
            [ShellType.PowerShellCore] = new ShellConfiguration
            {
                Type = ShellType.PowerShellCore,
                ClearCommand = "Clear-Host",
                ChangeDirectoryCommand = "Set-Location",
                PrintWorkingDirectoryCommand = "(Get-Location).Path",
                HistoryCommand = "Get-History",
                ListDirectoryCommand = "Get-ChildItem",
                SupportsOsc7 = false,
                SupportsOsc9 = true,
                SupportsOsc133 = false,
                ProfileFiles = [
                    "~/.config/powershell/Microsoft.PowerShell_profile.ps1",
                    "~/Documents/PowerShell/Microsoft.PowerShell_profile.ps1"
                ],
                LoginArguments = "-NoLogo",
                InteractiveArguments = "-NoExit",
                CommandSeparator = ";",
                LineContinuation = "`",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$env:",
                SetEnvironmentVariableTemplate = "$env:{0} = \"{1}\"",
                EscapeQuotesWithBackslash = false,
                DefaultEnvironment = new Dictionary<string, string>(defaultEnv)
            },

            // ─────────────────────────────────────────────────────────────
            // Cmd (Windows Command Prompt)
            // ─────────────────────────────────────────────────────────────
            [ShellType.Cmd] = new ShellConfiguration
            {
                Type = ShellType.Cmd,
                ClearCommand = "cls",
                ChangeDirectoryCommand = "cd /d",
                PrintWorkingDirectoryCommand = "cd",
                HistoryCommand = "doskey /history",
                ListDirectoryCommand = "dir",
                SupportsOsc7 = false,
                SupportsOsc9 = false,
                SupportsOsc133 = false,
                ProfileFiles = [],
                LoginArguments = "/k",
                InteractiveArguments = string.Empty,
                CommandSeparator = "&",
                LineContinuation = "^",
                CommentPrefix = "REM",
                EnvironmentVariablePrefix = "%",
                SetEnvironmentVariableTemplate = "set {0}={1}",
                EscapeQuotesWithBackslash = false,
                DefaultEnvironment = new Dictionary<string, string>()
            },

            // ─────────────────────────────────────────────────────────────
            // Nushell
            // ─────────────────────────────────────────────────────────────
            [ShellType.Nushell] = new ShellConfiguration
            {
                Type = ShellType.Nushell,
                ClearCommand = "clear",
                ChangeDirectoryCommand = "cd",
                PrintWorkingDirectoryCommand = "pwd",
                HistoryCommand = "history",
                ListDirectoryCommand = "ls",
                SupportsOsc7 = true,
                SupportsOsc9 = false,
                SupportsOsc133 = false,
                ProfileFiles = ["~/.config/nushell/config.nu"],
                LoginArguments = "--login",
                InteractiveArguments = string.Empty,
                CommandSeparator = ";",
                LineContinuation = "\\",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$env.",
                SetEnvironmentVariableTemplate = "$env.{0} = \"{1}\"",
                EscapeQuotesWithBackslash = true,
                DefaultEnvironment = new Dictionary<string, string>(defaultEnv)
            },

            // ─────────────────────────────────────────────────────────────
            // WSL (Windows Subsystem for Linux)
            // ─────────────────────────────────────────────────────────────
            [ShellType.Wsl] = new ShellConfiguration
            {
                Type = ShellType.Wsl,
                ClearCommand = "clear",
                ChangeDirectoryCommand = "cd",
                PrintWorkingDirectoryCommand = "pwd",
                HistoryCommand = "history",
                ListDirectoryCommand = "ls -la",
                SupportsOsc7 = true,
                SupportsOsc9 = false,
                SupportsOsc133 = true,
                ProfileFiles = ["~/.bashrc", "~/.bash_profile"],
                LoginArguments = "--login",
                InteractiveArguments = "-i",
                CommandSeparator = ";",
                LineContinuation = "\\",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$",
                SetEnvironmentVariableTemplate = "export {0}={1}",
                EscapeQuotesWithBackslash = true,
                DefaultEnvironment = new Dictionary<string, string>(defaultEnv)
            },

            // ─────────────────────────────────────────────────────────────
            // Tcsh
            // ─────────────────────────────────────────────────────────────
            [ShellType.Tcsh] = new ShellConfiguration
            {
                Type = ShellType.Tcsh,
                ClearCommand = "clear",
                ChangeDirectoryCommand = "cd",
                PrintWorkingDirectoryCommand = "pwd",
                HistoryCommand = "history",
                ListDirectoryCommand = "ls -la",
                SupportsOsc7 = false,
                SupportsOsc9 = false,
                SupportsOsc133 = false,
                ProfileFiles = ["~/.tcshrc", "~/.cshrc"],
                LoginArguments = string.Empty,
                InteractiveArguments = string.Empty,
                CommandSeparator = ";",
                LineContinuation = "\\",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$",
                SetEnvironmentVariableTemplate = "setenv {0} {1}",
                EscapeQuotesWithBackslash = true,
                DefaultEnvironment = new Dictionary<string, string>(defaultEnv)
            },

            // ─────────────────────────────────────────────────────────────
            // Ksh (Korn Shell)
            // ─────────────────────────────────────────────────────────────
            [ShellType.Ksh] = new ShellConfiguration
            {
                Type = ShellType.Ksh,
                ClearCommand = "clear",
                ChangeDirectoryCommand = "cd",
                PrintWorkingDirectoryCommand = "pwd",
                HistoryCommand = "history",
                ListDirectoryCommand = "ls -la",
                SupportsOsc7 = false,
                SupportsOsc9 = false,
                SupportsOsc133 = false,
                ProfileFiles = ["~/.kshrc", "~/.profile"],
                LoginArguments = string.Empty,
                InteractiveArguments = "-i",
                CommandSeparator = ";",
                LineContinuation = "\\",
                CommentPrefix = "#",
                EnvironmentVariablePrefix = "$",
                SetEnvironmentVariableTemplate = "export {0}={1}",
                EscapeQuotesWithBackslash = true,
                DefaultEnvironment = new Dictionary<string, string>(defaultEnv)
            }
        };
    }
}
