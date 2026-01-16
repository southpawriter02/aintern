namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE OPERATION STATUS (v0.4.4a)                                          │
// │ Status of a file operation within a multi-file proposal.                 │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Status of a file operation within a multi-file proposal.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public enum FileOperationStatus
{
    /// <summary>
    /// Not yet applied.
    /// Initial state, eligible for apply.
    /// </summary>
    Pending,

    /// <summary>
    /// Successfully applied.
    /// Terminal success state.
    /// </summary>
    Applied,

    /// <summary>
    /// Skipped by user choice.
    /// User deselected this operation.
    /// </summary>
    Skipped,

    /// <summary>
    /// Failed to apply.
    /// Check ErrorMessage for details.
    /// </summary>
    Failed,

    /// <summary>
    /// Conflict detected (file exists with different content).
    /// Requires user resolution.
    /// </summary>
    Conflict,

    /// <summary>
    /// Currently being applied.
    /// Transient state during batch apply.
    /// </summary>
    InProgress
}
