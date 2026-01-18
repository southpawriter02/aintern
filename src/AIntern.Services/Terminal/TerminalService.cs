using System.Collections.Concurrent;
using System.Text;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Core.Terminal;
using Microsoft.Extensions.Logging;
using Pty.Net;

namespace AIntern.Services.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TERMINAL SERVICE (v0.5.1d)                                              │
// │ PTY-based implementation of ITerminalService.                           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// PTY-based implementation of the terminal service.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d.</para>
/// <para>
/// Uses Pty.Net for cross-platform pseudo-terminal support:
/// <list type="bullet">
///   <item><description>Windows: ConPTY (Windows 10 1809+) or WinPTY</description></item>
///   <item><description>macOS/Linux: Native PTY via fork/exec</description></item>
/// </list>
/// </para>
/// <para>
/// This service is thread-safe and manages multiple terminal sessions
/// concurrently. Data is received via the PtyData event.
/// </para>
/// </remarks>
public sealed class TerminalService : ITerminalService
{
    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    private readonly IShellDetectionService _shellDetection;
    private readonly ILogger<TerminalService> _logger;

    /// <summary>Thread-safe dictionary of active sessions.</summary>
    private readonly ConcurrentDictionary<Guid, TerminalSessionContext> _sessions = new();

    /// <summary>Semaphore for serializing session creation.</summary>
    private readonly SemaphoreSlim _sessionLock = new(1, 1);

    /// <summary>Tracks disposal state.</summary>
    private volatile bool _disposed;

