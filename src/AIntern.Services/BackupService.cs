using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BACKUP SERVICE (v0.4.3c)                                                 │
// │ Service for managing file backups to support undo operations.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for managing file backups to support undo operations.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3c.</para>
/// </remarks>
public sealed class BackupService : IBackupService, IDisposable
{
    private readonly ILogger<BackupService>? _logger;
    private readonly BackupOptions _options;
    private readonly object _lock = new();
    private bool _disposed;

    private const string BackupExtension = ".backup";
    private const string MetadataExtension = ".backup.meta";

    /// <inheritdoc />
    public string BackupDirectory { get; }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the BackupService.
    /// </summary>
    public BackupService(
        ILogger<BackupService>? logger = null,
        BackupOptions? options = null)
    {
        _logger = logger;
        _options = options ?? BackupOptions.Default;

        BackupDirectory = _options.CustomBackupDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AIntern", "backups");

        if (!Directory.Exists(BackupDirectory))
        {
            Directory.CreateDirectory(BackupDirectory);
            _logger?.LogDebug("Created backup directory: {Dir}", BackupDirectory);
        }

        _logger?.LogInformation("BackupService initialized. Directory: {Dir}", BackupDirectory);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Backup Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<string?> CreateBackupAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            _logger?.LogWarning("CreateBackupAsync: File not found: {Path}", filePath);
            return null;
        }

