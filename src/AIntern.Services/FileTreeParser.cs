using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PARSER (v0.4.4b)                                               │
// │ Parses LLM responses to detect and extract multi-file proposals.        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Parses LLM responses to detect and extract multi-file proposals.
/// </summary>
/// <remarks>
/// <para>
/// Uses source-generated regex for performance.
/// Handles multiple tree formats and edge cases.
/// </para>
/// <para>Added in v0.4.4b.</para>
/// </remarks>
public sealed partial class FileTreeParser : IFileTreeParser
{
    private readonly FileTreeParserOptions _options;
    private readonly ILogger<FileTreeParser>? _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // Regex Patterns (Source-Generated)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Pattern for fenced code blocks containing tree structures.
    /// Matches any fenced code block with tree-like characters.
    /// </summary>
    [GeneratedRegex(
        @"```(?:text|tree|plaintext|ascii|structure)?\s*\r?\n([\s\S]*?[│├└┬┼─|+`\-\\][\s\S]*?)```",
        RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex TreeBlockPattern();

    /// <summary>
    /// Pattern for standard tree lines with Unicode box-drawing.
    /// </summary>
    [GeneratedRegex(
        @"^[\s│]*[├└](?:──|─)\s*(.+?)(?:\s*#.*)?$",
        RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex TreeLinePattern();

    /// <summary>
    /// Pattern for ASCII-only tree lines.
    /// </summary>
    [GeneratedRegex(
        @"^[\s|]*[+`](?:--|-)\s*(.+?)(?:\s*#.*)?$",
        RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex AsciiTreeLinePattern();

    /// <summary>
    /// Pattern for simple indented directory listing.
    /// </summary>
    [GeneratedRegex(
        @"^(\s*)([a-zA-Z_][\w\-\.]*(?:/[\w\-\.]+)*/?)$",
        RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex SimpleListingPattern();

    /// <summary>
    /// Pattern for root directory line.
    /// </summary>
    [GeneratedRegex(
        @"^([a-zA-Z_][\w\-\.]*)/?(?:\s*#.*)?$",
        RegexOptions.Compiled)]
    private static partial Regex RootDirectoryPattern();

    /// <summary>
    /// Pattern for inline comments.
    /// </summary>
    [GeneratedRegex(
        @"\s*#\s*(.+)$",
        RegexOptions.Compiled)]
    private static partial Regex InlineCommentPattern();

    /// <summary>
    /// Pattern for markdown formatting to remove.
    /// </summary>
    [GeneratedRegex(
        @"^#+\s*|[*_`\[\]]",
        RegexOptions.Compiled)]
    private static partial Regex MarkdownFormattingPattern();

    /// <summary>
    /// Pattern for path validation (valid characters).
    /// </summary>
    [GeneratedRegex(
        @"^[\w\-\./\\@]+$",
        RegexOptions.Compiled)]
    private static partial Regex ValidPathPattern();

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTreeParser"/> class.
    /// </summary>
    /// <param name="options">Parser options (optional).</param>
    /// <param name="logger">Logger instance (optional).</param>
    public FileTreeParser(
        FileTreeParserOptions? options = null,
        ILogger<FileTreeParser>? logger = null)
    {
        _options = options ?? FileTreeParserOptions.Default;
        _logger = logger;

        _logger?.LogDebug(
            "FileTreeParser initialized with MinFiles={Min}, MaxDepth={Depth}",
            _options.MinimumFilesForProposal,
            _options.MaxTreeDepth);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IFileTreeParser Implementation
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public FileTreeProposal? ParseProposal(
        string content,
        Guid messageId,
        IReadOnlyList<CodeBlock> codeBlocks)
    {
        TryParseProposal(content, messageId, codeBlocks, out var proposal, out _);
        return proposal;
    }

    /// <inheritdoc/>
    public bool TryParseProposal(
        string content,
        Guid messageId,
        IReadOnlyList<CodeBlock> codeBlocks,
        out FileTreeProposal? proposal,
        out string? reason)
    {
        proposal = null;
        reason = null;

        _logger?.LogDebug("Attempting to parse proposal for message {MessageId}", messageId);

        // Validate inputs
        if (string.IsNullOrWhiteSpace(content))
        {
            reason = "Content is empty";
            _logger?.LogDebug("Parse failed: {Reason}", reason);
            return false;
        }

        if (codeBlocks == null || codeBlocks.Count == 0)
        {
            reason = "No code blocks provided";
            _logger?.LogDebug("Parse failed: {Reason}", reason);
            return false;
        }

        // Filter to code blocks with file paths
        var blocksWithPaths = codeBlocks
            .Where(b => !string.IsNullOrEmpty(b.TargetFilePath))
            .Where(b => _options.IncludedBlockTypes.Contains(b.BlockType))
            .ToList();

        _logger?.LogDebug(
            "Found {Count} code blocks with paths out of {Total}",
            blocksWithPaths.Count,
            codeBlocks.Count);

        if (blocksWithPaths.Count < _options.MinimumFilesForProposal)
        {
            reason = $"Only {blocksWithPaths.Count} code blocks with paths found " +
                     $"(minimum: {_options.MinimumFilesForProposal})";
            _logger?.LogDebug("Proposal rejected: {Reason}", reason);
            return false;
        }

        if (blocksWithPaths.Count > _options.MaxFilesInProposal)
        {
            reason = $"Too many files ({blocksWithPaths.Count}) exceeds maximum " +
                     $"({_options.MaxFilesInProposal})";
            _logger?.LogWarning("Proposal rejected: {Reason}", reason);
            return false;
        }

        // Check for explicit ASCII file tree
        string? rawTreeText = null;
        var treePaths = new List<string>();

        if (ContainsFileTree(content))
        {
            var treeMatch = TreeBlockPattern().Match(content);
            if (treeMatch.Success)
            {
                rawTreeText = treeMatch.Groups[1].Value;
                treePaths = ParseAsciiTree(rawTreeText).ToList();

                _logger?.LogDebug(
                    "Parsed ASCII tree with {PathCount} paths",
                    treePaths.Count);
            }
        }

        // Build operations from code blocks
        var operations = new List<FileOperation>();
        int order = 0;

        foreach (var block in blocksWithPaths)
        {
            var operation = FileOperation.FromCodeBlock(block, order++);

            // Validate path
            if (!IsValidPath(operation.Path))
            {
                _logger?.LogWarning(
                    "Skipping invalid path: {Path}",
                    operation.Path);
                continue;
            }

            operations.Add(operation);
        }

        if (operations.Count < _options.MinimumFilesForProposal)
        {
            reason = $"Only {operations.Count} valid operations after filtering";
            _logger?.LogDebug("Proposal rejected: {Reason}", reason);
            return false;
        }

        // Find common root path
        var rootPath = FindCommonRoot(operations.Select(o => o.Path));

        // Extract description
        var description = ExtractDescription(content);

        proposal = new FileTreeProposal
        {
            MessageId = messageId,
            RootPath = rootPath,
            Operations = operations,
            Description = description,
            RawTreeText = _options.PreserveRawTreeText ? rawTreeText : null
        };

        _logger?.LogInformation(
            "Created proposal with {FileCount} files in root '{Root}'",
            operations.Count,
            rootPath);

        return true;
    }

    /// <inheritdoc/>
    public bool ContainsFileTree(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var lowerContent = content.ToLowerInvariant();

        // Check for structure indicators if required
        if (_options.RequireStructureIndicator)
        {
            var hasIndicator = _options.StructureIndicators
                .Any(ind => lowerContent.Contains(ind.ToLowerInvariant()));

            if (!hasIndicator)
            {
                _logger?.LogDebug("No structure indicator found in content");
                return false;
            }
        }

        // Check for tree block pattern
        var hasTreeBlock = TreeBlockPattern().IsMatch(content);
        _logger?.LogDebug("Tree block pattern match: {HasMatch}", hasTreeBlock);

        return hasTreeBlock;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> ParseAsciiTree(string treeContent)
    {
        if (string.IsNullOrWhiteSpace(treeContent))
        {
            _logger?.LogDebug("Empty tree content provided");
            return Array.Empty<string>();
        }

        var paths = new List<string>();
        var lines = treeContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var pathStack = new Stack<(int Depth, string Name, bool IsDir)>();

        _logger?.LogDebug("Parsing {LineCount} tree lines", lines.Length);

        foreach (var line in lines)
        {
            var parseResult = ParseTreeLine(line);
            if (parseResult == null)
                continue;

            var (name, depth, isDirectory, comment) = parseResult.Value;

            // Enforce max depth
            if (depth > _options.MaxTreeDepth)
            {
                _logger?.LogWarning(
                    "Tree depth {Depth} exceeds maximum {Max}",
                    depth,
                    _options.MaxTreeDepth);
                continue;
            }

            // Pop items from stack until we're at the right level
            while (pathStack.Count > 0 && pathStack.Peek().Depth >= depth)
            {
                pathStack.Pop();
            }

            // Handle root directory specially
            if (depth == 0 && isDirectory && pathStack.Count == 0)
            {
                pathStack.Push((depth, name.TrimEnd('/'), true));
                continue;
            }

            pathStack.Push((depth, name.TrimEnd('/'), isDirectory));

            // Build full path from stack
            var pathParts = pathStack.Reverse().Select(p => p.Name).ToList();
            var fullPath = string.Join("/", pathParts);

            // Only add files (not directories)
            if (!isDirectory)
            {
                paths.Add(NormalizePath(fullPath));
            }
        }

        _logger?.LogDebug("Extracted {Count} file paths from tree", paths.Count);
        return paths;
    }

    /// <inheritdoc/>
    public ParsedTreeNode? ParseAsciiTreeStructured(string treeContent)
    {
        if (string.IsNullOrWhiteSpace(treeContent))
            return null;

        var root = new ParsedTreeNode
        {
            Name = "",
            FullPath = "",
            IsDirectory = true,
            Depth = -1
        };

        var lines = treeContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var nodeStack = new Stack<ParsedTreeNode>();
        nodeStack.Push(root);

        foreach (var line in lines)
        {
            var parseResult = ParseTreeLine(line);
            if (parseResult == null)
                continue;

            var (name, depth, isDirectory, comment) = parseResult.Value;

            // Pop until we find the parent
            while (nodeStack.Count > 1 && nodeStack.Peek().Depth >= depth)
            {
                nodeStack.Pop();
            }

            var parent = nodeStack.Peek();

            // Build full path
            var fullPath = string.IsNullOrEmpty(parent.FullPath)
                ? name.TrimEnd('/')
                : $"{parent.FullPath}/{name.TrimEnd('/')}";

            var node = new ParsedTreeNode
            {
                Name = name.TrimEnd('/'),
                FullPath = fullPath,
                IsDirectory = isDirectory,
                Depth = depth,
                Parent = parent,
                Comment = comment,
                OriginalLine = line
            };

            parent.Children.Add(node);

            if (isDirectory)
            {
                nodeStack.Push(node);
            }
        }

        return root;
    }

    /// <inheritdoc/>
    public string? ExtractDescription(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var lowerContent = content.ToLowerInvariant();

        // Find the best matching indicator
        foreach (var indicator in _options.StructureIndicators)
        {
            var index = lowerContent.IndexOf(indicator.ToLowerInvariant());
            if (index < 0)
                continue;

            // Get the line containing the indicator
            var lineStart = content.LastIndexOf('\n', Math.Max(0, index - 1)) + 1;
            var lineEnd = content.IndexOf('\n', index);
            if (lineEnd < 0)
                lineEnd = content.Length;

            var line = content[lineStart..lineEnd].Trim();

            // Clean markdown formatting
            line = MarkdownFormattingPattern().Replace(line, "").Trim();

            // Validate length
            if (line.Length >= _options.MinDescriptionLength &&
                line.Length <= _options.MaxDescriptionLength)
            {
                _logger?.LogDebug("Extracted description: {Description}", line);
                return line;
            }

            // Try to truncate if too long
            if (line.Length > _options.MaxDescriptionLength)
            {
                var truncated = line[..(_options.MaxDescriptionLength - 3)] + "...";
                _logger?.LogDebug("Truncated description: {Description}", truncated);
                return truncated;
            }
        }

        _logger?.LogDebug("No description found in content");
        return null;
    }

    /// <inheritdoc/>
    public string FindCommonRoot(IEnumerable<string> paths)
    {
        var pathList = paths.Select(NormalizePath).ToList();

        if (pathList.Count == 0)
            return string.Empty;

        if (pathList.Count == 1)
        {
            var dir = Path.GetDirectoryName(pathList[0]);
            return dir?.Replace('\\', '/') ?? string.Empty;
        }

        // Split all paths into segments
        var segments = pathList
            .Select(p => p.Split('/'))
            .ToList();

        // Find minimum length (excluding the filename)
        var minLength = segments.Min(s => s.Length);
        var commonParts = new List<string>();

        // Compare each segment position
        for (int i = 0; i < minLength - 1; i++)
        {
            var segment = segments[0][i];
            var allMatch = segments.All(s =>
                s[i].Equals(segment, StringComparison.OrdinalIgnoreCase));

            if (allMatch)
            {
                commonParts.Add(segment);
            }
            else
            {
                break;
            }
        }

        var root = string.Join("/", commonParts);
        _logger?.LogDebug("Common root for {Count} paths: '{Root}'", pathList.Count, root);

        return root;
    }

    /// <inheritdoc/>
    public bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Check for absolute paths BEFORE normalization
        if (path.StartsWith("/") || path.StartsWith("\\"))
        {
            _logger?.LogDebug("Invalid path (absolute): {Path}", path);
            return false;
        }

        // Check for Windows absolute paths (e.g., C:\)
        if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':')
        {
            _logger?.LogDebug("Invalid path (absolute Windows): {Path}", path);
            return false;
        }

        // Normalize the path
        path = NormalizePath(path);

        // Check for dangerous patterns
        if (path.Contains(".."))
        {
            _logger?.LogDebug("Invalid path (contains ..): {Path}", path);
            return false;
        }

        // Check for valid characters
        if (!ValidPathPattern().IsMatch(path))
        {
            _logger?.LogDebug("Invalid path (invalid characters): {Path}", path);
            return false;
        }

        // Check path length (Windows limit)
        if (path.Length > 260)
        {
            _logger?.LogDebug("Invalid path (too long): {Path}", path);
            return false;
        }

        return true;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parse a single line from the tree structure.
    /// </summary>
    private (string Name, int Depth, bool IsDirectory, string? Comment)? ParseTreeLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        string? name = null;
        int depth = 0;
        string? comment = null;

        // Try standard Unicode tree format first
        var match = TreeLinePattern().Match(line);
        if (match.Success)
        {
            name = match.Groups[1].Value.Trim();
            depth = CalculateDepth(line);
        }

        // Try ASCII-only format
        if (name == null)
        {
            match = AsciiTreeLinePattern().Match(line);
            if (match.Success)
            {
                name = match.Groups[1].Value.Trim();
                depth = CalculateDepth(line);
            }
        }

        // Try simple listing format
        if (name == null && _options.EnableSimpleListing)
        {
            match = SimpleListingPattern().Match(line);
            if (match.Success)
            {
                name = match.Groups[2].Value.Trim();
                depth = match.Groups[1].Value.Length / 2; // 2 spaces per level
            }
        }

        // Try root directory pattern
        if (name == null)
        {
            match = RootDirectoryPattern().Match(line.Trim());
            if (match.Success)
            {
                name = match.Groups[1].Value + "/";
                depth = 0;
            }
        }

        if (name == null)
            return null;

        // Extract comment if present
        if (_options.TrimComments)
        {
            var commentMatch = InlineCommentPattern().Match(name);
            if (commentMatch.Success)
            {
                comment = commentMatch.Groups[1].Value.Trim();
                name = name[..commentMatch.Index].Trim();
            }
        }

        var isDirectory = name.EndsWith('/') ||
                         name.EndsWith('\\') ||
                         !name.Contains('.');

        return (name, depth, isDirectory, comment);
    }

    /// <summary>
    /// Calculate the depth/indentation level of a tree line.
    /// </summary>
    private static int CalculateDepth(string line)
    {
        int depth = 0;
        int spaceCount = 0;

        foreach (var c in line)
        {
            switch (c)
            {
                case ' ':
                    spaceCount++;
                    if (spaceCount == 4)
                    {
                        depth++;
                        spaceCount = 0;
                    }
                    break;

                case '\t':
                    depth++;
                    spaceCount = 0;
                    break;

                case '│':
                case '|':
                    depth++;
                    spaceCount = 0;
                    break;

                case '├':
                case '└':
                case '+':
                case '`':
                    // Stop counting at the branch marker
                    return depth;

                default:
                    if (!char.IsWhiteSpace(c))
                        return depth;
                    break;
            }
        }

        return depth;
    }

    /// <summary>
    /// Normalize a path to use forward slashes and clean up.
    /// </summary>
    private static string NormalizePath(string path)
    {
        return path
            .Replace('\\', '/')
            .TrimStart('/')
            .TrimEnd('/');
    }
}
