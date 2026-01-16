namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE OPERATION TYPE (v0.4.4a)                                            │
// │ Type of file operation within a multi-file proposal.                     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Type of file operation within a multi-file proposal.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public enum FileOperationType
{
    /// <summary>
    /// Create a new file.
    /// File must not exist (or will be overwritten with confirmation).
    /// </summary>
    Create,

    /// <summary>
    /// Modify an existing file.
    /// File must exist.
    /// </summary>
    Modify,

    /// <summary>
    /// Delete a file.
    /// File must exist.
    /// </summary>
    Delete,

    /// <summary>
    /// Rename a file (within same directory).
    /// Source must exist, target must not exist.
    /// </summary>
    Rename,

    /// <summary>
    /// Move a file to a new location.
    /// Source must exist, target directory must exist.
    /// </summary>
    Move,

    /// <summary>
    /// Create a directory.
    /// Parent directories will be created as needed.
    /// </summary>
    CreateDirectory,

    /// <summary>
    /// Unknown or unsupported operation type.
    /// Cannot be applied.
    /// </summary>
    Unknown
}
