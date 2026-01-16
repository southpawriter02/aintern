namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CONFLICT REASON (v0.4.3e)                                                │
// │ Specifies the reason for a file conflict.                                │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Specifies the reason for a file conflict.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3e.</para>
/// </remarks>
public enum ConflictReason
{
    /// <summary>No conflict detected.</summary>
    None = 0,

    /// <summary>No snapshot was taken for this file.</summary>
    NoSnapshot = 1,

    /// <summary>File was created after the snapshot was taken.</summary>
    FileCreated = 2,

    /// <summary>File was deleted after the snapshot was taken.</summary>
    FileDeleted = 3,

    /// <summary>File content was modified (hash mismatch).</summary>
    ContentModified = 4,

    /// <summary>File permissions changed making it inaccessible.</summary>
    PermissionChanged = 5
}
