using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ UNDO MANAGER (v0.4.3d)                                                   │
// │ Manages undo operations with time-based expiration.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Manages undo operations with time-based expiration.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3d.</para>
/// </remarks>
public sealed class UndoManager : IUndoManager
{
    private readonly IFileChangeService _fileChangeService;
    private readonly ILogger<UndoManager>? _logger;
    private readonly UndoOptions _options;
    private readonly ConcurrentDictionary<Guid, UndoState> _pendingUndos = new();
    private readonly ConcurrentDictionary<string, Guid> _fileToLatestChange = new();
    private readonly object _lock = new();
    private readonly Timer _cleanupTimer;
    private readonly Timer _uiUpdateTimer;
    private bool _disposed;

    // ═══════════════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public TimeSpan UndoWindow => _options.UndoWindow;

    /// <inheritdoc />
    public int PendingUndoCount => _pendingUndos.Count(kv => !kv.Value.IsExpired);

    /// <inheritdoc />
    public bool HasPendingUndos => _pendingUndos.Any(kv => !kv.Value.IsExpired);

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public event EventHandler<UndoAvailableEventArgs>? UndoAvailable;

    /// <inheritdoc />
    public event EventHandler<UndoExpiredEventArgs>? UndoExpired;

    /// <inheritdoc />
    public event EventHandler<UndoCompletedEventArgs>? UndoCompleted;

    /// <inheritdoc />
    public event EventHandler<TimeRemainingChangedEventArgs>? TimeRemainingChanged;

