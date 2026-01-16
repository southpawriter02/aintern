namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ VALIDATION SEVERITY (v0.4.4a)                                            │
// │ Severity level of a validation issue.                                    │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Severity level of a validation issue.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message.
    /// Does not prevent apply.
    /// </summary>
    Info,

    /// <summary>
    /// Warning - can proceed with caution.
    /// User should review before continuing.
    /// </summary>
    Warning,

    /// <summary>
    /// Error - cannot proceed.
    /// Must be resolved before apply.
    /// </summary>
    Error
}
