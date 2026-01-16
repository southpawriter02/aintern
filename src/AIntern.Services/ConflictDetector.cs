using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CONFLICT DETECTOR (v0.4.3e)                                              │
// │ Detects conflicts between proposed changes and current file state.      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Detects conflicts between proposed changes and current file state.
/// Uses snapshot-based comparison with SHA-256 content hashing.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3e.</para>
/// </remarks>
public sealed class ConflictDetector : IConflictDetector
{
    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<ConflictDetector>? _logger;
    private readonly ConcurrentDictionary<string, FileSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <inheritdoc />
    public int SnapshotCount => _snapshots.Count;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the ConflictDetector.
    /// </summary>
    public ConflictDetector(
        IFileSystemService fileSystem,
        ILogger<ConflictDetector>? logger = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger;

        _logger?.LogDebug("ConflictDetector initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Snapshot Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task TakeSnapshotAsync(string filePath, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path required", nameof(filePath));

        var normalizedPath = NormalizePath(filePath);

        if (!await _fileSystem.FileExistsAsync(normalizedPath))
        {
            // File doesn't exist - snapshot records this state
            _snapshots[normalizedPath] = new FileSnapshot
            {
                Path = normalizedPath,
                Exists = false,
                ContentHash = null,
                LastModified = default,
                Size = 0,
                TakenAt = DateTime.UtcNow
            };

            _logger?.LogDebug("Snapshot taken for non-existent file: {Path}", normalizedPath);
            return;
        }

        // File exists - capture full state
        var content = await _fileSystem.ReadFileAsync(normalizedPath, ct);
        var fileInfo = new FileInfo(normalizedPath);

        _snapshots[normalizedPath] = new FileSnapshot
        {
            Path = normalizedPath,
            Exists = true,
            ContentHash = ComputeContentHash(content),
            LastModified = fileInfo.LastWriteTimeUtc,
            Size = fileInfo.Length,
            TakenAt = DateTime.UtcNow
        };

        _logger?.LogDebug("Snapshot taken for {Path}, size={Size}", normalizedPath, fileInfo.Length);
    }

    /// <inheritdoc />
    public async Task TakeSnapshotsAsync(IEnumerable<string> filePaths, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(filePaths);

        var tasks = filePaths.Select(path => TakeSnapshotAsync(path, ct));
        await Task.WhenAll(tasks);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Conflict Detection
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ConflictInfo> CheckConflictAsync(string filePath, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path required", nameof(filePath));

        var normalizedPath = NormalizePath(filePath);

        // Check if snapshot exists
        if (!_snapshots.TryGetValue(normalizedPath, out var snapshot))
        {
            return ConflictInfo.NoSnapshotExists();
        }

        // Check current file existence
        bool currentlyExists;
        try
        {
            currentlyExists = await _fileSystem.FileExistsAsync(normalizedPath);
        }
        catch (UnauthorizedAccessException)
        {
            _logger?.LogWarning("Permission denied checking {Path}", normalizedPath);
            return ConflictInfo.PermissionsChanged(snapshot.TakenAt);
        }

        // File was created when we expected it not to exist
        if (!snapshot.Exists && currentlyExists)
        {
            _logger?.LogDebug("Conflict: file created after snapshot: {Path}", normalizedPath);
            return ConflictInfo.FileWasCreated(snapshot.TakenAt);
        }

        // File was deleted when we expected it to exist
        if (snapshot.Exists && !currentlyExists)
        {
            _logger?.LogDebug("Conflict: file deleted after snapshot: {Path}", normalizedPath);
            return ConflictInfo.FileWasDeleted(snapshot.TakenAt);
        }

        // File didn't exist and still doesn't - no conflict
        if (!currentlyExists)
        {
            return ConflictInfo.NoConflict();
        }

        // File exists - compare content hash
        string currentContent;
        try
        {
            currentContent = await _fileSystem.ReadFileAsync(normalizedPath, ct);
        }
        catch (UnauthorizedAccessException)
        {
            _logger?.LogWarning("Permission denied reading {Path}", normalizedPath);
            return ConflictInfo.PermissionsChanged(snapshot.TakenAt);
        }

        var currentHash = ComputeContentHash(currentContent);

        if (!string.Equals(currentHash, snapshot.ContentHash, StringComparison.Ordinal))
        {
            var fileInfo = new FileInfo(normalizedPath);
            _logger?.LogDebug("Conflict: content modified for {Path}", normalizedPath);
            return ConflictInfo.ContentWasModified(fileInfo.LastWriteTimeUtc, snapshot.TakenAt);
        }

        // Content matches - no conflict
        return ConflictInfo.NoConflict();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, ConflictInfo>> CheckConflictsAsync(
        IEnumerable<string> filePaths,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(filePaths);

        var pathList = filePaths.ToList();
        var results = new ConcurrentDictionary<string, ConflictInfo>(StringComparer.OrdinalIgnoreCase);

        await Parallel.ForEachAsync(
            pathList,
            new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 4 },
            async (path, token) =>
            {
                var conflict = await CheckConflictAsync(path, token);
                results[path] = conflict;
            });

        return results;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Snapshot Management
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public void ClearSnapshot(string filePath)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(filePath)) return;

        var normalizedPath = NormalizePath(filePath);
        if (_snapshots.TryRemove(normalizedPath, out _))
        {
            _logger?.LogDebug("Cleared snapshot for {Path}", normalizedPath);
        }
    }

    /// <inheritdoc />
    public void ClearAllSnapshots()
    {
        ThrowIfDisposed();
        var count = _snapshots.Count;
        _snapshots.Clear();
        _logger?.LogDebug("Cleared {Count} snapshots", count);
    }

    /// <inheritdoc />
    public bool HasSnapshot(string filePath)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(filePath)) return false;

        var normalizedPath = NormalizePath(filePath);
        return _snapshots.ContainsKey(normalizedPath);
    }

    /// <inheritdoc />
    public DateTime? GetSnapshotTime(string filePath)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(filePath)) return null;

        var normalizedPath = NormalizePath(filePath);
        return _snapshots.TryGetValue(normalizedPath, out var snapshot)
            ? snapshot.TakenAt
            : null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Utility Methods
    // ═══════════════════════════════════════════════════════════════════════

    private static string ComputeContentHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _snapshots.Clear();
        _disposed = true;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Internal Snapshot Class
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Internal class representing a file state snapshot.
    /// </summary>
    private sealed class FileSnapshot
    {
        public required string Path { get; init; }
        public required bool Exists { get; init; }
        public string? ContentHash { get; init; }
        public DateTime LastModified { get; init; }
        public long Size { get; init; }
        public required DateTime TakenAt { get; init; }
    }
}
