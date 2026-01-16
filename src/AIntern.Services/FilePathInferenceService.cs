namespace AIntern.Services;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Services.Helpers;

/// <summary>
/// Service for inferring target file paths for code blocks (v0.4.1e).
/// </summary>
/// <remarks>
/// <para>
/// Uses a cascade of strategies to determine the most likely target file
/// for code blocks extracted from LLM responses. Each strategy has an
/// associated confidence score:
/// </para>
/// <list type="bullet">
/// <item>ExplicitPath: 1.0 (path was specified in the code fence)</item>
/// <item>SingleContext: 0.95 (only one file attached)</item>
/// <item>LanguageMatch: 0.80-0.85 (matched by language/extension)</item>
/// <item>TypeNameMatch: 0.75-0.90 (matched by class/type name)</item>
/// <item>ContentSimilarity: 0.60-0.70 (matched by namespace/content)</item>
/// <item>GeneratedNew: 0.50 (suggested new file from type name)</item>
/// </list>
/// </remarks>
public sealed class FilePathInferenceService : IFilePathInferenceService
{
    private readonly ILanguageDetectionService _languageService;
    private readonly ILogger<FilePathInferenceService>? _logger;

    /// <inheritdoc />
    public float MinimumConfidenceThreshold { get; set; } = 0.6f;

    /// <inheritdoc />
    public bool AllowNewFileSuggestions { get; set; } = true;

    /// <inheritdoc />
    public string NewFileBaseDirectory { get; set; } = "src";

    /// <inheritdoc />
    public event EventHandler<PathInferredEventArgs>? PathInferred;

    /// <inheritdoc />
    public event EventHandler<AmbiguousPathEventArgs>? InferenceAmbiguous;

