namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ VALIDATION ISSUE (v0.4.4a)                                               │
// │ Individual validation issue for file tree proposals.                    │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// A validation issue with a file operation.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public sealed class ValidationIssue
{
    /// <summary>
    /// ID of the affected operation.
    /// </summary>
    public Guid? OperationId { get; init; }

    /// <summary>
    /// Path of the affected file.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Type of issue.
    /// </summary>
    public ValidationIssueType Type { get; init; }

    /// <summary>
    /// Severity of the issue.
    /// </summary>
    public ValidationSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Suggested fix for the issue.
    /// </summary>
    public string? SuggestedFix { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Create an error issue.
    /// </summary>
    public static ValidationIssue Error(
        string path,
        ValidationIssueType type,
        string message,
        Guid? operationId = null,
        string? suggestedFix = null) => new()
    {
        OperationId = operationId,
        Path = path,
        Type = type,
        Severity = ValidationSeverity.Error,
        Message = message,
        SuggestedFix = suggestedFix
    };

    /// <summary>
    /// Create a warning issue.
    /// </summary>
    public static ValidationIssue Warning(
        string path,
        ValidationIssueType type,
        string message,
        Guid? operationId = null,
        string? suggestedFix = null) => new()
    {
        OperationId = operationId,
        Path = path,
        Type = type,
        Severity = ValidationSeverity.Warning,
        Message = message,
        SuggestedFix = suggestedFix
    };

    /// <summary>
    /// Create an info issue.
    /// </summary>
    public static ValidationIssue Info(
        string path,
        ValidationIssueType type,
        string message,
        Guid? operationId = null) => new()
    {
        OperationId = operationId,
        Path = path,
        Type = type,
        Severity = ValidationSeverity.Info,
        Message = message
    };
}
