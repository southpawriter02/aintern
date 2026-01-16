namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PROPOSAL VALIDATION RESULT (v0.4.4a)                                     │
// │ Result of validating a file tree proposal.                               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Result of validating a file tree proposal.
/// Contains all validation issues found during analysis.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public sealed class ProposalValidationResult
{
    // ═══════════════════════════════════════════════════════════════════════
    // Core Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether the proposal is valid and can be applied.
    /// True only if there are no Error-level issues.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// All validation issues found.
    /// Ordered by severity (errors first), then by path.
    /// </summary>
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = Array.Empty<ValidationIssue>();

    // ═══════════════════════════════════════════════════════════════════════
    // Filtered Views
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Issues that are errors (blocking apply).
    /// </summary>
    public IEnumerable<ValidationIssue> Errors =>
        Issues.Where(i => i.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Issues that are warnings (can proceed with caution).
    /// </summary>
    public IEnumerable<ValidationIssue> Warnings =>
        Issues.Where(i => i.Severity == ValidationSeverity.Warning);

    /// <summary>
    /// Issues that are informational.
    /// </summary>
    public IEnumerable<ValidationIssue> InfoMessages =>
        Issues.Where(i => i.Severity == ValidationSeverity.Info);

    // ═══════════════════════════════════════════════════════════════════════
    // Summary Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether there are any errors.
    /// </summary>
    public bool HasErrors => Errors.Any();

    /// <summary>
    /// Whether there are any warnings.
    /// </summary>
    public bool HasWarnings => Warnings.Any();

    /// <summary>
    /// Total number of errors.
    /// </summary>
    public int ErrorCount => Errors.Count();

    /// <summary>
    /// Total number of warnings.
    /// </summary>
    public int WarningCount => Warnings.Count();

    /// <summary>
    /// Total number of issues.
    /// </summary>
    public int TotalIssueCount => Issues.Count;

    /// <summary>
    /// Summary message describing the validation result.
    /// </summary>
    public string SummaryMessage
    {
        get
        {
            if (IsValid && !HasWarnings)
                return "Proposal is valid and ready to apply.";

            if (IsValid && HasWarnings)
                return $"Proposal is valid with {WarningCount} warning(s).";

            return $"Proposal has {ErrorCount} error(s) that must be resolved.";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Query Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get all issues for a specific path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>Issues affecting that path.</returns>
    public IEnumerable<ValidationIssue> GetIssuesForPath(string path) =>
        Issues.Where(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Get all issues for a specific operation.
    /// </summary>
    /// <param name="operationId">The operation ID.</param>
    /// <returns>Issues for that operation.</returns>
    public IEnumerable<ValidationIssue> GetIssuesForOperation(Guid operationId) =>
        Issues.Where(i => i.OperationId == operationId);

    /// <summary>
    /// Get issues of a specific type.
    /// </summary>
    /// <param name="type">The issue type.</param>
    /// <returns>Issues of that type.</returns>
    public IEnumerable<ValidationIssue> GetIssuesByType(ValidationIssueType type) =>
        Issues.Where(i => i.Type == type);

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Create a valid result.
    /// </summary>
    public static ProposalValidationResult Valid() => new() { IsValid = true };

    /// <summary>
    /// Create a valid result with warnings.
    /// </summary>
    public static ProposalValidationResult ValidWithWarnings(params ValidationIssue[] warnings) => new()
    {
        IsValid = true,
        Issues = warnings.Where(w => w.Severity != ValidationSeverity.Error).ToList()
    };

    /// <summary>
    /// Create an invalid result with issues.
    /// </summary>
    public static ProposalValidationResult Invalid(params ValidationIssue[] issues) => new()
    {
        IsValid = false,
        Issues = issues
    };

    /// <summary>
    /// Create an invalid result with a single error.
    /// </summary>
    public static ProposalValidationResult InvalidWithError(
        string path,
        ValidationIssueType type,
        string message) => Invalid(ValidationIssue.Error(path, type, message));
}