    /// <inheritdoc />
    public event EventHandler? AllUndosExpired;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the UndoManager.
    /// </summary>
    public UndoManager(
        IFileChangeService fileChangeService,
        ILogger<UndoManager>? logger = null,
        UndoOptions? options = null)
    {
        _fileChangeService = fileChangeService ?? throw new ArgumentNullException(nameof(fileChangeService));
        _logger = logger;
        _options = options ?? UndoOptions.Default;

        // Subscribe to file change events
        _fileChangeService.FileChanged += OnFileChanged;

        // Setup cleanup timer
        _cleanupTimer = new Timer(
            OnCleanupTick,
            null,
            _options.CleanupInterval,
            _options.CleanupInterval);

        // Setup UI update timer
        _uiUpdateTimer = new Timer(
            OnUiUpdateTick,
            null,
            _options.UiUpdateInterval,
            _options.UiUpdateInterval);

        _logger?.LogInformation("UndoManager initialized with {Window} undo window", _options.UndoWindow);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event Handlers
    // ═══════════════════════════════════════════════════════════════════════

    private void OnFileChanged(object? sender, FileChangedEventArgs e)
    {
        if (e.ChangeRecord != null)
        {
            RegisterChange(e.ChangeRecord);
        }
    }

    private void OnCleanupTick(object? state)
    {
        CleanupExpired();
    }

    private void OnUiUpdateTick(object? state)
    {
        var pending = GetAllPendingUndos();
        if (pending.Count > 0)
        {
            var expiringSoonCount = pending.Count(u => u.IsExpiringSoon);
            TimeRemainingChanged?.Invoke(this, new TimeRemainingChangedEventArgs
            {
                PendingUndos = pending,
                ExpiringSoonCount = expiringSoonCount
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // State Management
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public void RegisterChange(FileChangeRecord changeRecord)
    {
        if (changeRecord == null) return;

        var state = new UndoState
        {
            ChangeId = changeRecord.Id,
            FilePath = changeRecord.FilePath,
            RelativePath = changeRecord.RelativePath,
            ChangeType = changeRecord.ChangeType,
            CreatedAt = changeRecord.ChangedAt,
            ExpiresAt = changeRecord.ChangedAt + _options.UndoWindow,
            Description = changeRecord.Description,
            CodeBlockId = changeRecord.CodeBlockId,
            MessageId = changeRecord.MessageId,
            ChangeRecord = changeRecord
        };

        lock (_lock)
        {
            _pendingUndos[state.ChangeId] = state;
            _fileToLatestChange[state.FilePath] = state.ChangeId;

            // Prune if over limit
            PruneExcessUndos();
        }

        _logger?.LogDebug("Registered undo for {FilePath}, expires at {ExpiresAt}",
            state.FilePath, state.ExpiresAt);

        UndoAvailable?.Invoke(this, new UndoAvailableEventArgs
        {
            FilePath = state.FilePath,
            RelativePath = state.RelativePath,
            ChangeType = state.ChangeType,
            ExpiresAt = state.ExpiresAt,
            ChangeId = state.ChangeId,
            UndoState = state
        });
    }

    private void PruneExcessUndos()
    {
        while (_pendingUndos.Count > _options.MaxPendingUndos)
        {
            var oldest = _pendingUndos.Values
                .OrderBy(u => u.CreatedAt)
                .FirstOrDefault();

            if (oldest != null)
            {
                _pendingUndos.TryRemove(oldest.ChangeId, out _);
                _logger?.LogDebug("Pruned oldest undo {ChangeId}", oldest.ChangeId);
            }
        }
    }

    private void CleanupExpired()
    {
        var expired = _pendingUndos.Values.Where(u => u.IsExpired).ToList();
        var hadPending = HasPendingUndos;

        foreach (var state in expired)
        {
            if (_pendingUndos.TryRemove(state.ChangeId, out _))
            {
                _logger?.LogDebug("Cleaned up expired undo for {FilePath}", state.FilePath);

                UndoExpired?.Invoke(this, new UndoExpiredEventArgs
                {
                    FilePath = state.FilePath,
                    ChangeId = state.ChangeId,
                    CreatedAt = state.CreatedAt,
                    ExpiredAt = state.EffectiveExpiresAt
                });
            }
        }

        if (hadPending && !HasPendingUndos)
        {
            AllUndosExpired?.Invoke(this, EventArgs.Empty);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Undo Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> UndoAsync(string filePath)
    {
        if (!_fileToLatestChange.TryGetValue(filePath, out var changeId))
        {
            _logger?.LogWarning("No undo available for {FilePath}", filePath);
            return false;
        }

        return await UndoByIdAsync(changeId);
    }

    /// <inheritdoc />
    public async Task<bool> UndoByIdAsync(Guid changeId)
    {
        if (!_pendingUndos.TryGetValue(changeId, out var state))
        {
            _logger?.LogWarning("Undo state not found for {ChangeId}", changeId);
            return false;
        }

        if (state.IsExpired)
        {
            _logger?.LogWarning("Undo expired for {ChangeId}", changeId);
            return false;
        }

        try
        {
            var success = await _fileChangeService.UndoChangeAsync(changeId);

            if (success)
            {
                _pendingUndos.TryRemove(changeId, out _);

                UndoCompleted?.Invoke(this, new UndoCompletedEventArgs
                {
                    FilePath = state.FilePath,
                    ChangeId = changeId,
                    Success = true
                });

                _logger?.LogInformation("Undo completed for {FilePath}", state.FilePath);
            }
            else
            {
                UndoCompleted?.Invoke(this, new UndoCompletedEventArgs
                {
                    FilePath = state.FilePath,
                    ChangeId = changeId,
                    Success = false,
                    ErrorMessage = "Undo operation failed"
                });
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error undoing {ChangeId}", changeId);

            UndoCompleted?.Invoke(this, new UndoCompletedEventArgs
            {
                FilePath = state.FilePath,
                ChangeId = changeId,
                Success = false,
                ErrorMessage = ex.Message
            });

            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> UndoAllAsync()
    {
        var pending = GetAllPendingUndos();
        return await UndoMultipleAsync(pending.Select(u => u.ChangeId));
    }

    /// <inheritdoc />
    public async Task<int> UndoMultipleAsync(IEnumerable<Guid> changeIds)
    {
        var count = 0;
        foreach (var id in changeIds)
        {
            if (await UndoByIdAsync(id))
                count++;
        }
        return count;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Query Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public bool CanUndo(string filePath)
    {
        if (!_fileToLatestChange.TryGetValue(filePath, out var changeId))
            return false;

        return CanUndoById(changeId);
    }

    /// <inheritdoc />
    public bool CanUndoById(Guid changeId)
    {
        return _pendingUndos.TryGetValue(changeId, out var state) && !state.IsExpired;
    }

    /// <inheritdoc />
    public TimeSpan GetTimeRemaining(string filePath)
    {
        if (!_fileToLatestChange.TryGetValue(filePath, out var changeId))
            return TimeSpan.Zero;

        return GetTimeRemainingById(changeId);
    }

    /// <inheritdoc />
    public TimeSpan GetTimeRemainingById(Guid changeId)
    {
        return _pendingUndos.TryGetValue(changeId, out var state)
            ? state.TimeRemaining
            : TimeSpan.Zero;
    }

    /// <inheritdoc />
    public UndoState? GetUndoState(string filePath)
    {
        if (!_fileToLatestChange.TryGetValue(filePath, out var changeId))
            return null;

        return GetUndoStateById(changeId);
    }

    /// <inheritdoc />
    public UndoState? GetUndoStateById(Guid changeId)
    {
        return _pendingUndos.TryGetValue(changeId, out var state) ? state : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<UndoState> GetAllPendingUndos()
    {
        return _pendingUndos.Values
            .Where(u => !u.IsExpired)
            .OrderBy(u => u.ExpiresAt)
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Timer Management
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public bool PauseCountdown(Guid changeId)
    {
        if (!_pendingUndos.TryGetValue(changeId, out var state))
            return false;

        if (state.IsPaused || state.IsExpired)
            return false;

        lock (_lock)
        {
            state.IsPaused = true;
            state.PausedAt = DateTime.UtcNow;
        }

        _logger?.LogDebug("Paused countdown for {ChangeId}", changeId);
        return true;
    }

    /// <inheritdoc />
    public bool ResumeCountdown(Guid changeId)
    {
        if (!_pendingUndos.TryGetValue(changeId, out var state))
            return false;

        if (!state.IsPaused)
            return false;

        lock (_lock)
        {
            if (state.PausedAt.HasValue)
            {
                state.TotalPausedTime += DateTime.UtcNow - state.PausedAt.Value;
            }
            state.IsPaused = false;
            state.PausedAt = null;
        }

        _logger?.LogDebug("Resumed countdown for {ChangeId}", changeId);
        return true;
    }

    /// <inheritdoc />
    public bool ExtendTime(Guid changeId, TimeSpan additionalTime)
    {
        if (!_options.AllowExtendTime)
            return false;

        if (!_pendingUndos.TryGetValue(changeId, out var state))
            return false;

        if (state.IsExpired)
            return false;

        // Check max extended time
        var totalTime = state.EffectiveExpiresAt - state.CreatedAt + additionalTime;
        if (totalTime > _options.MaxExtendedTime)
        {
            _logger?.LogDebug("Cannot extend {ChangeId}: exceeds max {Max}", changeId, _options.MaxExtendedTime);
            return false;
        }

        lock (_lock)
        {
            state.TotalPausedTime += additionalTime;
        }

        _logger?.LogDebug("Extended {ChangeId} by {Time}", changeId, additionalTime);
        return true;
    }

    /// <inheritdoc />
    public bool Dismiss(Guid changeId)
    {
        if (_pendingUndos.TryRemove(changeId, out var state))
        {
            _logger?.LogDebug("Dismissed undo for {FilePath}", state.FilePath);
            return true;
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose
    // ═══════════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;

        _fileChangeService.FileChanged -= OnFileChanged;
        _cleanupTimer.Dispose();
        _uiUpdateTimer.Dispose();

        _disposed = true;
    }
}
