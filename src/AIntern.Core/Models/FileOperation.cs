using System.Text;

namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE OPERATION (v0.4.4a)                                                 │
// │ Represents a single file operation within a multi-file proposal.        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a single file operation within a multi-file proposal.
/// Each operation corresponds to one code block or detected file action.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public sealed class FileOperation
{
    // ═══════════════════════════════════════════════════════════════════════
    // Identity Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Unique identifier for this operation.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Relative path within the workspace.
    /// Always uses forward slashes for consistency.
    /// </summary>
    /// <example>"src/Models/User.cs"</example>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Type of file operation to perform.
    /// </summary>
    public FileOperationType Type { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Content Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Content for create/modify operations.
    /// Null for delete/rename operations.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// ID of the source CodeBlock (if applicable).
    /// Links back to v0.4.1 code extraction.
    /// </summary>
    public Guid? CodeBlockId { get; init; }

    /// <summary>
    /// Detected programming language identifier.
    /// </summary>
    /// <example>"csharp", "typescript", "python"</example>
    public string? Language { get; init; }

    /// <summary>
    /// Display-friendly language name for UI.
    /// </summary>
    /// <example>"C#", "TypeScript", "Python"</example>
    public string? DisplayLanguage { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // State Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether this operation is selected for batch apply.
    /// Default true - user can deselect individual operations.
    /// </summary>
    public bool IsSelected { get; set; } = true;

    /// <summary>
    /// Status of this individual operation.
    /// </summary>
    public FileOperationStatus Status { get; set; } = FileOperationStatus.Pending;

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    // ═══════════════════════════════════════════════════════════════════════
    // Rename/Move Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// New path for rename/move operations.
    /// Only used when Type is Rename or Move.
    /// </summary>
    public string? NewPath { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Ordering Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sequence order for dependency handling.
    /// Lower numbers are processed first.
    /// </summary>
    /// <remarks>
    /// Used to ensure directories are created before files,
    /// and dependencies are resolved in correct order.
    /// </remarks>
    public int Order { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties - Path Components
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// File name without directory path.
    /// </summary>
    /// <example>"User.cs" from "src/Models/User.cs"</example>
    public string FileName => System.IO.Path.GetFileName(Path);

    /// <summary>
    /// Directory containing this file.
    /// </summary>
    /// <example>"src/Models" from "src/Models/User.cs"</example>
    public string? Directory => System.IO.Path.GetDirectoryName(Path)?.Replace('\\', '/');

    /// <summary>
    /// File extension including dot.
    /// </summary>
    /// <example>".cs" from "src/Models/User.cs"</example>
    public string Extension => System.IO.Path.GetExtension(Path);

    /// <summary>
    /// File name without extension.
    /// </summary>
    /// <example>"User" from "src/Models/User.cs"</example>
    public string FileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);

    /// <summary>
    /// Depth of the path (number of directory levels).
    /// </summary>
    /// <example>2 for "src/Models/User.cs"</example>
    public int PathDepth => Path.Count(c => c == '/');

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties - Content Analysis
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Content size in bytes (UTF-8 encoded).
    /// </summary>
    public long ContentSizeBytes => Content != null
        ? Encoding.UTF8.GetByteCount(Content)
        : 0;

    /// <summary>
    /// Human-readable content size.
    /// </summary>
    public string ContentSizeFormatted => FormatBytes(ContentSizeBytes);

    /// <summary>
    /// Line count of content.
    /// </summary>
    public int LineCount => Content?.Split('\n').Length ?? 0;

    /// <summary>
    /// Whether the content is empty or whitespace only.
    /// </summary>
    public bool IsContentEmpty => string.IsNullOrWhiteSpace(Content);

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties - State
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether this operation can be applied.
    /// Must be pending and have a known operation type.
    /// </summary>
    public bool CanApply =>
        Status == FileOperationStatus.Pending &&
        Type != FileOperationType.Unknown;

    /// <summary>
    /// Whether this operation has been completed (success or failure).
    /// </summary>
    public bool IsCompleted =>
        Status is FileOperationStatus.Applied
        or FileOperationStatus.Failed
        or FileOperationStatus.Skipped;

    /// <summary>
    /// Whether this operation is currently being processed.
    /// </summary>
    public bool IsInProgress => Status == FileOperationStatus.InProgress;

    /// <summary>
    /// Whether this operation has a conflict.
    /// </summary>
    public bool HasConflict => Status == FileOperationStatus.Conflict;

    // ═══════════════════════════════════════════════════════════════════════
    // Display Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Operation type display text.
    /// </summary>
    public string OperationDisplayText => Type switch
    {
        FileOperationType.Create => "Create",
        FileOperationType.Modify => "Modify",
        FileOperationType.Delete => "Delete",
        FileOperationType.Rename => "Rename",
        FileOperationType.Move => "Move",
        FileOperationType.CreateDirectory => "Create Directory",
        _ => "Unknown"
    };

    /// <summary>
    /// Status display text.
    /// </summary>
    public string StatusDisplayText => Status switch
    {
        FileOperationStatus.Pending => "Pending",
        FileOperationStatus.Applied => "Applied",
        FileOperationStatus.Skipped => "Skipped",
        FileOperationStatus.Failed => "Failed",
        FileOperationStatus.Conflict => "Conflict",
        FileOperationStatus.InProgress => "In Progress",
        _ => "Unknown"
    };

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Create a FileOperation from a CodeBlock.
    /// </summary>
    /// <param name="block">The source code block.</param>
    /// <param name="order">The sequence order.</param>
    /// <returns>A new FileOperation.</returns>
    public static FileOperation FromCodeBlock(CodeBlock block, int order = 0) => new()
    {
        Path = NormalizePath(block.TargetFilePath ?? string.Empty),
        Type = FileOperationType.Create,
        Content = block.Content,
        CodeBlockId = block.Id,
        Language = block.Language,
        DisplayLanguage = block.DisplayLanguage,
        Order = order
    };

    /// <summary>
    /// Create a directory creation operation.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <param name="order">The sequence order.</param>
    /// <returns>A new FileOperation for directory creation.</returns>
    public static FileOperation CreateDirectory(string path, int order = 0) => new()
    {
        Path = NormalizePath(path),
        Type = FileOperationType.CreateDirectory,
        Order = order
    };

    /// <summary>
    /// Create a file deletion operation.
    /// </summary>
    /// <param name="path">The file path to delete.</param>
    /// <param name="order">The sequence order.</param>
    /// <returns>A new FileOperation for deletion.</returns>
    public static FileOperation Delete(string path, int order = 0) => new()
    {
        Path = NormalizePath(path),
        Type = FileOperationType.Delete,
        Order = order
    };

    /// <summary>
    /// Create a file rename operation.
    /// </summary>
    /// <param name="oldPath">The current file path.</param>
    /// <param name="newPath">The new file path.</param>
    /// <param name="order">The sequence order.</param>
    /// <returns>A new FileOperation for renaming.</returns>
    public static FileOperation Rename(string oldPath, string newPath, int order = 0) => new()
    {
        Path = NormalizePath(oldPath),
        NewPath = NormalizePath(newPath),
        Type = FileOperationType.Rename,
        Order = order
    };

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Normalize a path to use forward slashes.
    /// </summary>
    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');

    /// <summary>
    /// Format bytes as human-readable string.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
