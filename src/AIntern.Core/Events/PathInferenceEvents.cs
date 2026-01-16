namespace AIntern.Core.Events;

using AIntern.Core.Models;

/// <summary>
/// Event args for when a path is successfully inferred (v0.4.1e).
/// </summary>
public sealed class PathInferredEventArgs : EventArgs
{
    /// <summary>
    /// The code block that was analyzed.
    /// </summary>
    public required CodeBlock CodeBlock { get; init; }

    /// <summary>
    /// The inference result.
    /// </summary>
    public required PathInferenceResult Result { get; init; }

    /// <summary>
    /// Time taken to perform inference.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Event args for when path inference is ambiguous (v0.4.1e).
/// </summary>
public sealed class AmbiguousPathEventArgs : EventArgs
{
    /// <summary>
    /// The code block that was analyzed.
    /// </summary>
    public required CodeBlock CodeBlock { get; init; }

    /// <summary>
    /// The possible target paths.
    /// </summary>
    public required IReadOnlyList<string> PossiblePaths { get; init; }

    /// <summary>
    /// Confidence scores for each path.
    /// </summary>
    public required IReadOnlyList<float> Confidences { get; init; }

    /// <summary>
    /// Set this to the selected path to resolve the ambiguity.
    /// If not set, the first path will be used.
    /// </summary>
    public string? SelectedPath { get; set; }
}

/// <summary>
/// Event args for when path inference fails (v0.4.1e).
/// </summary>
public sealed class PathInferenceFailedEventArgs : EventArgs
{
    /// <summary>
    /// The code block that was analyzed.
    /// </summary>
    public required CodeBlock CodeBlock { get; init; }

    /// <summary>
    /// Reason for failure.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Set this to manually specify a path.
    /// </summary>
    public string? ManualPath { get; set; }
}
