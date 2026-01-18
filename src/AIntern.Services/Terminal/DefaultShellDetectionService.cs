using System.Runtime.InteropServices;
using AIntern.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIntern.Services.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DEFAULT SHELL DETECTION SERVICE (v0.5.1d)                               │
// │ Basic implementation of shell detection for terminal sessions.          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Default implementation of shell detection service.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d.</para>
/// <para>
/// This provides a basic implementation that will be enhanced in v0.5.1e
/// with more sophisticated shell detection and version checking.
/// </para>
/// </remarks>
public sealed class DefaultShellDetectionService : IShellDetectionService
{
    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    private readonly ILogger<DefaultShellDetectionService> _logger;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new default shell detection service.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public DefaultShellDetectionService(ILogger<DefaultShellDetectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellDetectionService Implementation
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<ShellInfo> DetectDefaultShellAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Detecting default shell for platform: {Platform}", RuntimeInformation.OSDescription);

        ShellInfo shell;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            shell = DetectWindowsDefaultShell();
        }
        else
        {
            shell = DetectUnixDefaultShell();
        }

        _logger.LogInformation("Detected default shell: {ShellPath} ({ShellType})",
            shell.Path, shell.ShellType);

        return Task.FromResult(shell);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ShellInfo>> GetAvailableShellsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting available shells for platform: {Platform}", RuntimeInformation.OSDescription);

        List<ShellInfo> shells;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            shells = GetWindowsShells();
        }
        else
        {
            shells = GetUnixShells();
        }

        _logger.LogDebug("Found {Count} available shells", shells.Count);
        return Task.FromResult<IReadOnlyList<ShellInfo>>(shells.AsReadOnly());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Windows
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the default shell on Windows.
    /// </summary>
    private ShellInfo DetectWindowsDefaultShell()
    {
        // Check for PowerShell Core first
        var pwshPath = FindExecutable("pwsh.exe");
        if (pwshPath != null)
        {
            return new ShellInfo
            {
                Path = pwshPath,
                ShellType = ShellType.PowerShellCore,
                DefaultArguments = ["-NoLogo"]
            };
        }

        // Fall back to Windows PowerShell
        var powershellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");

        if (File.Exists(powershellPath))
        {
            return new ShellInfo
            {
                Path = powershellPath,
                ShellType = ShellType.PowerShell,
                DefaultArguments = ["-NoLogo"]
            };
        }

        // Last resort: cmd.exe
        var comspec = Environment.GetEnvironmentVariable("COMSPEC");
        var cmdPath = comspec ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

        return new ShellInfo
        {
            Path = cmdPath,
            ShellType = ShellType.Cmd,
            DefaultArguments = []
        };
    }

    /// <summary>
    /// Gets available shells on Windows.
    /// </summary>
    private List<ShellInfo> GetWindowsShells()
    {
        var shells = new List<ShellInfo>();

        // cmd.exe
        var comspec = Environment.GetEnvironmentVariable("COMSPEC");
        var cmdPath = comspec ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

        if (File.Exists(cmdPath))
        {
            shells.Add(new ShellInfo
            {
                Path = cmdPath,
                ShellType = ShellType.Cmd,
                DefaultArguments = []
            });
        }

        // Windows PowerShell
        var powershellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");

        if (File.Exists(powershellPath))
        {
            shells.Add(new ShellInfo
            {
                Path = powershellPath,
                ShellType = ShellType.PowerShell,
                DefaultArguments = ["-NoLogo"]
            });
        }

        // PowerShell Core
        var pwshPath = FindExecutable("pwsh.exe");
        if (pwshPath != null)
        {
            shells.Add(new ShellInfo
            {
                Path = pwshPath,
                ShellType = ShellType.PowerShellCore,
                DefaultArguments = ["-NoLogo"]
            });
        }

        // Git Bash
        var gitBashPath = @"C:\Program Files\Git\bin\bash.exe";
        if (File.Exists(gitBashPath))
        {
            shells.Add(new ShellInfo
            {
                Path = gitBashPath,
                ShellType = ShellType.Bash,
                DefaultArguments = ["--login"]
            });
        }

        return shells;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Unix
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the default shell on Unix-like systems.
    /// </summary>
    private ShellInfo DetectUnixDefaultShell()
    {
        // Check SHELL environment variable
        var shellEnv = Environment.GetEnvironmentVariable("SHELL");

        if (!string.IsNullOrEmpty(shellEnv) && File.Exists(shellEnv))
        {
            var shellType = DetermineShellType(shellEnv);
            return new ShellInfo
            {
                Path = shellEnv,
                ShellType = shellType,
                DefaultArguments = GetDefaultArguments(shellType)
            };
        }

        // Common shell locations in order of preference
        var shellPaths = new[]
        {
            "/bin/zsh",
            "/bin/bash",
            "/bin/sh"
        };

        foreach (var path in shellPaths)
        {
            if (File.Exists(path))
            {
                var shellType = DetermineShellType(path);
                return new ShellInfo
                {
                    Path = path,
                    ShellType = shellType,
                    DefaultArguments = GetDefaultArguments(shellType)
                };
            }
        }

        // Last resort
        return new ShellInfo
        {
            Path = "/bin/sh",
            ShellType = ShellType.Sh,
            DefaultArguments = []
        };
    }

    /// <summary>
    /// Gets available shells on Unix-like systems.
    /// </summary>
    private List<ShellInfo> GetUnixShells()
    {
        var shells = new List<ShellInfo>();

        // Common shell locations
        var shellPaths = new Dictionary<string, ShellType>
        {
            { "/bin/bash", ShellType.Bash },
            { "/bin/zsh", ShellType.Zsh },
            { "/bin/sh", ShellType.Sh },
            { "/usr/bin/fish", ShellType.Fish },
            { "/opt/homebrew/bin/fish", ShellType.Fish },
        };

        foreach (var (path, type) in shellPaths)
        {
            if (File.Exists(path))
            {
                shells.Add(new ShellInfo
                {
                    Path = path,
                    ShellType = type,
                    DefaultArguments = GetDefaultArguments(type)
                });
            }
        }

        return shells;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Determines shell type from executable path.
    /// </summary>
    private static ShellType DetermineShellType(string path)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();

        return name switch
        {
            "bash" or "bash.exe" => ShellType.Bash,
            "zsh" or "zsh.exe" => ShellType.Zsh,
            "sh" => ShellType.Sh,
            "fish" or "fish.exe" => ShellType.Fish,
            "cmd.exe" or "cmd" => ShellType.Cmd,
            "powershell.exe" or "powershell" => ShellType.PowerShell,
            "pwsh.exe" or "pwsh" => ShellType.PowerShellCore,
            _ => ShellType.Unknown
        };
    }

    /// <summary>
    /// Gets default arguments for a shell type.
    /// </summary>
    private static string[]? GetDefaultArguments(ShellType shellType)
    {
        return shellType switch
        {
            ShellType.Bash => ["--login"],
            ShellType.Zsh => ["--login"],
            ShellType.PowerShell => ["-NoLogo"],
            ShellType.PowerShellCore => ["-NoLogo"],
            _ => null
        };
    }

    /// <summary>
    /// Finds an executable in the system PATH.
    /// </summary>
    private static string? FindExecutable(string execName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            return null;
        }

        var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

        foreach (var dir in pathEnv.Split(separator))
        {
            var fullPath = Path.Combine(dir, execName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}
