namespace AIntern.Core.Interfaces;

using AIntern.Core.Events;
using AIntern.Core.Models;

/// <summary>
/// Service for inferring target file paths for code blocks (v0.4.1e).
/// </summary>
/// <remarks>
/// <para>Uses a cascade of strategies to determine the most likely target file:</para>
/// <list type="number">
///   <item><description>Explicit path from fence syntax or file comment</description></item>
///   <item><description>Single attached file context</description></item>
///   <item><description>Language matching with attached files</description></item>
///   <item><description>Type/class name matching</description></item>
///   <item><description>Content similarity analysis</description></item>
///   <item><description>Generate new path from extracted type name</description></item>
/// </list>
/// </remarks>
public interface IFilePathInferenceService
{
    #region Primary Methods

    /// <summary>
    /// Infer the target file path for a code block.
    /// </summary>
    /// <param name="block">The code block to analyze.</param>
    /// <param name="attachedContext">Files attached to the conversation.</param>
    /// <returns>Inference result with path, confidence, and strategy.</returns>
    PathInferenceResult InferTargetFilePath(
        CodeBlock block,
        IReadOnlyList<FileContext> attachedContext);

    /// <summary>
    /// Infer target paths for multiple code blocks at once.
    /// </summary>
    /// <remarks>
    /// Batch processing allows for cross-block analysis, such as
    /// detecting when multiple blocks target the same file.
    /// </remarks>
    IReadOnlyList<PathInferenceResult> InferTargetFilePaths(
        IReadOnlyList<CodeBlock> blocks,
        IReadOnlyList<FileContext> attachedContext);

    /// <summary>
    /// Extract the primary type/class name from code content.
    /// </summary>
    /// <param name="content">The code content.</param>
    /// <param name="language">The programming language.</param>
    /// <returns>Extracted type name or null.</returns>
    string? InferFileNameFromContent(string content, string? language);

    /// <summary>
    /// Get all type names declared in the content.
    /// </summary>
    IReadOnlyList<string> ExtractAllTypeNames(string content, string? language);

    /// <summary>
    /// Get the appropriate file extension for a language.
    /// </summary>
    string? GetExtensionForLanguage(string? language);

    #endregion

    #region Configuration

    /// <summary>
    /// Minimum confidence threshold for accepting an inferred path.
    /// Paths below this threshold will be marked as ambiguous.
    /// Default: 0.6
    /// </summary>
    float MinimumConfidenceThreshold { get; set; }

    /// <summary>
    /// Whether to suggest new file paths when no match is found.
    /// Default: true
    /// </summary>
    bool AllowNewFileSuggestions { get; set; }

    /// <summary>
    /// Base directory for new file suggestions (relative to workspace).
    /// Default: "src"
    /// </summary>
    string NewFileBaseDirectory { get; set; }

    #endregion

    #region Events

    /// <summary>
    /// Raised when a path is successfully inferred.
    /// </summary>
    event EventHandler<PathInferredEventArgs>? PathInferred;

    /// <summary>
    /// Raised when multiple paths match with similar confidence.
    /// </summary>
    event EventHandler<AmbiguousPathEventArgs>? InferenceAmbiguous;

    /// <summary>
    /// Raised when no path could be inferred.
    /// </summary>
    event EventHandler<PathInferenceFailedEventArgs>? InferenceFailed;

    #endregion
}
