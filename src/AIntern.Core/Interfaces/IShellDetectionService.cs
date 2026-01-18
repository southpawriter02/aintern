namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL DETECTION SERVICE INTERFACE (v0.5.1e)                             │
// │ Cross-platform shell discovery and identification.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for detecting available shells on the system.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d. Enhanced in v0.5.1e.</para>
/// <para>
/// This service provides cross-platform shell detection capabilities:
/// <list type="bullet">
///   <item>Automatic default shell detection based on OS configuration</item>
///   <item>Enumeration of all available shells</item>
///   <item>Shell validation and version detection</item>
/// </list>
/// </para>
/// <para>
/// Implementations should cache results to avoid repeated filesystem
/// and process operations. Detection strategies vary by platform:
/// </para>
/// <para>
/// <b>Windows:</b> PowerShell Core → Windows PowerShell → cmd.exe<br/>
/// <b>macOS:</b> $SHELL → dscl → /bin/zsh<br/>
/// <b>Linux:</b> $SHELL → /etc/passwd → /bin/bash
/// </para>
/// </remarks>
public interface IShellDetectionService
{
    // ─────────────────────────────────────────────────────────────────────
    // Default Shell Detection
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the user's default shell.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the default shell.</returns>
    /// <remarks>
    /// <para>
    /// This method always returns a valid <see cref="ShellInfo"/>. If
    /// detection fails, a platform-appropriate fallback is returned.
    /// </para>
    /// <para>
    /// Results are cached after the first call for the lifetime of the
    /// service instance.
    /// </para>
    /// </remarks>
    Task<ShellInfo> DetectDefaultShellAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the path to the default shell executable.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Full path to the default shell executable.</returns>
    /// <remarks>
    /// <para>
    /// Convenience method that returns just the path from
    /// <see cref="DetectDefaultShellAsync"/>. Useful when only the
    /// path is needed, not the full shell metadata.
    /// </para>
    /// <para>
    /// Detection strategies by platform:
    /// <list type="bullet">
    ///   <item>Windows: PowerShell Core → Windows PowerShell → cmd.exe</item>
    ///   <item>macOS: $SHELL → dscl → /bin/zsh</item>
    ///   <item>Linux: $SHELL → /etc/passwd → /bin/bash</item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<string> GetDefaultShellAsync(CancellationToken cancellationToken = default);

    // ─────────────────────────────────────────────────────────────────────
    // Shell Enumeration
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets a list of all available shells on the system.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of detected shells, ordered with default shell first.</returns>
    /// <remarks>
    /// <para>
    /// Results are cached after the first call. The cache persists for
    /// the lifetime of the service instance.
    /// </para>
    /// <para>
    /// The returned list is ordered with the default shell first, followed
    /// by other shells in alphabetical order by name.
    /// </para>
    /// <para>
    /// Exactly one shell in the list will have
    /// <see cref="ShellInfo.IsDefault"/> set to true.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<ShellInfo>> GetAvailableShellsAsync(CancellationToken cancellationToken = default);

    // ─────────────────────────────────────────────────────────────────────
    // Shell Validation
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks if a shell is available at the specified path.
    /// </summary>
    /// <param name="path">Full path to the shell executable.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the shell exists and is executable; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method verifies that:
    /// <list type="bullet">
    ///   <item>The file exists at the specified path</item>
    ///   <item>The file is executable (responds to --version or similar)</item>
    /// </list>
    /// </para>
    /// <para>
    /// A timeout of 5 seconds is applied to the validation process.
    /// </para>
    /// </remarks>
    Task<bool> IsShellAvailableAsync(string path, CancellationToken cancellationToken = default);
}

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL INFO RECORD (v0.5.1e)                                            │
// │ Metadata about a detected shell installation.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Information about an available shell.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d. Enhanced in v0.5.1e.</para>
/// <para>
/// This record contains metadata about a detected shell, including
/// its display name, path, type classification, version information,
/// and whether it is the system default.
/// </para>
/// </remarks>
public sealed record ShellInfo
{
    // ─────────────────────────────────────────────────────────────────────
    // Required Properties
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the display name for the shell (e.g., "PowerShell", "Bash", "Zsh").
    /// </summary>
    /// <remarks>
    /// This is a user-friendly name suitable for display in UI elements
    /// like dropdown menus or tab labels.
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the absolute path to the shell executable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the absolute path that should be passed to the PTY
    /// when spawning a new terminal session.
    /// </para>
    /// <para>
    /// Examples: "/bin/zsh", "/bin/bash", "C:\Windows\System32\cmd.exe"
    /// </para>
    /// </remarks>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the type of shell.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used to determine shell-specific behaviors such as:
    /// <list type="bullet">
    ///   <item>Default arguments to pass</item>
    ///   <item>Clear screen command</item>
    ///   <item>Working directory change command</item>
    /// </list>
    /// </para>
    /// </remarks>
    public required ShellType ShellType { get; init; }

