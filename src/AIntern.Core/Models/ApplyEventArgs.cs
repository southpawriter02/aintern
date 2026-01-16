namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APPLY EVENT ARGS (v0.4.3a)                                               │
// │ Event arguments for apply-related notifications.                         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event arguments for when a file change has been applied.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3a.</para>
/// </remarks>
public sealed class FileChangedEventArgs : EventArgs
{
    /// <summary>
    /// The result of the apply operation.
    /// </summary>
    public ApplyResult Result { get; }

    /// <summary>
    /// The change record for undo tracking.
    /// </summary>
    public FileChangeRecord? ChangeRecord { get; init; }

    /// <summary>
    /// ID of the code block that was applied.
    /// </summary>
    public Guid? CodeBlockId { get; init; }

    public FileChangedEventArgs(ApplyResult result)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }
}

/// <summary>
/// Event arguments for when a file change has failed.
/// </summary>
public sealed class FileChangeFailedEventArgs : EventArgs
{
    /// <summary>
    /// Path to the file that failed to change.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Type of failure.
    /// </summary>
    public ApplyResultType ResultType { get; }

    /// <summary>
    /// Error message describing the failure.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Exception that caused the failure (if any).
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// ID of the code block that failed to apply.
    /// </summary>
    public Guid? CodeBlockId { get; init; }

    /// <summary>
    /// Whether the operation can be retried.
    /// </summary>
    public bool CanRetry => ResultType.IsRetryable();

    public FileChangeFailedEventArgs(
        string filePath,
        ApplyResultType resultType,
        string errorMessage)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        ResultType = resultType;
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
    }
}

/// <summary>
/// Event arguments for when a file change has been undone.
/// </summary>
public sealed class FileChangeUndoneEventArgs : EventArgs
{
    /// <summary>
    /// The change record that was undone.
    /// </summary>
    public FileChangeRecord ChangeRecord { get; }

    /// <summary>
    /// Whether the undo was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if undo failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public FileChangeUndoneEventArgs(FileChangeRecord changeRecord)
    {
        ChangeRecord = changeRecord ?? throw new ArgumentNullException(nameof(changeRecord));
    }
}

/// <summary>
/// Event arguments for when a file conflict is detected.
/// </summary>
public sealed class FileConflictDetectedEventArgs : EventArgs
{
    /// <summary>
    /// Path to the conflicting file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Relative path within the workspace.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// The conflict check result with details.
    /// </summary>
    public ConflictCheckResult ConflictResult { get; }

    /// <summary>
    /// ID of the code block that has the conflict.
    /// </summary>
    public Guid? CodeBlockId { get; init; }

    /// <summary>
    /// Whether the user has chosen to overwrite.
    /// Set by event handlers to indicate resolution choice.
    /// </summary>
    public bool AllowOverwrite { get; set; }

    public FileConflictDetectedEventArgs(
        string filePath,
        ConflictCheckResult conflictResult)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        ConflictResult = conflictResult ?? throw new ArgumentNullException(nameof(conflictResult));
    }
}
