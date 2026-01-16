namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ UNDO STATE (v0.4.3d)                                                     │
// │ Represents the state of a single undoable change.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents the state of a single undoable change.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3d.</para>
/// </remarks>
public sealed class UndoState
{
    /// <summary>
    /// Unique identifier for the change.
    /// </summary>
    public Guid ChangeId { get; init; }

    /// <summary>
    /// Absolute path to the changed file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Relative path within the workspace.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Type of change (Created, Modified, Deleted).
    /// </summary>
    public FileChangeType ChangeType { get; init; }

    /// <summary>
    /// When the change was made.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the undo window expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Description of the change.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Code block ID that caused this change (if applicable).
    /// </summary>
    public Guid? CodeBlockId { get; init; }

    /// <summary>
    /// Message ID that contained the code block (if applicable).
    /// </summary>
    public Guid? MessageId { get; init; }

    /// <summary>
    /// Whether the countdown is currently paused.
    /// </summary>
    public bool IsPaused { get; internal set; }

    /// <summary>
    /// When the countdown was paused (if paused).
    /// </summary>
    public DateTime? PausedAt { get; internal set; }

    /// <summary>
    /// Total time the countdown has been paused.
    /// </summary>
    public TimeSpan TotalPausedTime { get; internal set; }

    /// <summary>
    /// Reference to the underlying change record.
    /// </summary>
    public FileChangeRecord? ChangeRecord { get; init; }

    /// <summary>
    /// Gets the effective expiration time (accounting for pause time).
    /// </summary>
    public DateTime EffectiveExpiresAt => ExpiresAt + TotalPausedTime +
        (IsPaused && PausedAt.HasValue ? DateTime.UtcNow - PausedAt.Value : TimeSpan.Zero);

    /// <summary>
    /// Gets whether the undo has expired.
    /// </summary>
    public bool IsExpired => !IsPaused && DateTime.UtcNow >= EffectiveExpiresAt;

    /// <summary>
    /// Gets the time remaining until expiration.
    /// </summary>
    public TimeSpan TimeRemaining
    {
        get
        {
            if (IsPaused)
                return ExpiresAt + TotalPausedTime - (PausedAt ?? DateTime.UtcNow);

            var remaining = EffectiveExpiresAt - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Gets the progress percentage (100% = just started, 0% = expired).
    /// </summary>
    public double ProgressPercentage
    {
        get
        {
            var totalWindow = ExpiresAt - CreatedAt;
            if (totalWindow.TotalSeconds <= 0)
                return 0;

            var elapsed = DateTime.UtcNow - CreatedAt - TotalPausedTime;
            if (IsPaused && PausedAt.HasValue)
                elapsed = PausedAt.Value - CreatedAt - TotalPausedTime;

            var remaining = totalWindow - elapsed;
            return Math.Max(0, Math.Min(100, (remaining.TotalSeconds / totalWindow.TotalSeconds) * 100));
        }
    }

    /// <summary>
    /// Gets whether the undo is expiring soon (less than 60 seconds).
    /// </summary>
    public bool IsExpiringSoon => TimeRemaining.TotalSeconds > 0 && TimeRemaining.TotalSeconds <= 60;

    /// <summary>
    /// Gets the filename portion of the file path.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets a formatted time remaining string (e.g., "28:45").
    /// </summary>
    public string FormattedTimeRemaining
    {
        get
        {
            var time = TimeRemaining;
            if (time.TotalHours >= 1)
                return time.ToString(@"h\:mm\:ss");
            return time.ToString(@"mm\:ss");
        }
    }
}