    // ─────────────────────────────────────────────────────────────────────
    // Optional Properties
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the default arguments for the shell.
    /// </summary>
    /// <remarks>
    /// Shell-specific defaults:
    /// <list type="bullet">
    ///   <item><description>bash/zsh: ["--login"]</description></item>
    ///   <item><description>fish: None</description></item>
    ///   <item><description>PowerShell: ["-NoLogo"]</description></item>
    ///   <item><description>cmd.exe: []</description></item>
    /// </list>
    /// </remarks>
    public string[]? DefaultArguments { get; init; }

    /// <summary>
    /// Gets the version string if available (e.g., "7.4.0", "5.2.15").
    /// </summary>
    /// <remarks>
    /// Obtained by running the shell with --version or equivalent.
    /// May be null if version detection fails or is not supported.
    /// </remarks>
    public string? Version { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is the system default shell.
    /// </summary>
    /// <remarks>
    /// Exactly one shell in the list returned by
    /// <see cref="IShellDetectionService.GetAvailableShellsAsync"/>
    /// will have this set to true.
    /// </remarks>
    public bool IsDefault { get; init; }

    // ─────────────────────────────────────────────────────────────────────
    // Display
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a display string for this shell.
    /// </summary>
    /// <returns>Name with version if available.</returns>
    public override string ToString() =>
        Version != null ? $"{Name} ({Version})" : Name;
}

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL TYPE ENUM (v0.5.1e)                                              │
// │ Classification of known shell types.                                   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Known shell types.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d. Enhanced in v0.5.1e.</para>
/// <para>
/// This enum identifies the type of shell for determining
/// shell-specific behaviors and commands.
/// </para>
/// </remarks>
public enum ShellType
{
    /// <summary>
    /// Unknown or unrecognized shell type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// GNU Bourne-Again Shell (bash).
    /// </summary>
    /// <remarks>
    /// Common on Linux and available on macOS/Windows via Git.
    /// </remarks>
    Bash,

    /// <summary>
    /// Z Shell (zsh).
    /// </summary>
    /// <remarks>
    /// Default on macOS since Catalina (10.15).
    /// </remarks>
    Zsh,

    /// <summary>
    /// Bourne Shell (sh).
    /// </summary>
    /// <remarks>
    /// POSIX-compliant shell, often a symlink to bash or dash.
    /// </remarks>
    Sh,

    /// <summary>
    /// Friendly Interactive Shell (fish).
    /// </summary>
    /// <remarks>
    /// Known for user-friendly features and syntax highlighting.
    /// </remarks>
    Fish,

    /// <summary>
    /// Windows Command Prompt (cmd.exe).
    /// </summary>
    /// <remarks>
    /// Legacy Windows shell.
    /// </remarks>
    Cmd,

    /// <summary>
    /// Windows PowerShell (powershell.exe).
    /// </summary>
    /// <remarks>
    /// Windows-only PowerShell, pre-installed on Windows.
    /// </remarks>
    PowerShell,

    /// <summary>
    /// PowerShell Core (pwsh).
    /// </summary>
    /// <remarks>
    /// Cross-platform PowerShell, available on Windows, macOS, and Linux.
    /// </remarks>
    PowerShellCore,

    /// <summary>
    /// Nushell (nu).
    /// </summary>
    /// <remarks>
    /// Modern shell with structured data support.
    /// </remarks>
    Nushell
}
