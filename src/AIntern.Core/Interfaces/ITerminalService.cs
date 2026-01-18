namespace AIntern.Core.Interfaces;

using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TERMINAL SERVICE INTERFACE (v0.5.1d)                                    │
// │ Core abstraction for terminal session management and PTY operations.   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Core service interface for terminal session management.
/// Provides PTY-based terminal emulation with cross-platform support.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d.</para>
/// <para>
/// This service manages the lifecycle of terminal sessions, including:
/// <list type="bullet">
///   <item><description>Creating and destroying sessions</description></item>
///   <item><description>Routing input to shell processes</description></item>
///   <item><description>Parsing and buffering shell output</description></item>
///   <item><description>Sending terminal signals</description></item>
///   <item><description>Resizing terminal dimensions</description></item>
/// </list>
/// </para>
/// <para>
/// Implementations must be thread-safe and properly dispose of all
/// PTY resources when the service is disposed.
/// </para>
/// </remarks>
public interface ITerminalService : IAsyncDisposable
{
    // ─────────────────────────────────────────────────────────────────────
    // Properties
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets all active terminal sessions.
    /// </summary>
    /// <remarks>
    /// Returns a snapshot of current sessions. The collection may change
    /// between calls as sessions are created or closed.
    /// </remarks>
    IReadOnlyList<TerminalSession> Sessions { get; }

    /// <summary>
    /// Gets the currently active (focused) terminal session, if any.
    /// </summary>
    /// <remarks>
    /// Only one session can be active at a time. Returns null if no
    /// sessions exist or none is currently focused.
    /// </remarks>
    TerminalSession? ActiveSession { get; }

