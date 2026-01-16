namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ VALIDATION ISSUE TYPE (v0.4.4a)                                          │
// │ Type of validation issue for file tree proposals.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Type of validation issue for file tree proposals.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public enum ValidationIssueType
{
    /// <summary>
    /// File already exists at the target path.
    /// </summary>
    FileExists,

    /// <summary>
    /// Directory already exists at the target path.
    /// </summary>
    DirectoryExists,

    /// <summary>
    /// Invalid file path format.
    /// </summary>
    InvalidPath,

    /// <summary>
    /// Permission denied to write to target.
    /// </summary>
    PermissionDenied,

    /// <summary>
    /// Parent directory doesn't exist.
    /// </summary>
    ParentNotExists,

    /// <summary>
    /// File path exceeds maximum length.
    /// </summary>
    PathTooLong,

    /// <summary>
    /// Invalid characters in path.
    /// </summary>
    InvalidCharacters,

    /// <summary>
    /// Circular dependency detected between operations.
    /// </summary>
    CircularDependency,

    /// <summary>
    /// Duplicate path in proposal.
    /// </summary>
    DuplicatePath,

    /// <summary>
    /// Content is empty for a create operation.
    /// </summary>
    EmptyContent,

    /// <summary>
    /// File path is outside the workspace.
    /// </summary>
    OutsideWorkspace
}
