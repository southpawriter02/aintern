namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CONFLICT RESOLUTION (v0.4.3g)                                            │
// │ Specifies the user's chosen resolution for a file conflict.             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Specifies the user's chosen resolution for a file conflict.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3g.</para>
/// </remarks>
public enum ConflictResolution
{
    /// <summary>User cancelled the operation.</summary>
    Cancel = 0,

    /// <summary>User requested to refresh the diff against current file content.</summary>
    RefreshDiff = 1,

    /// <summary>User chose to force apply despite the conflict.</summary>
    ForceApply = 2
}
