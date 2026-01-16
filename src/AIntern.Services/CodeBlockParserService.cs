namespace AIntern.Services;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

/// <summary>
/// Parses code blocks from markdown-formatted LLM responses (v0.4.1b).
/// </summary>
public sealed partial class CodeBlockParserService : ICodeBlockParserService
{
    private readonly ILogger<CodeBlockParserService>? _logger;

    // === Source-Generated Regex Patterns ===

    /// <summary>
    /// Matches fenced code blocks: ```lang:path\ncode```
    /// Groups: lang (optional), path (optional), code
    /// </summary>
    [GeneratedRegex(
        @"```(?<lang>[\w\-+#]+)?(?::(?<path>[^\n\r]+))?\r?\n(?<code>[\s\S]*?)```",
        RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex FencedBlockPattern();

    /// <summary>
    /// Matches file path comments in first lines.
    /// Supports: //, #, --, /* */, <!-- -->
    /// </summary>
    [GeneratedRegex(
        @"^(?://|#|--|/\*|<!--)\s*(?:File|Path|Filename):\s*(?<path>.+?)(?:\s*\*/|-->)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex FileCommentPattern();

    /// <summary>
    /// Language display name mappings.
    /// </summary>
    private static readonly Dictionary<string, string> LanguageDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "csharp", "C#" }, { "cs", "C#" },
        { "javascript", "JavaScript" }, { "js", "JavaScript" },
        { "typescript", "TypeScript" }, { "ts", "TypeScript" },
        { "python", "Python" }, { "py", "Python" },
        { "java", "Java" },
        { "cpp", "C++" }, { "c++", "C++" },
        { "c", "C" },
        { "bash", "Bash" }, { "sh", "Bash" }, { "shell", "Bash" },
        { "powershell", "PowerShell" }, { "ps1", "PowerShell" },
        { "sql", "SQL" },
        { "json", "JSON" },
        { "xml", "XML" },
        { "yaml", "YAML" }, { "yml", "YAML" },
        { "html", "HTML" },
        { "css", "CSS" },
        { "markdown", "Markdown" }, { "md", "Markdown" },
        { "rust", "Rust" }, { "rs", "Rust" },
        { "go", "Go" }
    };

    /// <summary>
    /// Language normalization mappings.
    /// </summary>
    private static readonly Dictionary<string, string> LanguageNormalization = new(StringComparer.OrdinalIgnoreCase)
    {
        { "cs", "csharp" },
        { "js", "javascript" },
        { "ts", "typescript" },
        { "py", "python" },
        { "c++", "cpp" },
        { "sh", "bash" }, { "shell", "bash" },
        { "ps1", "powershell" },
        { "yml", "yaml" },
        { "md", "markdown" },
        { "rs", "rust" }
    };

    /// <summary>
    /// Command-type languages.
    /// </summary>
    private static readonly HashSet<string> CommandLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "bash", "sh", "shell", "powershell", "ps1", "cmd", "zsh", "fish"
    };

    /// <summary>
    /// Output/log-type patterns in fence language.
    /// </summary>
    private static readonly HashSet<string> OutputLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "output", "log", "console", "terminal", "stdout", "stderr", "text", "plaintext"
    };

    public CodeBlockParserService(ILogger<CodeBlockParserService>? logger = null)
    {
        _logger = logger;
    }

    public IReadOnlyList<CodeBlock> ParseMessage(string content, Guid messageId)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<CodeBlock>();

        var blocks = new List<CodeBlock>();
        var matches = FencedBlockPattern().Matches(content);