    // ─────────────────────────────────────────────────────────────────────
    // Session Lifecycle Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new terminal session with the specified options.
    /// </summary>
    /// <param name="options">Configuration for the new session.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The newly created terminal session.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the shell executable cannot be found or started.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is cancelled.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The session starts in the <see cref="TerminalSessionState.Starting"/>
    /// state and transitions to <see cref="TerminalSessionState.Running"/>
    /// once the shell process is successfully spawned.
    /// </para>
    /// <para>
    /// If options is null, default options will be used with auto-detected
    /// shell and default terminal dimensions (80x24).
    /// </para>
    /// </remarks>
    Task<TerminalSession> CreateSessionAsync(
        TerminalSessionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an existing terminal session.
    /// </summary>
    /// <param name="sessionId">The ID of the session to close.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the session was found and closed; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method gracefully terminates the shell process by sending
    /// SIGTERM (or equivalent on Windows), then forcefully kills it
    /// after a timeout if necessary.
    /// </para>
    /// <para>
    /// The session transitions through <see cref="TerminalSessionState.Closing"/>
    /// before being removed from the sessions collection.
    /// </para>
    /// </remarks>
    Task<bool> CloseSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active (focused) terminal session.
    /// </summary>
    /// <param name="sessionId">The ID of the session to activate.</param>
    /// <returns>True if the session was found and activated; false otherwise.</returns>
    bool SetActiveSession(Guid sessionId);

    // ─────────────────────────────────────────────────────────────────────
    // I/O Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes input data to a terminal session.
    /// </summary>
    /// <param name="sessionId">The target session ID.</param>
    /// <param name="data">The data to write (typically user keystrokes).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the data was written; false if session not found.</returns>
    /// <remarks>
    /// <para>
    /// The data is written directly to the PTY input stream, which forwards
    /// it to the shell process. This supports both regular text and special
    /// key sequences (e.g., escape codes for arrow keys).
    /// </para>
    /// <para>
    /// Writing to a session that is not in the Running state will return false.
    /// </para>
    /// </remarks>
    Task<bool> WriteInputAsync(
        Guid sessionId,
        string data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resizes the terminal dimensions for a session.
    /// </summary>
    /// <param name="sessionId">The target session ID.</param>
    /// <param name="columns">New column count (width in characters).</param>
    /// <param name="rows">New row count (height in characters).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if resized successfully; false if session not found.</returns>
    /// <remarks>
    /// <para>
    /// This sends a SIGWINCH signal to the shell process, informing it of
    /// the new terminal dimensions. Applications running in the terminal
    /// (like vim, htop) will redraw accordingly.
    /// </para>
    /// <para>
    /// The session's <see cref="TerminalSession.Size"/> property is updated.
    /// </para>
    /// </remarks>
    Task<bool> ResizeAsync(
        Guid sessionId,
        int columns,
        int rows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a terminal signal to a session.
    /// </summary>
    /// <param name="sessionId">The target session ID.</param>
    /// <param name="signal">The signal to send.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if signal sent; false if session not found.</returns>
    /// <remarks>
    /// <para>
    /// Signals are translated to platform-specific equivalents:
    /// <list type="bullet">
    ///   <item><description>Interrupt → SIGINT (Ctrl+C)</description></item>
    ///   <item><description>Terminate → SIGTERM</description></item>
    ///   <item><description>Kill → SIGKILL</description></item>
    ///   <item><description>Suspend → SIGTSTP (Ctrl+Z)</description></item>
    ///   <item><description>Continue → SIGCONT</description></item>
    ///   <item><description>EndOfFile → Writes Ctrl+D to input</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<bool> SendSignalAsync(
        Guid sessionId,
        TerminalSignal signal,
        CancellationToken cancellationToken = default);

    // ─────────────────────────────────────────────────────────────────────
    // Buffer Access
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the terminal buffer for a session.
    /// </summary>
    /// <param name="sessionId">The target session ID.</param>
    /// <returns>The terminal buffer, or null if session not found.</returns>
    /// <remarks>
    /// <para>
    /// The buffer contains all terminal content including scrollback history.
    /// Use this for rendering the terminal display and accessing terminal state.
    /// </para>
    /// <para>
    /// The returned buffer is the live buffer associated with the session.
    /// Changes from shell output will be reflected in subsequent reads.
    /// </para>
    /// </remarks>
    TerminalBuffer? GetBuffer(Guid sessionId);

    // ─────────────────────────────────────────────────────────────────────
    // Convenience Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Changes the working directory for a session.
    /// </summary>
    /// <param name="sessionId">The target session ID.</param>
    /// <param name="path">The new working directory path.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if command was sent; false if session not found.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method that writes "cd {path}\n" to the session.
    /// The path is properly quoted to handle spaces and special characters.
    /// </para>
    /// <para>
    /// Note: This does not verify that the directory change succeeded.
    /// The shell will output an error if the path is invalid.
    /// </para>
    /// </remarks>
    Task<bool> ChangeDirectoryAsync(
        Guid sessionId,
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command in a terminal session.
    /// </summary>
    /// <param name="sessionId">The target session ID.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if command was sent; false if session not found.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method that writes "{command}\n" to the session.
    /// The command is executed exactly as provided without modification.
    /// </para>
    /// <para>
    /// For commands that require elevated privileges, the shell will prompt
    /// for credentials as normal.
    /// </para>
    /// </remarks>
    Task<bool> ExecuteCommandAsync(
        Guid sessionId,
        string command,
        CancellationToken cancellationToken = default);

    // ─────────────────────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when output is received from a terminal session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Output is delivered as raw data. The event args contain both the
    /// raw output and the session ID.
    /// </para>
    /// <para>
    /// This event may be raised from a background thread. Handlers should
    /// marshal to the UI thread if necessary.
    /// </para>
    /// </remarks>
    event EventHandler<TerminalOutputEventArgs>? OutputReceived;

    /// <summary>
    /// Raised when a new terminal session is created.
    /// </summary>
    event EventHandler<TerminalSessionEventArgs>? SessionCreated;

    /// <summary>
    /// Raised when a terminal session is closed.
    /// </summary>
    /// <remarks>
    /// This is raised after the session has been removed from the
    /// Sessions collection and all resources have been cleaned up.
    /// </remarks>
    event EventHandler<TerminalSessionEventArgs>? SessionClosed;

    /// <summary>
    /// Raised when a session's state changes.
    /// </summary>
    /// <remarks>
    /// State transitions follow this pattern:
    /// Starting → Running → (Exited | Error) → Closing
    /// </remarks>
    event EventHandler<TerminalSessionStateEventArgs>? SessionStateChanged;

    /// <summary>
    /// Raised when the terminal title changes (via escape sequence).
    /// </summary>
    /// <remarks>
    /// Applications can set the terminal title using OSC escape sequences.
    /// This event allows the UI to update tab titles accordingly.
    /// </remarks>
    event EventHandler<TerminalTitleEventArgs>? TitleChanged;

    /// <summary>
    /// Raised when the terminal bell is triggered (BEL character).
    /// </summary>
    event EventHandler<TerminalBellEventArgs>? BellTriggered;
}

// ─────────────────────────────────────────────────────────────────────────────
// Supporting Types
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Options for creating a new terminal session.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d.</para>
/// <para>
/// All properties have sensible defaults. Pass null to
/// <see cref="ITerminalService.CreateSessionAsync"/> to use defaults.
/// </para>
/// </remarks>
public sealed record TerminalSessionOptions
{
    /// <summary>
    /// The shell executable path. If null, auto-detects the default shell.
    /// </summary>
    /// <remarks>
    /// Auto-detection uses:
    /// <list type="bullet">
    ///   <item><description>Windows: %COMSPEC% or cmd.exe</description></item>
    ///   <item><description>macOS/Linux: $SHELL or /bin/bash</description></item>
    /// </list>
    /// </remarks>
    public string? ShellPath { get; init; }

    /// <summary>
    /// Arguments to pass to the shell. If null, uses shell-appropriate defaults.
    /// </summary>
    /// <remarks>
    /// Default arguments:
    /// <list type="bullet">
    ///   <item><description>PowerShell: -NoLogo</description></item>
    ///   <item><description>cmd.exe: (none)</description></item>
    ///   <item><description>bash/zsh: --login</description></item>
    /// </list>
    /// </remarks>
    public string[]? Arguments { get; init; }

    /// <summary>
    /// Initial working directory. If null, uses the current directory.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Initial terminal width in columns. Default is 80.
    /// </summary>
    /// <remarks>
    /// Common values: 80 (standard), 120 (wide), 132 (DEC VT).
    /// Must be at least 1.
    /// </remarks>
    public int Columns { get; init; } = 80;

    /// <summary>
    /// Initial terminal height in rows. Default is 24.
    /// </summary>
    /// <remarks>
    /// Common values: 24 (standard), 25 (DOS), 43/50 (EGA/VGA).
    /// Must be at least 1.
    /// </remarks>
    public int Rows { get; init; } = 24;

    /// <summary>
    /// Additional environment variables to set.
    /// </summary>
    /// <remarks>
    /// These are merged with inherited environment variables.
    /// Existing variables with the same name are overwritten.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }

    /// <summary>
    /// User-friendly name for the session (e.g., for tab titles).
    /// If null, uses the shell name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Scrollback buffer size in lines. Default is 10000.
    /// </summary>
    /// <remarks>
    /// Larger values consume more memory but preserve more history.
    /// Set to 0 to disable scrollback (not recommended).
    /// </remarks>
    public int ScrollbackLines { get; init; } = 10000;

    /// <summary>
    /// Creates default options with auto-detected shell.
    /// </summary>
    public static TerminalSessionOptions Default => new();
}

/// <summary>
/// Terminal signals that can be sent to a session.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d.</para>
/// <para>
/// These map to POSIX signals on Unix-like systems and equivalent
/// operations on Windows.
/// </para>
/// </remarks>
public enum TerminalSignal
{
    /// <summary>
    /// Interrupt signal (SIGINT). Equivalent to Ctrl+C.
    /// Typically causes the foreground process to terminate.
    /// </summary>
    Interrupt,

    /// <summary>
    /// Terminate signal (SIGTERM). Requests graceful termination.
    /// Processes should clean up and exit.
    /// </summary>
    Terminate,

    /// <summary>
    /// Kill signal (SIGKILL). Forces immediate termination.
    /// Cannot be caught or ignored by the process.
    /// </summary>
    Kill,

    /// <summary>
    /// Suspend signal (SIGTSTP). Equivalent to Ctrl+Z.
    /// Suspends the foreground process.
    /// </summary>
    Suspend,

    /// <summary>
    /// Continue signal (SIGCONT). Resumes a suspended process.
    /// </summary>
    Continue,

    /// <summary>
    /// End of file. Writes Ctrl+D to input stream.
    /// Signals end of input to the shell.
    /// </summary>
    EndOfFile
}

// ─────────────────────────────────────────────────────────────────────────────
// Event Args Classes
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Event args for terminal output events.
/// </summary>
/// <remarks>Added in v0.5.1d.</remarks>
public sealed class TerminalOutputEventArgs : EventArgs
{
    /// <summary>
    /// The session that produced the output.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// The raw output data received from the PTY.
    /// </summary>
    /// <remarks>
    /// This data has not been parsed and may contain ANSI escape sequences.
    /// For parsed content, access the session's terminal buffer.
    /// </remarks>
    public required string Data { get; init; }
}

/// <summary>
/// Event args for session lifecycle events.
/// </summary>
/// <remarks>Added in v0.5.1d.</remarks>
public sealed class TerminalSessionEventArgs : EventArgs
{
    /// <summary>
    /// The affected session.
    /// </summary>
    public required TerminalSession Session { get; init; }
}

/// <summary>
/// Event args for session state change events.
/// </summary>
/// <remarks>Added in v0.5.1d.</remarks>
public sealed class TerminalSessionStateEventArgs : EventArgs
{
    /// <summary>
    /// The affected session ID.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// The previous state.
    /// </summary>
    public required TerminalSessionState OldState { get; init; }

    /// <summary>
    /// The new state.
    /// </summary>
    public required TerminalSessionState NewState { get; init; }

    /// <summary>
    /// Exit code if the session exited, null otherwise.
    /// </summary>
    public int? ExitCode { get; init; }
}

/// <summary>
/// Event args for terminal title change events.
/// </summary>
/// <remarks>Added in v0.5.1d.</remarks>
public sealed class TerminalTitleEventArgs : EventArgs
{
    /// <summary>
    /// The session whose title changed.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// The new title.
    /// </summary>
    public required string Title { get; init; }
}

/// <summary>
/// Event args for terminal bell events.
/// </summary>
/// <remarks>Added in v0.5.1d.</remarks>
public sealed class TerminalBellEventArgs : EventArgs
{
    /// <summary>
    /// The session that triggered the bell.
    /// </summary>
    public required Guid SessionId { get; init; }
}
