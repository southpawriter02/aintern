namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CONFLICT INFO (v0.4.3e)                                                  │
// │ Contains information about a detected file conflict.                     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Contains information about a detected file conflict.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3e.</para>
/// </remarks>
public sealed class ConflictInfo
{
    /// <summary>Gets whether a conflict was detected.</summary>
    public bool HasConflict { get; init; }

    /// <summary>Gets the reason for the conflict.</summary>
    public ConflictReason Reason { get; init; }

    /// <summary>Gets a human-readable message describing the conflict.</summary>
    public string? Message { get; init; }

    /// <summary>Gets the file's last modification time (if available).</summary>
    public DateTime? LastModified { get; init; }

    /// <summary>Gets the time when the snapshot was taken.</summary>
    public DateTime? SnapshotTime { get; init; }

    /// <summary>Gets the file path this conflict applies to.</summary>
    public string? FilePath { get; init; }

    /// <summary>Creates a ConflictInfo indicating no conflict.</summary>
    public static ConflictInfo NoConflict() => new()
    {
        HasConflict = false,
        Reason = ConflictReason.None
    };

    /// <summary>Creates a ConflictInfo indicating no snapshot exists.</summary>
    public static ConflictInfo NoSnapshotExists() => new()
    {
        HasConflict = false,
        Reason = ConflictReason.NoSnapshot
    };

    /// <summary>Creates a ConflictInfo for file creation conflict.</summary>
    public static ConflictInfo FileWasCreated(DateTime snapshotTime) => new()
    {
        HasConflict = true,
        Reason = ConflictReason.FileCreated,
        Message = "File was created after the proposal was generated",
        SnapshotTime = snapshotTime
    };

    /// <summary>Creates a ConflictInfo for file deletion conflict.</summary>
    public static ConflictInfo FileWasDeleted(DateTime snapshotTime) => new()
    {
        HasConflict = true,
        Reason = ConflictReason.FileDeleted,
        Message = "File was deleted after the proposal was generated",
        SnapshotTime = snapshotTime
    };

    /// <summary>Creates a ConflictInfo for content modification conflict.</summary>
    public static ConflictInfo ContentWasModified(DateTime lastModified, DateTime snapshotTime) => new()
    {
        HasConflict = true,
        Reason = ConflictReason.ContentModified,
        Message = "File content has been modified",
        LastModified = lastModified,
        SnapshotTime = snapshotTime
    };

    /// <summary>Creates a ConflictInfo for permission change conflict.</summary>
    public static ConflictInfo PermissionsChanged(DateTime snapshotTime) => new()
    {
        HasConflict = true,
        Reason = ConflictReason.PermissionChanged,
        Message = "File permissions have changed",
        SnapshotTime = snapshotTime
    };
}