    /// <inheritdoc />
    public event EventHandler<PathInferenceFailedEventArgs>? InferenceFailed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilePathInferenceService"/> class.
    /// </summary>
    /// <param name="languageService">Service for language detection and extension mapping.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public FilePathInferenceService(
        ILanguageDetectionService languageService,
        ILogger<FilePathInferenceService>? logger = null)
    {
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _logger = logger;
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ PRIMARY METHODS                                                          │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <inheritdoc />
    public PathInferenceResult InferTargetFilePath(
        CodeBlock block,
        IReadOnlyList<FileContext> attachedContext)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger?.LogDebug(
                "[INFO] Starting path inference for block {BlockId}, language={Language}, contextCount={ContextCount}",
                block.Id, block.Language ?? "null", attachedContext.Count);

            // ┌─────────────────────────────────────────────────────────────────┐
            // │ Strategy 1: Explicit path already set (highest priority)         │
            // └─────────────────────────────────────────────────────────────────┘
            if (!string.IsNullOrEmpty(block.TargetFilePath))
            {
                var result = PathInferenceResult.Success(
                    NormalizePath(block.TargetFilePath),
                    1.0f,
                    InferenceStrategy.ExplicitPath,
                    "Path explicitly specified in code block");

                _logger?.LogDebug("[INFO] Strategy 1 (ExplicitPath): {Path}", result.Path);
                RaisePathInferred(block, result, stopwatch.Elapsed);
                return result;
            }

            // ┌─────────────────────────────────────────────────────────────────┐
            // │ Strategy 2: Single attached context                              │
            // └─────────────────────────────────────────────────────────────────┘
            if (attachedContext.Count == 1)
            {
                var result = PathInferenceResult.Success(
                    attachedContext[0].FilePath,
                    0.95f,
                    InferenceStrategy.SingleContext,
                    "Single file attached to conversation");

                _logger?.LogDebug("[INFO] Strategy 2 (SingleContext): {Path}", result.Path);
                RaisePathInferred(block, result, stopwatch.Elapsed);
                return result;
            }

            // ┌─────────────────────────────────────────────────────────────────┐
            // │ Strategy 3: Language match                                       │
            // └─────────────────────────────────────────────────────────────────┘
            if (!string.IsNullOrEmpty(block.Language) && attachedContext.Count > 0)
            {
                var languageMatchResult = TryLanguageMatch(block, attachedContext);
                if (languageMatchResult != null)
                {
                    _logger?.LogDebug("[INFO] Strategy 3 (LanguageMatch): {Path}", languageMatchResult.Path);
                    RaisePathInferred(block, languageMatchResult, stopwatch.Elapsed);
                    return languageMatchResult;
                }
            }

            // ┌─────────────────────────────────────────────────────────────────┐
            // │ Strategy 4: Type name match                                      │
            // └─────────────────────────────────────────────────────────────────┘
            var typeName = InferFileNameFromContent(block.Content, block.Language);
            if (!string.IsNullOrEmpty(typeName) && attachedContext.Count > 0)
            {
                var typeMatchResult = TryTypeNameMatch(typeName, block, attachedContext);
                if (typeMatchResult != null)
                {
                    _logger?.LogDebug("[INFO] Strategy 4 (TypeNameMatch): {Path}", typeMatchResult.Path);
                    RaisePathInferred(block, typeMatchResult, stopwatch.Elapsed);
                    return typeMatchResult;
                }
            }

            // ┌─────────────────────────────────────────────────────────────────┐
            // │ Strategy 5: Content similarity (requires attached context)       │
            // └─────────────────────────────────────────────────────────────────┘
            if (attachedContext.Count > 0)
            {
                var similarityResult = TryContentSimilarity(block, attachedContext);
                if (similarityResult != null)
                {
                    _logger?.LogDebug("[INFO] Strategy 5 (ContentSimilarity): {Path}", similarityResult.Path);
                    RaisePathInferred(block, similarityResult, stopwatch.Elapsed);
                    return similarityResult;
                }
            }

            // ┌─────────────────────────────────────────────────────────────────┐
            // │ Strategy 6: Generate new path                                    │
            // └─────────────────────────────────────────────────────────────────┘
            if (AllowNewFileSuggestions && !string.IsNullOrEmpty(typeName))
            {
                var newFileResult = GenerateNewFilePath(typeName, block);
                if (newFileResult != null)
                {
                    _logger?.LogDebug("[INFO] Strategy 6 (GeneratedNew): {Path}", newFileResult.Path);
                    RaisePathInferred(block, newFileResult, stopwatch.Elapsed);
                    return newFileResult;
                }
            }

            // ┌─────────────────────────────────────────────────────────────────┐
            // │ No path could be inferred                                        │
            // └─────────────────────────────────────────────────────────────────┘
            var notFoundResult = PathInferenceResult.NotFound(
                "Could not determine target file from content or context");

            _logger?.LogDebug("[INFO] No path inferred for block {BlockId}", block.Id);
            RaiseInferenceFailed(block, notFoundResult.Explanation!);
            return notFoundResult;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] Error during path inference for block {BlockId}", block.Id);
            var errorResult = PathInferenceResult.NotFound($"Inference error: {ex.Message}");
            RaiseInferenceFailed(block, errorResult.Explanation!);
            return errorResult;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PathInferenceResult> InferTargetFilePaths(
        IReadOnlyList<CodeBlock> blocks,
        IReadOnlyList<FileContext> attachedContext)
    {
        _logger?.LogDebug(
            "[INFO] Batch inferring paths for {BlockCount} blocks with {ContextCount} attached files",
            blocks.Count, attachedContext.Count);

        var results = new List<PathInferenceResult>(blocks.Count);
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var block in blocks)
        {
            var result = InferTargetFilePath(block, attachedContext);

            // Track used paths for conflict detection
            if (result.IsSuccess && !result.IsNewFile)
            {
                if (usedPaths.Contains(result.Path!))
                {
                    _logger?.LogDebug(
                        "[INFO] Multiple blocks target same file: {Path}",
                        result.Path);
                }
                usedPaths.Add(result.Path!);
            }

            results.Add(result);
        }

        return results;
    }

    /// <inheritdoc />
    public string? InferFileNameFromContent(string content, string? language)
    {
        return TypeNameExtractor.ExtractPrimaryTypeName(content, language);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ExtractAllTypeNames(string content, string? language)
    {
        return TypeNameExtractor.ExtractAllTypeNames(content, language);
    }

    /// <inheritdoc />
    public string? GetExtensionForLanguage(string? language)
    {
        if (string.IsNullOrEmpty(language))
            return null;

        return _languageService.GetFileExtension(language);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ INFERENCE STRATEGIES                                                     │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Strategy 3: Match by programming language.
    /// </summary>
    private PathInferenceResult? TryLanguageMatch(
        CodeBlock block,
        IReadOnlyList<FileContext> contexts)
    {
        var matchingFiles = new List<(string Path, float Confidence)>();

        foreach (var ctx in contexts)
        {
            // Match by explicit language property
            if (!string.IsNullOrEmpty(ctx.Language) &&
                ctx.Language.Equals(block.Language, StringComparison.OrdinalIgnoreCase))
            {
                matchingFiles.Add((ctx.FilePath, 0.85f));
                continue;
            }

            // Match by file extension
            var ext = Path.GetExtension(ctx.FilePath);
            var langFromExt = _languageService.GetLanguageForExtension(ext);
            if (!string.IsNullOrEmpty(langFromExt) &&
                langFromExt.Equals(block.Language, StringComparison.OrdinalIgnoreCase))
            {
                matchingFiles.Add((ctx.FilePath, 0.80f));
            }
        }

        if (matchingFiles.Count == 0)
            return null;

        // Single match - return it
        if (matchingFiles.Count == 1)
        {
            return PathInferenceResult.Success(
                matchingFiles[0].Path,
                matchingFiles[0].Confidence,
                InferenceStrategy.LanguageMatch,
                $"Matched by language: {block.Language}");
        }

        // Multiple matches - raise ambiguous event
        if (matchingFiles.Count <= 3)
        {
            var ambiguousArgs = new AmbiguousPathEventArgs
            {
                CodeBlock = block,
                PossiblePaths = matchingFiles.Select(m => m.Path).ToList(),
                Confidences = matchingFiles.Select(m => m.Confidence).ToList()
            };

            InferenceAmbiguous?.Invoke(this, ambiguousArgs);

            // If handler selected a path, use it
            if (!string.IsNullOrEmpty(ambiguousArgs.SelectedPath))
            {
                return PathInferenceResult.Success(
                    ambiguousArgs.SelectedPath,
                    0.85f,
                    InferenceStrategy.LanguageMatch,
                    "User selected from ambiguous matches");
            }

            return PathInferenceResult.Ambiguous(
                matchingFiles.Select(m => m.Path).ToList(),
                $"Multiple {block.Language} files attached");
        }

        // Too many matches to be useful
        return null;
    }

    /// <summary>
    /// Strategy 4: Match by extracted type/class name.
    /// </summary>
    private PathInferenceResult? TryTypeNameMatch(
        string typeName,
        CodeBlock block,
        IReadOnlyList<FileContext> contexts)
    {
        foreach (var ctx in contexts)
        {
            var fileName = Path.GetFileNameWithoutExtension(ctx.FilePath);

            // Exact match (highest confidence)
            if (fileName.Equals(typeName, StringComparison.OrdinalIgnoreCase))
            {
                return PathInferenceResult.Success(
                    ctx.FilePath,
                    0.90f,
                    InferenceStrategy.TypeNameMatch,
                    $"Exact type name match: {typeName}");
            }

            // Partial match (file contains type name or vice versa)
            if (fileName.Contains(typeName, StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return PathInferenceResult.Success(
                    ctx.FilePath,
                    0.75f,
                    InferenceStrategy.TypeNameMatch,
                    $"Partial type name match: {typeName} ~ {fileName}");
            }
        }

        return null;
    }

    /// <summary>
    /// Strategy 5: Match by content similarity (namespace, imports).
    /// </summary>
    private PathInferenceResult? TryContentSimilarity(
        CodeBlock block,
        IReadOnlyList<FileContext> contexts)
    {
        // Extract namespace/imports from block
        var blockNamespace = TypeNameExtractor.ExtractNamespace(
            block.Content, block.Language);

        if (string.IsNullOrEmpty(blockNamespace))
            return null;

        foreach (var ctx in contexts)
        {
            if (string.IsNullOrEmpty(ctx.Content))
                continue;

            // Check if context file has matching namespace
            var ctxNamespace = TypeNameExtractor.ExtractNamespace(
                ctx.Content, ctx.Language ?? block.Language);

            if (!string.IsNullOrEmpty(ctxNamespace) &&
                blockNamespace.Equals(ctxNamespace, StringComparison.OrdinalIgnoreCase))
            {
                return PathInferenceResult.Success(
                    ctx.FilePath,
                    0.70f,
                    InferenceStrategy.ContentSimilarity,
                    $"Matching namespace: {blockNamespace}");
            }

            // Check if block appears to be modifying code from context
            var contentLines = block.Content.Split('\n');
            if (contentLines.Length > 0 &&
                ctx.Content.Contains(contentLines[0].Trim(), StringComparison.Ordinal))
            {
                return PathInferenceResult.Success(
                    ctx.FilePath,
                    0.60f,
                    InferenceStrategy.ContentSimilarity,
                    "Content appears to modify attached file");
            }
        }

        return null;
    }

    /// <summary>
    /// Strategy 6: Generate a new file path from extracted type name.
    /// </summary>
    private PathInferenceResult? GenerateNewFilePath(string typeName, CodeBlock block)
    {
        var extension = GetExtensionForLanguage(block.Language);
        if (string.IsNullOrEmpty(extension))
            return null;

        // Try to extract namespace for path generation
        var namespaceStr = TypeNameExtractor.ExtractNamespace(
            block.Content, block.Language);

        string path;
        if (!string.IsNullOrEmpty(namespaceStr))
        {
            // Convert namespace to path structure
            path = TypeNameExtractor.BuildSuggestedPath(
                typeName, namespaceStr, extension);
        }
        else
        {
            // Use base directory + type name
            path = string.IsNullOrEmpty(NewFileBaseDirectory)
                ? $"{typeName}{extension}"
                : $"{NewFileBaseDirectory}/{typeName}{extension}";
        }

        return PathInferenceResult.NewFile(
            NormalizePath(path),
            0.50f,
            typeName,
            $"Suggested new file for type: {typeName}");
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ HELPER METHODS                                                           │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Normalize a file path (convert backslashes, trim leading slashes).
    /// </summary>
    private static string NormalizePath(string path)
    {
        return path.Trim()
            .Replace('\\', '/')
            .TrimStart('/');
    }

    /// <summary>
    /// Raise the PathInferred event.
    /// </summary>
    private void RaisePathInferred(
        CodeBlock block,
        PathInferenceResult result,
        TimeSpan duration)
    {
        _logger?.LogDebug(
            "[INFO] Inferred path for block {BlockId}: {Path} (confidence: {Confidence}, strategy: {Strategy}, duration: {Duration}ms)",
            block.Id, result.Path, result.Confidence, result.Strategy, duration.TotalMilliseconds);

        PathInferred?.Invoke(this, new PathInferredEventArgs
        {
            CodeBlock = block,
            Result = result,
            Duration = duration
        });
    }

    /// <summary>
    /// Raise the InferenceFailed event.
    /// </summary>
    private void RaiseInferenceFailed(CodeBlock block, string reason)
    {
        _logger?.LogDebug(
            "[INFO] Path inference failed for block {BlockId}: {Reason}",
            block.Id, reason);

        var args = new PathInferenceFailedEventArgs
        {
            CodeBlock = block,
            Reason = reason
        };

        InferenceFailed?.Invoke(this, args);

        // Handler could have provided a manual path
        if (!string.IsNullOrEmpty(args.ManualPath))
        {
            _logger?.LogDebug(
                "[INFO] Manual path provided for block {BlockId}: {Path}",
                block.Id, args.ManualPath);
        }
    }
}
