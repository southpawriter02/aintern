namespace AIntern.Core.Models;

/// <summary>
/// Result of a file path inference operation (v0.4.1e).
/// </summary>
/// <remarks>
/// <para>
/// Contains the inferred path, confidence score, and information about
/// which strategy produced the result. Factory methods are provided for
/// common result types.
/// </para>
/// </remarks>
public sealed class PathInferenceResult
{
    /// <summary>
    /// The inferred file path (relative to workspace root).
    /// Null if no path could be inferred.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Confidence score for the inference (0.0 to 1.0).
    /// Higher values indicate more reliable inference.
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// The strategy that produced this result.
    /// </summary>
    public InferenceStrategy Strategy { get; init; }

    /// <summary>
    /// Whether a path was successfully inferred.
    /// </summary>
    public bool IsSuccess => !string.IsNullOrEmpty(Path);

    /// <summary>
    /// Whether this inference is ambiguous (multiple possible matches).
    /// </summary>
    public bool IsAmbiguous { get; init; }

    /// <summary>
    /// Alternative paths if the inference was ambiguous.
    /// </summary>
    public IReadOnlyList<string> AlternativePaths { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this path represents a new file to be created.
    /// </summary>
    public bool IsNewFile { get; init; }

    /// <summary>
    /// The type name extracted from content (if applicable).
    /// </summary>
    public string? ExtractedTypeName { get; init; }

    /// <summary>
    /// Human-readable explanation of how the path was inferred.
    /// </summary>
    public string? Explanation { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="path">The inferred file path.</param>
    /// <param name="confidence">Confidence score (0.0 to 1.0).</param>
    /// <param name="strategy">The strategy that produced the result.</param>
    /// <param name="explanation">Optional explanation.</param>
    public static PathInferenceResult Success(
        string path,
        float confidence,
        InferenceStrategy strategy,
        string? explanation = null) => new()
    {
        Path = path,
        Confidence = confidence,
        Strategy = strategy,
        Explanation = explanation
    };

    /// <summary>
    /// Creates a result indicating no path could be inferred.
    /// </summary>
    /// <param name="explanation">Optional explanation of why inference failed.</param>
    public static PathInferenceResult NotFound(string? explanation = null) => new()
    {
        Path = null,
        Confidence = 0,
        Strategy = InferenceStrategy.None,
        Explanation = explanation ?? "Unable to infer target file path"
    };

    /// <summary>
    /// Creates an ambiguous result with multiple possible paths.
    /// </summary>
    /// <param name="paths">The possible target paths.</param>
    /// <param name="explanation">Optional explanation.</param>
    public static PathInferenceResult Ambiguous(
        IReadOnlyList<string> paths,
        string? explanation = null) => new()
    {
        Path = paths.Count > 0 ? paths[0] : null,
        Confidence = 0.5f,
        Strategy = InferenceStrategy.Ambiguous,
        IsAmbiguous = true,
        AlternativePaths = paths,
        Explanation = explanation ?? $"Multiple possible targets: {string.Join(", ", paths)}"
    };

    /// <summary>
    /// Creates a result for a new file to be created.
    /// </summary>
    /// <param name="path">The suggested path for the new file.</param>
    /// <param name="confidence">Confidence score.</param>
    /// <param name="typeName">The type name extracted from content.</param>
    /// <param name="explanation">Optional explanation.</param>
    public static PathInferenceResult NewFile(
        string path,
        float confidence,
        string? typeName = null,
        string? explanation = null) => new()
    {
        Path = path,
        Confidence = confidence,
        Strategy = InferenceStrategy.GeneratedNew,
        IsNewFile = true,
        ExtractedTypeName = typeName,
        Explanation = explanation ?? $"New file will be created: {path}"
    };
}

/// <summary>
/// Describes which strategy was used to infer a file path (v0.4.1e).
/// </summary>
public enum InferenceStrategy
{
    /// <summary>No strategy was applicable.</summary>
    None,

    /// <summary>Path was explicitly specified in fence or comment.</summary>
    ExplicitPath,

    /// <summary>Single file was attached to conversation.</summary>
    SingleContext,

    /// <summary>Matched by programming language.</summary>
    LanguageMatch,

    /// <summary>Matched by extracted type/class name.</summary>
    TypeNameMatch,

    /// <summary>Matched by content similarity analysis.</summary>
    ContentSimilarity,

    /// <summary>Generated a new path from type name.</summary>
    GeneratedNew,

    /// <summary>Multiple paths matched with similar confidence.</summary>
    Ambiguous
}
