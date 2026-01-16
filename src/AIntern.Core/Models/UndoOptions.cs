namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ UNDO OPTIONS (v0.4.3d)                                                   │
// │ Configuration options for the undo system.                               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Configuration options for the undo system.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3d.</para>
/// </remarks>
public sealed record UndoOptions
{
    /// <summary>
    /// Time window during which undo is available. Default: 30 minutes.
    /// </summary>
    public TimeSpan UndoWindow { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Whether to show toast notifications for undo events. Default: true.
    /// </summary>
    public bool ShowNotifications { get; init; } = true;

    /// <summary>
    /// Whether to play a sound when undo expires. Default: false.
    /// </summary>
    public bool PlaySoundOnExpire { get; init; } = false;

    /// <summary>
    /// Maximum number of pending undos to track. Default: 100.
    /// </summary>
    public int MaxPendingUndos { get; init; } = 100;

    /// <summary>
    /// Interval for cleanup timer. Default: 1 minute.
    /// </summary>
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Interval for UI update timer. Default: 1 second.
    /// </summary>
    public TimeSpan UiUpdateInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Warning threshold for expiring soon indicator. Default: 60 seconds.
    /// </summary>
    public TimeSpan ExpiringSoonThreshold { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Whether to allow extending undo time. Default: true.
    /// </summary>
    public bool AllowExtendTime { get; init; } = true;

    /// <summary>
    /// Maximum time that undo can be extended. Default: 1 hour.
    /// </summary>
    public TimeSpan MaxExtendedTime { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Default undo options.
    /// </summary>
    public static UndoOptions Default { get; } = new();

    /// <summary>
    /// Quick undo options (shorter window).
    /// </summary>
    public static UndoOptions Quick { get; } = new()
    {
        UndoWindow = TimeSpan.FromMinutes(5),
        ExpiringSoonThreshold = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Extended undo options (longer window).
    /// </summary>
    public static UndoOptions Extended { get; } = new()
    {
        UndoWindow = TimeSpan.FromHours(1),
        MaxExtendedTime = TimeSpan.FromHours(2)
    };
}
