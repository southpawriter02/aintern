namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PROPOSAL STATUS (v0.4.4a)                                      │
// │ Status of a file tree proposal lifecycle.                                │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Status of a file tree proposal lifecycle.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public enum FileTreeProposalStatus
{
    /// <summary>
    /// No operations have been applied yet.
    /// Initial state after proposal detection.
    /// </summary>
    Pending,

    /// <summary>
    /// Some operations have been applied.
    /// User may have selected a subset of operations.
    /// </summary>
    PartiallyApplied,

    /// <summary>
    /// All selected operations have been applied successfully.
    /// Terminal success state.
    /// </summary>
    FullyApplied,

    /// <summary>
    /// User explicitly rejected the proposal.
    /// Terminal failure state.
    /// </summary>
    Rejected,

    /// <summary>
    /// Apply was cancelled mid-operation.
    /// May have partial changes applied.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Proposal validation failed.
    /// Contains invalid paths, conflicts, or other issues.
    /// </summary>
    Invalid
}