        _logger?.LogDebug("[INFO] Found {Count} code block(s) in message {MessageId}",
            matches.Count, messageId);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];

            try
            {
                var block = ExtractBlockFromMatch(match, messageId, i, content);
                if (block != null)
                {
                    blocks.Add(block);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "[WARN] Failed to parse code block {Index} in message {MessageId}",
                    i, messageId);
            }
        }

        return blocks;
    }

    public CodeProposal CreateProposal(
        string content,
        Guid messageId,
        IReadOnlyList<string>? attachedFilePaths = null)
    {
        var blocks = ParseMessage(content, messageId);

        _logger?.LogDebug("[INFO] Created proposal with {Count} blocks for message {MessageId}",
            blocks.Count, messageId);

        return new CodeProposal
        {
            MessageId = messageId,
            CodeBlocks = blocks
        };
    }

    public CodeBlock? ParseSingleBlock(string fenceContent, Guid messageId, int sequenceNumber)
    {
        var match = FencedBlockPattern().Match(fenceContent);
        if (!match.Success)
            return null;

        return ExtractBlockFromMatch(match, messageId, sequenceNumber, fenceContent);
    }

    public bool ContainsCodeBlocks(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return FencedBlockPattern().IsMatch(content);
    }

    public int CountCodeBlocks(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return FencedBlockPattern().Count(content);
    }

    // === Private Helper Methods ===

    private CodeBlock? ExtractBlockFromMatch(
        Match match,
        Guid messageId,
        int sequenceNumber,
        string fullContent)
    {
        var rawCode = match.Groups["code"].Value;
        var langSpec = match.Groups["lang"].Value;
        var pathFromFence = match.Groups["path"].Value?.Trim();

        // Skip empty code blocks
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            _logger?.LogDebug("[INFO] Skipping empty code block at position {Position}",
                match.Index);
            return null;
        }

        // Extract file path from first-line comment if not in fence
        var pathFromComment = ExtractPathFromComment(rawCode);
        var targetPath = !string.IsNullOrEmpty(pathFromFence)
            ? NormalizePath(pathFromFence)
            : !string.IsNullOrEmpty(pathFromComment)
                ? NormalizePath(pathFromComment)
                : null;

        // Remove the path comment from content if present
        var cleanedCode = pathFromComment != null
            ? RemovePathComment(rawCode)
            : rawCode;

        // Detect/normalize language
        var (language, displayLanguage) = DetectLanguage(langSpec, targetPath);

        // Classify block type
        var blockType = ClassifyBlockType(cleanedCode, language, langSpec);

        return new CodeBlock
        {
            Content = cleanedCode.Trim(),
            Language = language,
            DisplayLanguage = displayLanguage,
            TargetFilePath = targetPath,
            BlockType = blockType,
            MessageId = messageId,
            SequenceNumber = sequenceNumber,
            SourceRange = new TextRange(match.Index, match.Index + match.Length),
            ConfidenceScore = CalculateConfidence(langSpec, targetPath, blockType)
        };
    }

    private static string? ExtractPathFromComment(string code)
    {
        // Check first few lines for file path comment
        var lines = code.Split('\n', 5);
        foreach (var line in lines.Take(3))
        {
            var match = FileCommentPattern().Match(line);
            if (match.Success)
                return match.Groups["path"].Value.Trim();
        }
        return null;
    }

    private static string RemovePathComment(string code)
    {
        var lines = code.Split('\n');
        var result = new List<string>();
        bool foundPath = false;

        foreach (var line in lines)
        {
            if (!foundPath && FileCommentPattern().IsMatch(line))
            {
                foundPath = true;
                continue; // Skip the path comment line
            }
            result.Add(line);
        }

        return string.Join('\n', result);
    }

    private static string NormalizePath(string path)
    {
        // Normalize path separators and remove leading/trailing whitespace
        return path.Trim()
            .Replace('\\', '/')
            .TrimStart('/');
    }

    private static (string? language, string? displayLanguage) DetectLanguage(
        string? langSpec,
        string? targetPath)
    {
        if (string.IsNullOrEmpty(langSpec))
        {
            // Try to infer from file extension
            if (!string.IsNullOrEmpty(targetPath))
            {
                var ext = Path.GetExtension(targetPath).TrimStart('.');
                langSpec = ext switch
                {
                    "cs" => "csharp",
                    "js" => "javascript",
                    "ts" => "typescript",
                    "py" => "python",
                    "rs" => "rust",
                    _ => ext
                };
            }
            else
            {
                return (null, null);
            }
        }

        // Normalize language
        var normalized = LanguageNormalization.TryGetValue(langSpec, out var norm) ? norm : langSpec.ToLowerInvariant();

        // Get display name
        var display = LanguageDisplayNames.TryGetValue(normalized, out var disp) ? disp : null;

        return (normalized, display);
    }

    private static CodeBlockType ClassifyBlockType(string code, string? language, string? langSpec)
    {
        // Check for command languages
        if (!string.IsNullOrEmpty(langSpec) && CommandLanguages.Contains(langSpec))
            return CodeBlockType.Command;

        // Check for output/log languages
        if (!string.IsNullOrEmpty(langSpec) && OutputLanguages.Contains(langSpec))
            return CodeBlockType.Output;

        // Check for config file patterns
        if (!string.IsNullOrEmpty(language) && language is "json" or "yaml" or "xml" or "toml")
            return CodeBlockType.Config;

        // Check code structure for complete file indicators
        if (HasCompleteFileStructure(code, language))
            return CodeBlockType.CompleteFile;

        return CodeBlockType.Snippet;
    }

    private static bool HasCompleteFileStructure(string code, string? language)
    {
        return language switch
        {
            "csharp" => code.Contains("namespace ") || code.Contains("using "),
            "java" => code.Contains("package ") || code.Contains("import "),
            "python" => code.Contains("def ") || code.Contains("class ") || code.Contains("import "),
            "javascript" or "typescript" => code.Contains("export ") || code.Contains("import "),
            _ => false
        };
    }

    private static float CalculateConfidence(
        string? langSpec,
        string? targetPath,
        CodeBlockType blockType)
    {
        float confidence = 1.0f;

        // Reduce confidence if language wasn't explicitly specified
        if (string.IsNullOrEmpty(langSpec))
            confidence -= 0.1f;

        // Reduce confidence for example blocks
        if (blockType == CodeBlockType.Example)
            confidence -= 0.2f;

        // Reduce confidence if no target path
        if (string.IsNullOrEmpty(targetPath))
            confidence -= 0.3f;

        return Math.Max(0.1f, confidence);
    }
}
