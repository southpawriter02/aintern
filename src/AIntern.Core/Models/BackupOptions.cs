namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BACKUP OPTIONS (v0.4.3c)                                                 │
// │ Configuration options for the backup service.                            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Configuration options for the backup service.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3c.</para>
/// </remarks>
public sealed record BackupOptions
{
    /// <summary>
    /// Maximum number of backups to keep per original file.
    /// Default: 10 backups per file.
    /// </summary>
    public int MaxBackupsPerFile { get; init; } = 10;

    /// <summary>
    /// Maximum total storage for all backups in bytes.
    /// Default: 500 MB.
    /// </summary>
    public long MaxTotalStorageBytes { get; init; } = 500 * 1024 * 1024;

    /// <summary>
    /// Maximum age of backups to keep.
    /// Default: 7 days.
    /// </summary>
    public TimeSpan MaxBackupAge { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Whether to compute content hash for each backup.
    /// Default: true.
    /// </summary>
    public bool ComputeContentHash { get; init; } = true;

    /// <summary>
    /// Whether to compress backup files.
    /// Reserved for future use. Default: false.
    /// </summary>
    public bool CompressBackups { get; init; } = false;

    /// <summary>
    /// Whether to run automatic cleanup after creating backups.
    /// Default: false (manual cleanup preferred).
    /// </summary>
    public bool AutoCleanup { get; init; } = false;

    /// <summary>
    /// Whether to verify backups after creation.
    /// Default: false (for performance).
    /// </summary>
    public bool VerifyAfterCreate { get; init; } = false;

    /// <summary>
    /// Custom backup directory path. If null, uses default location.
    /// </summary>
    public string? CustomBackupDirectory { get; init; }

    /// <summary>
    /// Default backup options.
    /// </summary>
    public static BackupOptions Default { get; } = new();

    /// <summary>
    /// Minimal options for testing (small limits).
    /// </summary>
    public static BackupOptions Minimal { get; } = new()
    {
        MaxBackupsPerFile = 3,
        MaxTotalStorageBytes = 10 * 1024 * 1024,
        MaxBackupAge = TimeSpan.FromHours(1),
        ComputeContentHash = true
    };

    /// <summary>
    /// Extended options for long-term backup retention.
    /// </summary>
    public static BackupOptions Extended { get; } = new()
    {
        MaxBackupsPerFile = 50,
        MaxTotalStorageBytes = 2L * 1024 * 1024 * 1024,
        MaxBackupAge = TimeSpan.FromDays(30),
        ComputeContentHash = true
    };
}
