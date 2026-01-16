namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BACKUP INFO (v0.4.3c)                                                    │
// │ Information about a backup file.                                         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Information about a backup file.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3c.</para>
/// </remarks>
public sealed class BackupInfo
{
    /// <summary>
    /// Full path to the backup file.
    /// </summary>
    public string BackupPath { get; init; } = string.Empty;

    /// <summary>
    /// Original file path that was backed up.
    /// </summary>
    public string OriginalPath { get; init; } = string.Empty;

    /// <summary>
    /// Relative path within the workspace (if available).
    /// </summary>
    public string? RelativePath { get; init; }

    /// <summary>
    /// When the backup was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Size of the backup file in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// SHA-256 hash of the file content (if computed).
    /// </summary>
    public string? ContentHash { get; init; }

    /// <summary>
    /// Whether the backup has been verified for integrity.
    /// </summary>
    public bool IsVerified { get; init; }

    /// <summary>
    /// The workspace path at the time of backup (if available).
    /// </summary>
    public string? WorkspacePath { get; init; }

    /// <summary>
    /// Original file's last modified time (if available).
    /// </summary>
    public DateTime? OriginalLastModified { get; init; }

    /// <summary>
    /// Gets the age of this backup.
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;

    /// <summary>
    /// Gets the filename portion of the backup path.
    /// </summary>
    public string BackupFileName => Path.GetFileName(BackupPath);

    /// <summary>
    /// Gets the filename portion of the original path.
    /// </summary>
    public string OriginalFileName => Path.GetFileName(OriginalPath);
}