        try
        {
            var content = await File.ReadAllBytesAsync(filePath, ct);
            var hash = _options.ComputeContentHash ? ComputeHash(content) : null;
            return await CreateBackupInternalAsync(filePath, content, hash, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating backup for {Path}", filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> CreateBackupWithHashAsync(string filePath, string contentHash, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            _logger?.LogWarning("CreateBackupWithHashAsync: File not found: {Path}", filePath);
            return null;
        }

        try
        {
            var content = await File.ReadAllBytesAsync(filePath, ct);
            return await CreateBackupInternalAsync(filePath, content, contentHash, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating backup for {Path}", filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> CreateIncrementalBackupAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var content = await File.ReadAllBytesAsync(filePath, ct);
            var hash = ComputeHash(content);

            // Check if we already have a backup with this hash
            var existingBackups = GetBackupsForFile(filePath);
            var existing = existingBackups.FirstOrDefault(b => b.ContentHash == hash);
            if (existing != null)
            {
                _logger?.LogDebug("Incremental backup: content unchanged for {Path}", filePath);
                return existing.BackupPath;
            }

            return await CreateBackupInternalAsync(filePath, content, hash, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating incremental backup for {Path}", filePath);
            return null;
        }
    }

    private async Task<string?> CreateBackupInternalAsync(
        string filePath, byte[] content, string? hash, CancellationToken ct)
    {
        var backupName = GenerateBackupName(filePath);
        var backupPath = Path.Combine(BackupDirectory, backupName);
        var metadataPath = backupPath + ".meta";

        lock (_lock)
        {
            File.WriteAllBytes(backupPath, content);
        }

        var metadata = new BackupMetadata
        {
            Version = 1,
            OriginalPath = filePath,
            CreatedAt = DateTime.UtcNow,
            OriginalSize = content.Length,
            ContentHash = hash,
            OriginalLastModified = File.GetLastWriteTimeUtc(filePath)
        };

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metadataPath, json, ct);

        _logger?.LogDebug("Created backup: {BackupPath} for {FilePath}", backupPath, filePath);

        // Auto-cleanup if enabled
        if (_options.AutoCleanup)
        {
            await CleanupByStorageLimitAsync(_options.MaxTotalStorageBytes, ct);
        }

        return backupPath;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Restore Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> RestoreBackupAsync(string backupPath, string targetPath, CancellationToken ct = default)
    {
        if (!File.Exists(backupPath))
        {
            _logger?.LogWarning("RestoreBackupAsync: Backup not found: {Path}", backupPath);
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var content = await File.ReadAllBytesAsync(backupPath, ct);
            await File.WriteAllBytesAsync(targetPath, content, ct);

            _logger?.LogInformation("Restored backup {BackupPath} to {TargetPath}", backupPath, targetPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error restoring backup {BackupPath}", backupPath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RestoreLatestBackupAsync(string originalPath, CancellationToken ct = default)
    {
        var backups = GetBackupsForFile(originalPath);
        if (backups.Count == 0)
        {
            _logger?.LogWarning("No backups found for {Path}", originalPath);
            return false;
        }

        var latest = backups[0]; // Already sorted newest first
        return await RestoreBackupAsync(latest.BackupPath, originalPath, ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Delete Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public bool DeleteBackup(string backupPath)
    {
        try
        {
            lock (_lock)
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                var metadataPath = backupPath + ".meta";
                if (File.Exists(metadataPath))
                    File.Delete(metadataPath);
            }

            _logger?.LogDebug("Deleted backup: {Path}", backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error deleting backup {Path}", backupPath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteAllBackupsForFileAsync(string originalPath, CancellationToken ct = default)
    {
        var backups = GetBackupsForFile(originalPath);
        var deleted = 0;

        foreach (var backup in backups)
        {
            ct.ThrowIfCancellationRequested();
            if (DeleteBackup(backup.BackupPath))
                deleted++;
        }

        _logger?.LogInformation("Deleted {Count} backups for {Path}", deleted, originalPath);
        return deleted;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Query Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public bool BackupExists(string backupPath)
    {
        return File.Exists(backupPath);
    }

    /// <inheritdoc />
    public IReadOnlyList<BackupInfo> GetBackupsForFile(string originalPath)
    {
        var pathHash = ComputePathHash(originalPath);
        var allBackups = GetAllBackups();
        return allBackups
            .Where(b => ComputePathHash(b.OriginalPath) == pathHash)
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<BackupInfo> GetAllBackups()
    {
        var backups = new List<BackupInfo>();

        if (!Directory.Exists(BackupDirectory))
            return backups;

        var backupFiles = Directory.GetFiles(BackupDirectory, $"*{BackupExtension}")
            .Where(f => !f.EndsWith(MetadataExtension));

        foreach (var file in backupFiles)
        {
            var info = GetBackupInfo(file);
            if (info != null)
                backups.Add(info);
        }

        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }

    /// <inheritdoc />
    public BackupInfo? GetBackupInfo(string backupPath)
    {
        if (!File.Exists(backupPath))
            return null;

        var metadataPath = backupPath + ".meta";
        BackupMetadata? metadata = null;

        if (File.Exists(metadataPath))
        {
            try
            {
                var json = File.ReadAllText(metadataPath);
                metadata = JsonSerializer.Deserialize<BackupMetadata>(json);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error reading metadata for {Path}", backupPath);
            }
        }

        var fileInfo = new FileInfo(backupPath);

        return new BackupInfo
        {
            BackupPath = backupPath,
            OriginalPath = metadata?.OriginalPath ?? string.Empty,
            CreatedAt = metadata?.CreatedAt ?? fileInfo.CreationTimeUtc,
            SizeBytes = fileInfo.Length,
            ContentHash = metadata?.ContentHash,
            WorkspacePath = metadata?.WorkspacePath,
            OriginalLastModified = metadata?.OriginalLastModified
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Cleanup Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public int CleanupExpiredBackups(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var allBackups = GetAllBackups();
        var expired = allBackups.Where(b => b.CreatedAt < cutoff).ToList();
        var deleted = 0;

        foreach (var backup in expired)
        {
            if (DeleteBackup(backup.BackupPath))
                deleted++;
        }

        _logger?.LogInformation("Cleaned up {Count} expired backups (older than {MaxAge})", deleted, maxAge);
        return deleted;
    }

    /// <inheritdoc />
    public async Task<int> CleanupByStorageLimitAsync(long maxStorageBytes, CancellationToken ct = default)
    {
        var allBackups = GetAllBackups().OrderBy(b => b.CreatedAt).ToList();
        var totalSize = allBackups.Sum(b => b.SizeBytes);
        var deleted = 0;

        while (totalSize > maxStorageBytes && allBackups.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var oldest = allBackups[0];

            if (DeleteBackup(oldest.BackupPath))
            {
                totalSize -= oldest.SizeBytes;
                deleted++;
            }

            allBackups.RemoveAt(0);
        }

        if (deleted > 0)
            _logger?.LogInformation("Cleaned up {Count} backups to stay under {Limit} bytes", deleted, maxStorageBytes);

        return deleted;
    }

    /// <inheritdoc />
    public async Task<int> CleanupOrphanedBackupsAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(BackupDirectory))
            return 0;

        var backupFiles = Directory.GetFiles(BackupDirectory, $"*{BackupExtension}")
            .Where(f => !f.EndsWith(MetadataExtension));

        var deleted = 0;

        foreach (var file in backupFiles)
        {
            ct.ThrowIfCancellationRequested();
            var metadataPath = file + ".meta";

            if (!File.Exists(metadataPath))
            {
                if (DeleteBackup(file))
                    deleted++;
            }
        }

        if (deleted > 0)
            _logger?.LogInformation("Cleaned up {Count} orphaned backups", deleted);

        return deleted;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Storage Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public long GetTotalBackupSize()
    {
        return GetAllBackups().Sum(b => b.SizeBytes);
    }

    /// <inheritdoc />
    public BackupStorageInfo GetStorageInfo()
    {
        var allBackups = GetAllBackups();
        var uniqueFiles = allBackups.Select(b => b.OriginalPath).Distinct().Count();
        var orphanedCount = 0;

        if (Directory.Exists(BackupDirectory))
        {
            var backupFiles = Directory.GetFiles(BackupDirectory, $"*{BackupExtension}")
                .Where(f => !f.EndsWith(MetadataExtension));
            orphanedCount = backupFiles.Count(f => !File.Exists(f + ".meta"));
        }

        var healthStatus = BackupHealthStatus.Healthy;
        if (orphanedCount > 0)
            healthStatus = BackupHealthStatus.Warning;

        long availableSpace = 0;
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(BackupDirectory) ?? BackupDirectory);
            availableSpace = driveInfo.AvailableFreeSpace;
        }
        catch { /* Ignore drive info errors */ }

        return new BackupStorageInfo
        {
            TotalSizeBytes = allBackups.Sum(b => b.SizeBytes),
            BackupCount = allBackups.Count,
            UniqueFilesCount = uniqueFiles,
            OldestBackup = allBackups.LastOrDefault()?.CreatedAt,
            NewestBackup = allBackups.FirstOrDefault()?.CreatedAt,
            OrphanedCount = orphanedCount,
            HealthStatus = healthStatus,
            BackupDirectory = BackupDirectory,
            AvailableDiskSpaceBytes = availableSpace
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Integrity Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> VerifyBackupIntegrityAsync(string backupPath, CancellationToken ct = default)
    {
        var info = GetBackupInfo(backupPath);
        if (info == null || string.IsNullOrEmpty(info.ContentHash))
            return true; // No hash to verify

        try
        {
            var content = await File.ReadAllBytesAsync(backupPath, ct);
            var currentHash = ComputeHash(content);
            return currentHash == info.ContentHash;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error verifying backup {Path}", backupPath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> VerifyAllBackupsAsync(CancellationToken ct = default)
    {
        var corrupted = new List<string>();
        var allBackups = GetAllBackups();

        foreach (var backup in allBackups)
        {
            ct.ThrowIfCancellationRequested();
            if (!await VerifyBackupIntegrityAsync(backup.BackupPath, ct))
            {
                corrupted.Add(backup.BackupPath);
            }
        }

        if (corrupted.Count > 0)
            _logger?.LogWarning("Found {Count} corrupted backups", corrupted.Count);

        return corrupted;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Utility Methods
    // ═══════════════════════════════════════════════════════════════════════

    private string GenerateBackupName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var pathHash = ComputePathHash(filePath);
        return $"{fileName}_{timestamp}_{pathHash}{extension}{BackupExtension}";
    }

    private static string ComputePathHash(string path)
    {
        var normalized = path.ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash)[..8].ToUpperInvariant();
    }

    private static string ComputeHash(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}

/// <summary>
/// Internal metadata stored with each backup.
/// </summary>
internal sealed class BackupMetadata
{
    public int Version { get; set; }
    public string OriginalPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long OriginalSize { get; set; }
    public string? ContentHash { get; set; }
    public string? WorkspacePath { get; set; }
    public DateTime? OriginalLastModified { get; set; }
}
