namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PARSED TREE NODE (v0.4.4b)                                               │
// │ A node in a parsed tree structure.                                       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// A node in a parsed tree structure.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4b.</para>
/// </remarks>
public sealed class ParsedTreeNode
{
    /// <summary>
    /// Name of the file or directory.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Full path from root.
    /// </summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>
    /// Whether this is a directory (ends with /).
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// Nesting depth (0 = root).
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// Child nodes (for directories).
    /// </summary>
    public List<ParsedTreeNode> Children { get; init; } = new();

    /// <summary>
    /// Parent node reference.
    /// </summary>
    public ParsedTreeNode? Parent { get; set; }

    /// <summary>
    /// Inline comment if present (e.g., "# main entry point").
    /// </summary>
    public string? Comment { get; init; }

    /// <summary>
    /// The original line from the tree.
    /// </summary>
    public string? OriginalLine { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// File extension (empty for directories).
    /// </summary>
    public string Extension => IsDirectory
        ? string.Empty
        : Path.GetExtension(Name);

    /// <summary>
    /// Whether this node has children.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Whether this is a root node (no parent).
    /// </summary>
    public bool IsRoot => Parent == null;

    /// <summary>
    /// Number of child nodes.
    /// </summary>
    public int ChildCount => Children.Count;

    // ═══════════════════════════════════════════════════════════════════════
    // Query Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get all descendant file paths.
    /// </summary>
    public IEnumerable<string> GetAllFilePaths()
    {
        if (!IsDirectory)
        {
            yield return FullPath;
        }

        foreach (var child in Children)
        {
            foreach (var path in child.GetAllFilePaths())
            {
                yield return path;
            }
        }
    }

    /// <summary>
    /// Get all descendant directory paths.
    /// </summary>
    public IEnumerable<string> GetAllDirectoryPaths()
    {
        if (IsDirectory && !string.IsNullOrEmpty(FullPath))
        {
            yield return FullPath;
        }

        foreach (var child in Children)
        {
            foreach (var path in child.GetAllDirectoryPaths())
            {
                yield return path;
            }
        }
    }

    /// <summary>
    /// Get all descendant nodes (depth-first).
    /// </summary>
    public IEnumerable<ParsedTreeNode> GetAllDescendants()
    {
        foreach (var child in Children)
        {
            yield return child;

            foreach (var descendant in child.GetAllDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Find a descendant by path.
    /// </summary>
    public ParsedTreeNode? FindByPath(string path)
    {
        if (FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))
        {
            return this;
        }

        foreach (var child in Children)
        {
            var found = child.FindByPath(path);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
