namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL DETECTION SERVICE (v0.5.1d stub, full implementation in v0.5.1e) │
// │ Provides shell detection and enumeration for terminal sessions.         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents information about a detected shell.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d.</para>
/// <para>
/// Contains metadata about a shell installation including its path,
/// type, and default arguments for optimal startup.
/// </para>
/// </remarks>
public sealed record ShellInfo
{
    /// <summary>
    /// Gets the absolute path to the shell executable.
    /// </summary>
    /// <remarks>
    /// Examples: "/bin/zsh", "/bin/bash", "C:\Windows\System32\cmd.exe"
    /// </remarks>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the type of shell.
    /// </summary>
    public required ShellType ShellType { get; init; }

    /// <summary>
    /// Gets the default arguments for the shell.
    /// </summary>
    /// <remarks>
    /// Shell-specific defaults:
    /// <list type="bullet">
    ///   <item><description>bash/zsh: ["--login"]</description></item>
    ///   <item><description>PowerShell: ["-NoLogo"]</description></item>
    ///   <item><description>cmd.exe: []</description></item>
    /// </list>
    /// </remarks>
    public string[]? DefaultArguments { get; init; }

    /// <summary>
    /// Gets the detected version string, if available.
    /// </summary>
    /// <remarks>
    /// May be null if version detection failed or was not performed.
    /// </remarks>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the display name for the shell.
    /// </summary>
    /// <remarks>
    /// Returns the shell type name for display in UI.
    /// </remarks>
    public string DisplayName => ShellType.ToString();
}

/// <summary>
/// Enumeration of known shell types.
/// </summary>
/// <remarks>Added in v0.5.1d.</remarks>
public enum ShellType
{
    /// <summary>Unknown or undetected shell.</summary>
    Unknown,

    /// <summary>Bash (Bourne Again SHell).</summary>
    Bash,

    /// <summary>Zsh (Z Shell).</summary>
    Zsh,

    /// <summary>Sh (Bourne Shell).</summary>
    Sh,

    /// <summary>Fish (Friendly Interactive Shell).</summary>
    Fish,

    /// <summary>Windows Command Prompt (cmd.exe).</summary>
    Cmd,

    /// <summary>Windows PowerShell.</summary>
    PowerShell,

    /// <summary>PowerShell Core (pwsh).</summary>
    PowerShellCore
}

/// <summary>
/// Service for detecting and enumerating available shells.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d (stub). Full implementation in v0.5.1e.</para>
/// <para>
/// This interface defines the contract for shell detection. The terminal
/// service uses this to determine which shell to spawn for new sessions.
/// </para>
/// </remarks>
public interface IShellDetectionService
{
    /// <summary>
    /// Detects the user's default shell.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the default shell.</returns>
    /// <remarks>
    /// Detection strategy:
    /// <list type="bullet">
    ///   <item><description>macOS/Linux: SHELL environment variable or /bin/bash</description></item>
    ///   <item><description>Windows: COMSPEC environment variable or cmd.exe</description></item>
    /// </list>
    /// </remarks>
    Task<ShellInfo> DetectDefaultShellAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all available shells on the system.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of detected shells.</returns>
    /// <remarks>
    /// Checks common shell locations and verifies executability.
    /// </remarks>
    Task<IReadOnlyList<ShellInfo>> GetAvailableShellsAsync(CancellationToken cancellationToken = default);
}
