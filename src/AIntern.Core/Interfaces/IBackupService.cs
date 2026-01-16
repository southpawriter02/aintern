namespace AIntern.Core.Interfaces;

using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BACKUP SERVICE INTERFACE (v0.4.3c)                                       │
// │ Service for managing file backups to support undo operations.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for managing file backups to support undo operations.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3b, expanded in v0.4.3c.</para>
/// </remarks>
public interface IBackupService
{
    // ═══════════════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the directory where backups are stored.
    /// </summary>
    string BackupDirectory { get; }

    // ═══════════════════════════════════════════════════════════════════════
    // Backup Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Create a backup of a file.
    /// </summary>
    Task<string?> CreateBackupAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Create a backup with a pre-computed content hash.
    /// </summary>
    Task<string?> CreateBackupWithHashAsync(string filePath, string contentHash, CancellationToken ct = default);

    /// <summary>
    /// Create an incremental backup only if content has changed.
    /// </summary>
    Task<string?> CreateIncrementalBackupAsync(string filePath, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Restore Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Restore a file from a specific backup.
    /// </summary>
    Task<bool> RestoreBackupAsync(string backupPath, string targetPath, CancellationToken ct = default);

    /// <summary>
    /// Restore a file from its most recent backup.
    /// </summary>
    Task<bool> RestoreLatestBackupAsync(string originalPath, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Delete Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Delete a specific backup file and its metadata.
    /// </summary>
    bool DeleteBackup(string backupPath);

    /// <summary>
    /// Delete all backups for a specific file.
    /// </summary>
    Task<int> DeleteAllBackupsForFileAsync(string originalPath, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Query Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Check if a backup exists.
    /// </summary>
    bool BackupExists(string backupPath);

    /// <summary>
    /// Get all backups for a specific file.
    /// </summary>
    IReadOnlyList<BackupInfo> GetBackupsForFile(string originalPath);

    /// <summary>
    /// Get all backups in the backup directory.
    /// </summary>
    IReadOnlyList<BackupInfo> GetAllBackups();

    /// <summary>
    /// Get information about a specific backup.
    /// </summary>
    BackupInfo? GetBackupInfo(string backupPath);

    // ═══════════════════════════════════════════════════════════════════════
    // Cleanup Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clean up backups older than the specified age.
    /// </summary>
    int CleanupExpiredBackups(TimeSpan maxAge);

    /// <summary>
    /// Clean up backups to stay under storage limit.
    /// </summary>
    Task<int> CleanupByStorageLimitAsync(long maxStorageBytes, CancellationToken ct = default);

    /// <summary>
    /// Clean up orphaned backups (missing or corrupted metadata).
    /// </summary>
    Task<int> CleanupOrphanedBackupsAsync(CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Storage Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get total size of all backup files.
    /// </summary>
    long GetTotalBackupSize();

    /// <summary>
    /// Get storage information and health status.
    /// </summary>
    BackupStorageInfo GetStorageInfo();

    // ═══════════════════════════════════════════════════════════════════════
    // Integrity Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verify integrity of a specific backup.
    /// </summary>
    Task<bool> VerifyBackupIntegrityAsync(string backupPath, CancellationToken ct = default);

    /// <summary>
    /// Verify integrity of all backups.
    /// </summary>
    Task<IReadOnlyList<string>> VerifyAllBackupsAsync(CancellationToken ct = default);
}
