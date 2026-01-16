using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CONFLICT DETECTOR INTERFACE (v0.4.3e)                                    │
// │ Provides file conflict detection through snapshot comparison.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Provides file conflict detection through snapshot comparison.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3e.</para>
/// </remarks>
public interface IConflictDetector : IDisposable
{
    /// <summary>Gets the count of stored snapshots.</summary>
    int SnapshotCount { get; }

    /// <summary>Takes a snapshot of a file's current state.</summary>
    Task TakeSnapshotAsync(string filePath, CancellationToken ct = default);

    /// <summary>Takes snapshots of multiple files.</summary>
    Task TakeSnapshotsAsync(IEnumerable<string> filePaths, CancellationToken ct = default);

    /// <summary>Checks if a file has changed since its snapshot.</summary>
    Task<ConflictInfo> CheckConflictAsync(string filePath, CancellationToken ct = default);

    /// <summary>Checks multiple files for conflicts.</summary>
    Task<IReadOnlyDictionary<string, ConflictInfo>> CheckConflictsAsync(
        IEnumerable<string> filePaths, CancellationToken ct = default);

    /// <summary>Clears the snapshot for a specific file.</summary>
    void ClearSnapshot(string filePath);

    /// <summary>Clears all stored snapshots.</summary>
    void ClearAllSnapshots();

    /// <summary>Gets whether a snapshot exists for the file.</summary>
    bool HasSnapshot(string filePath);

    /// <summary>Gets the time when a snapshot was taken.</summary>
    DateTime? GetSnapshotTime(string filePath);
}