    /// <summary>Currently active session ID.</summary>
    private Guid _activeSessionId = Guid.Empty;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new terminal service.
    /// </summary>
    /// <param name="shellDetection">Shell detection service for finding shells.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if shellDetection or logger is null.
    /// </exception>
    public TerminalService(
        IShellDetectionService shellDetection,
        ILogger<TerminalService> logger)
    {
        _shellDetection = shellDetection ?? throw new ArgumentNullException(nameof(shellDetection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("TerminalService created");
    }

    // ─────────────────────────────────────────────────────────────────────
    // ITerminalService Properties
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<TerminalSession> Sessions
    {
        get
        {
            ThrowIfDisposed();
            // Return snapshot to prevent concurrent modification issues
            return _sessions.Values
                .Select(ctx => ctx.Session)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <inheritdoc />
    public TerminalSession? ActiveSession
    {
        get
        {
            ThrowIfDisposed();
            if (_activeSessionId == Guid.Empty)
            {
                return null;
            }

            return _sessions.TryGetValue(_activeSessionId, out var ctx)
                ? ctx.Session
                : null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // ITerminalService Events
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public event EventHandler<TerminalOutputEventArgs>? OutputReceived;

    /// <inheritdoc />
    public event EventHandler<TerminalSessionEventArgs>? SessionCreated;

    /// <inheritdoc />
    public event EventHandler<TerminalSessionEventArgs>? SessionClosed;

    /// <inheritdoc />
    public event EventHandler<TerminalSessionStateEventArgs>? SessionStateChanged;

    /// <inheritdoc />
    public event EventHandler<TerminalTitleEventArgs>? TitleChanged;

    /// <inheritdoc />
    public event EventHandler<TerminalBellEventArgs>? BellTriggered;

    // ─────────────────────────────────────────────────────────────────────
    // Session Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<TerminalSession> CreateSessionAsync(
        TerminalSessionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        options ??= TerminalSessionOptions.Default;

        _logger.LogDebug("Creating new terminal session with options: Columns={Columns}, Rows={Rows}",
            options.Columns, options.Rows);

        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            // ─────────────────────────────────────────────────────────────
            // Step 1: Detect shell if not specified
            // ─────────────────────────────────────────────────────────────
            var shellInfo = await _shellDetection.DetectDefaultShellAsync(cancellationToken);
            var shellPath = options.ShellPath ?? shellInfo.Path;

            _logger.LogDebug("Using shell: {ShellPath}", shellPath);

            // ─────────────────────────────────────────────────────────────
            // Step 2: Create session model
            // ─────────────────────────────────────────────────────────────
            var sessionId = Guid.NewGuid();
            var session = new TerminalSession
            {
                Id = sessionId,
                Name = options.Name ?? Path.GetFileNameWithoutExtension(shellPath),
                ShellPath = shellPath,
                WorkingDirectory = options.WorkingDirectory ?? Environment.CurrentDirectory,
                Size = new TerminalSize(
                    Math.Max(1, options.Columns),
                    Math.Max(1, options.Rows)),
                State = TerminalSessionState.Starting,
                CreatedAt = DateTime.UtcNow
            };

            // ─────────────────────────────────────────────────────────────
            // Step 3: Create terminal buffer and ANSI parser
            // ─────────────────────────────────────────────────────────────
            var buffer = new TerminalBuffer(
                session.Size.Columns,
                session.Size.Rows,
                options.ScrollbackLines);

            var parser = new AnsiParser(buffer);
            ConfigureParserCallbacks(parser, sessionId);

            // ─────────────────────────────────────────────────────────────
            // Step 4: Spawn PTY process using Pty.Net API
            // ─────────────────────────────────────────────────────────────
            _logger.LogInformation("Spawning PTY process: {ShellPath} in {WorkingDirectory}",
                shellPath, session.WorkingDirectory);

            // Pty.Net 0.1.16-pre uses a simpler API
            var pty = PtyProvider.Spawn(
                shellPath,
                session.Size.Columns,
                session.Size.Rows,
                session.WorkingDirectory);

            // ─────────────────────────────────────────────────────────────
            // Step 5: Create context and track session
            // ─────────────────────────────────────────────────────────────
            var context = new TerminalSessionContext(session, pty, buffer, parser);

            // Subscribe to PTY events
            pty.PtyData += (sender, data) => OnPtyData(sessionId, data);
            pty.PtyDisconnected += sender => OnPtyDisconnected(sessionId);

            // Set dispose callback on session
            session.OnDisposeAsync = async () => { await CloseSessionInternalAsync(sessionId); };

            _sessions[sessionId] = context;

            // ─────────────────────────────────────────────────────────────
            // Step 6: Update state and finalize
            // ─────────────────────────────────────────────────────────────
            UpdateSessionState(context, TerminalSessionState.Running);

            // Set as active if first session
            if (_activeSessionId == Guid.Empty)
            {
                _activeSessionId = sessionId;
                _logger.LogDebug("Setting session {SessionId} as active", sessionId);
            }

            _logger.LogInformation("Terminal session created: {SessionId} ({SessionName})",
                sessionId, session.Name);

            // Raise SessionCreated event
            SessionCreated?.Invoke(this, new TerminalSessionEventArgs { Session = session });

            return session;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> CloseSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await CloseSessionInternalAsync(sessionId);
    }

    /// <inheritdoc />
    public bool SetActiveSession(Guid sessionId)
    {
        ThrowIfDisposed();

        if (!_sessions.ContainsKey(sessionId))
        {
            _logger.LogWarning("Cannot set active: session not found: {SessionId}", sessionId);
            return false;
        }

        _activeSessionId = sessionId;
        _logger.LogDebug("Active session set to: {SessionId}", sessionId);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────
    // I/O Operations
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<bool> WriteInputAsync(
        Guid sessionId,
        string data,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!_sessions.TryGetValue(sessionId, out var context))
        {
            _logger.LogDebug("WriteInput failed: session not found: {SessionId}", sessionId);
            return false;
        }

        if (context.Session.State != TerminalSessionState.Running)
        {
            _logger.LogDebug("WriteInput skipped: session not running: {SessionId} (state={State})",
                sessionId, context.Session.State);
            return false;
        }

        try
        {
            // Use the Pty.Net WriteAsync API
            await context.Pty.WriteAsync(data);

            _logger.LogTrace("Wrote {CharCount} chars to session: {SessionId}",
                data.Length, sessionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to terminal session: {SessionId}", sessionId);
            return false;
        }
    }

    /// <inheritdoc />
    public Task<bool> ResizeAsync(
        Guid sessionId,
        int columns,
        int rows,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!_sessions.TryGetValue(sessionId, out var context))
        {
            _logger.LogDebug("Resize failed: session not found: {SessionId}", sessionId);
            return Task.FromResult(false);
        }

        if (columns < 1 || rows < 1)
        {
            _logger.LogWarning("Invalid resize dimensions: {Columns}x{Rows}", columns, rows);
            return Task.FromResult(false);
        }

        try
        {
            // Resize PTY (sends SIGWINCH)
            context.Pty.Resize(columns, rows);

            // Update session model
            context.Session.Size = new TerminalSize(columns, rows);

            // Resize buffer
            context.Buffer.Resize(columns, rows);

            _logger.LogDebug("Terminal resized to {Columns}x{Rows}: {SessionId}",
                columns, rows, sessionId);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing terminal: {SessionId}", sessionId);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendSignalAsync(
        Guid sessionId,
        TerminalSignal signal,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!_sessions.TryGetValue(sessionId, out var context))
        {
            _logger.LogDebug("SendSignal failed: session not found: {SessionId}", sessionId);
            return false;
        }

        try
        {
            _logger.LogDebug("Sending signal {Signal} to session: {SessionId}", signal, sessionId);

            switch (signal)
            {
                case TerminalSignal.Interrupt:
                    // Send Ctrl+C (ETX, 0x03)
                    return await WriteInputAsync(sessionId, "\x03", cancellationToken);

                case TerminalSignal.Terminate:
                case TerminalSignal.Kill:
                    // Dispose the PTY connection to terminate
                    context.Pty.Dispose();
                    return true;

                case TerminalSignal.Suspend:
                    // Send Ctrl+Z (SUB, 0x1A)
                    return await WriteInputAsync(sessionId, "\x1a", cancellationToken);

                case TerminalSignal.Continue:
                    // Best effort: send 'fg' command to resume
                    return await WriteInputAsync(sessionId, "fg\n", cancellationToken);

                case TerminalSignal.EndOfFile:
                    // Send Ctrl+D (EOT, 0x04)
                    return await WriteInputAsync(sessionId, "\x04", cancellationToken);

                default:
                    _logger.LogWarning("Unknown terminal signal: {Signal}", signal);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending signal {Signal} to session: {SessionId}",
                signal, sessionId);
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Buffer Access
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public TerminalBuffer? GetBuffer(Guid sessionId)
    {
        ThrowIfDisposed();
        return _sessions.TryGetValue(sessionId, out var context)
            ? context.Buffer
            : null;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Convenience Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<bool> ChangeDirectoryAsync(
        Guid sessionId,
        string path,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Quote path to handle spaces and special characters
        var quotedPath = path.Contains(' ') || path.Contains('\'')
            ? $"\"{path.Replace("\"", "\\\"")}\""
            : path;

        var command = $"cd {quotedPath}\n";

        _logger.LogDebug("Changing directory to: {Path} for session: {SessionId}",
            path, sessionId);

        return await WriteInputAsync(sessionId, command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExecuteCommandAsync(
        Guid sessionId,
        string command,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Ensure command ends with newline
        var commandWithNewline = command.EndsWith('\n') ? command : command + "\n";

        _logger.LogDebug("Executing command in session {SessionId}: {Command}",
            sessionId, command.TrimEnd('\n', '\r'));

        return await WriteInputAsync(sessionId, commandWithNewline, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────
    // IAsyncDisposable
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _logger.LogDebug("Disposing TerminalService...");

        // Close all sessions
        var sessionIds = _sessions.Keys.ToList();
        foreach (var sessionId in sessionIds)
        {
            try
            {
                await CloseSessionInternalAsync(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing session during disposal: {SessionId}", sessionId);
            }
        }

        _sessionLock.Dispose();
        _logger.LogInformation("TerminalService disposed. Closed {Count} sessions.", sessionIds.Count);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Session Management
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Internal session close implementation.
    /// </summary>
    private ValueTask<bool> CloseSessionInternalAsync(Guid sessionId)
    {
        if (!_sessions.TryRemove(sessionId, out var context))
        {
            _logger.LogWarning("CloseSession: session not found or already removed: {SessionId}",
                sessionId);
            return ValueTask.FromResult(false);
        }

        _logger.LogDebug("Closing terminal session: {SessionId}", sessionId);

        try
        {
            // Update state to closing
            UpdateSessionState(context, TerminalSessionState.Closing);

            // Update active session if needed
            if (_activeSessionId == sessionId)
            {
                _activeSessionId = _sessions.Keys.FirstOrDefault();
                _logger.LogDebug("Active session changed to: {SessionId}",
                    _activeSessionId == Guid.Empty ? "none" : _activeSessionId.ToString());
            }

            // Set closed timestamp
            context.Session.ClosedAt = DateTime.UtcNow;

            // Dispose context resources (disposes PTY)
            context.Dispose();

            _logger.LogInformation("Terminal session closed: {SessionId} ({SessionName})",
                sessionId, context.Session.Name);

            // Raise SessionClosed event
            SessionClosed?.Invoke(this, new TerminalSessionEventArgs { Session = context.Session });

            return ValueTask.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing terminal session: {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Throws ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TerminalService));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - PTY Event Handlers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles data received from the PTY.
    /// </summary>
    private void OnPtyData(Guid sessionId, string data)
    {
        if (!_sessions.TryGetValue(sessionId, out var context))
        {
            return;
        }

        try
        {
            // Parse through ANSI parser (updates buffer)
            context.Parser.Parse(data);

            // Raise OutputReceived event for raw data
            OutputReceived?.Invoke(this, new TerminalOutputEventArgs
            {
                SessionId = sessionId,
                Data = data
            });

            _logger.LogTrace("Received {CharCount} chars from session: {SessionId}",
                data.Length, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PTY data for session: {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Handles PTY disconnection (process exit).
    /// </summary>
    private void OnPtyDisconnected(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var context))
        {
            return;
        }

        _logger.LogDebug("PTY disconnected for session: {SessionId}", sessionId);

        // Update state to exited (exit code not available in this API)
        context.Session.ExitCode = 0;
        UpdateSessionState(context, TerminalSessionState.Exited, 0);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Parser Configuration
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Configures ANSI parser callbacks for event raising.
    /// </summary>
    private void ConfigureParserCallbacks(AnsiParser parser, Guid sessionId)
    {
        // Subscribe to parser events
        parser.Bell += () =>
        {
            _logger.LogTrace("Bell triggered for session: {SessionId}", sessionId);
            BellTriggered?.Invoke(this, new TerminalBellEventArgs { SessionId = sessionId });
        };

        parser.TitleChanged += title =>
        {
            _logger.LogDebug("Title changed for session {SessionId}: {Title}", sessionId, title);

            // Update session title
            if (_sessions.TryGetValue(sessionId, out var ctx))
            {
                ctx.Session.Title = title;
            }

            TitleChanged?.Invoke(this, new TerminalTitleEventArgs
            {
                SessionId = sessionId,
                Title = title
            });
        };

        parser.WorkingDirectoryChanged += path =>
        {
            _logger.LogDebug("Working directory changed for session {SessionId}: {Path}",
                sessionId, path);

            // Update session working directory
            if (_sessions.TryGetValue(sessionId, out var ctx))
            {
                ctx.Session.WorkingDirectory = path;
            }
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - State Management
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates session state and raises the SessionStateChanged event.
    /// </summary>
    private void UpdateSessionState(
        TerminalSessionContext context,
        TerminalSessionState newState,
        int? exitCode = null)
    {
        var oldState = context.Session.State;

        // Skip if no change
        if (oldState == newState)
        {
            return;
        }

        _logger.LogDebug("Session {SessionId} state: {OldState} → {NewState}",
            context.Session.Id, oldState, newState);

        // Update session
        context.Session.State = newState;
        if (exitCode.HasValue)
        {
            context.Session.ExitCode = exitCode;
        }

        // Raise event
        SessionStateChanged?.Invoke(this, new TerminalSessionStateEventArgs
        {
            SessionId = context.Session.Id,
            OldState = oldState,
            NewState = newState,
            ExitCode = exitCode
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    // Nested Types
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Internal context holding all resources for a terminal session.
    /// </summary>
    /// <remarks>
    /// Groups together the session model, PTY connection, buffer, and parser
    /// for coordinated management.
    /// </remarks>
    private sealed class TerminalSessionContext : IDisposable
    {
        /// <summary>Gets or sets the session model.</summary>
        public TerminalSession Session { get; set; }

        /// <summary>Gets the PTY connection.</summary>
        public IPtyConnection Pty { get; }

        /// <summary>Gets the terminal buffer.</summary>
        public TerminalBuffer Buffer { get; }

        /// <summary>Gets the ANSI parser.</summary>
        public AnsiParser Parser { get; }

        /// <summary>
        /// Creates a new session context.
        /// </summary>
        public TerminalSessionContext(
            TerminalSession session,
            IPtyConnection pty,
            TerminalBuffer buffer,
            AnsiParser parser)
        {
            Session = session;
            Pty = pty;
            Buffer = buffer;
            Parser = parser;
        }

        /// <summary>
        /// Disposes of the PTY connection.
        /// </summary>
        public void Dispose()
        {
            Pty.Dispose();
        }
    }
}
