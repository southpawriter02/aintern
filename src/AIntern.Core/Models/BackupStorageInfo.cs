namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BACKUP STORAGE INFO (v0.4.3c)                                            │
// │ Storage statistics and health information for backups.                   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Storage statistics and health information for backups.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3c.</para>
/// </remarks>
public sealed class BackupStorageInfo
{
    /// <summary>
    /// Total size of all backup files in bytes.
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Total number of backup files.
    /// </summary>
    public int BackupCount { get; init; }

    /// <summary>
    /// Number of unique original files with backups.
    /// </summary>
    public int UniqueFilesCount { get; init; }

    /// <summary>
    /// Timestamp of the oldest backup.
    /// </summary>
    public DateTime? OldestBackup { get; init; }

    /// <summary>
    /// Timestamp of the newest backup.
    /// </summary>
    public DateTime? NewestBackup { get; init; }

    /// <summary>
    /// Number of backups with corrupted or missing metadata.
    /// </summary>
    public int OrphanedCount { get; init; }

    /// <summary>
    /// Number of backups that failed integrity verification.
    /// </summary>
    public int CorruptedCount { get; init; }

    /// <summary>
    /// Overall health status of the backup system.
    /// </summary>
    public BackupHealthStatus HealthStatus { get; init; }

    /// <summary>
    /// Path to the backup directory.
    /// </summary>
    public string BackupDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Available disk space in the backup directory.
    /// </summary>
    public long AvailableDiskSpaceBytes { get; init; }

    /// <summary>
    /// Formatted total size (e.g., "125.5 MB").
    /// </summary>
    public string FormattedTotalSize => FormatBytes(TotalSizeBytes);

    /// <summary>
    /// Formatted available space (e.g., "50.2 GB").
    /// </summary>
    public string FormattedAvailableSpace => FormatBytes(AvailableDiskSpaceBytes);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Health status of the backup system.
/// </summary>
public enum BackupHealthStatus
{
    /// <summary>
    /// All backups are healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// Some backups have issues but system is functional.
    /// </summary>
    Warning,

    /// <summary>
    /// Critical issues detected (e.g., disk full, many corrupted backups).
    /// </summary>
    Critical,

    /// <summary>
    /// Backup system is unavailable.
    /// </summary>
    Unavailable
}
