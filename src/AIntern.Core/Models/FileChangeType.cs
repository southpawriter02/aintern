namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE CHANGE TYPE (v0.4.3a)                                               │
// │ Enum for types of file modifications.                                    │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Type of file modification made during an apply operation.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3a.</para>
/// </remarks>
public enum FileChangeType
{
    /// <summary>
    /// A new file was created.
    /// </summary>
    Created,

    /// <summary>
    /// An existing file was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// A file was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// A file was renamed.
    /// </summary>
    Renamed
}
