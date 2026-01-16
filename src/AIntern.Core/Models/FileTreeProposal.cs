using System.Text;

namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PROPOSAL (v0.4.4a)                                             │
// │ Represents a proposal to create multiple files and directories.         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a proposal to create multiple files and directories.
/// This is the root model for multi-file creation proposals extracted
/// from LLM responses containing multiple code blocks with file paths.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4a.</para>
/// </remarks>
public sealed class FileTreeProposal
{
    // ═══════════════════════════════════════════════════════════════════════
    // Identity Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Unique identifier for this proposal.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// ID of the ChatMessage that generated this proposal.
    /// Used to link back to the source message for context.
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// When this proposal was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════════════
    // Structure Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Root directory for the proposal (relative to workspace).
    /// Computed as the common path prefix of all operations.
    /// May be empty if files span multiple root directories.
    /// </summary>
    /// <example>
    /// For operations: ["src/Models/User.cs", "src/Services/UserService.cs"]
    /// RootPath would be: "src"
    /// </example>
    public string RootPath { get; init; } = string.Empty;

    /// <summary>
    /// All proposed file operations in dependency order.
    /// Order is determined by the Order property of each FileOperation.
    /// </summary>
    public IReadOnlyList<FileOperation> Operations { get; init; } = Array.Empty<FileOperation>();

    /// <summary>
    /// Summary description extracted from the LLM response.
    /// Typically the text immediately preceding the file tree or code blocks.
    /// </summary>
    /// <example>
    /// "Here's a complete user authentication module with the following structure:"
    /// </example>
    public string? Description { get; set; }

    /// <summary>
    /// The raw file tree text if detected (e.g., ASCII tree representation).
    /// Preserved for display purposes and potential re-parsing.
    /// </summary>
    /// <example>
    /// src/
    /// ├── Models/
    /// │   └── User.cs
    /// └── Services/
    ///     └── UserService.cs
    /// </example>
    public string? RawTreeText { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Status Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Status of the overall proposal.
    /// Tracks whether operations have been applied, rejected, etc.
    /// </summary>
    public FileTreeProposalStatus Status { get; set; } = FileTreeProposalStatus.Pending;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties - Counts
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Total files to create (operations with Type == Create).
    /// </summary>
    public int FileCount => Operations.Count(o => o.Type == FileOperationType.Create);

    /// <summary>
    /// Total files to modify (operations with Type == Modify).
    /// </summary>
    public int ModifyCount => Operations.Count(o => o.Type == FileOperationType.Modify);

    /// <summary>
    /// Total files to delete (operations with Type == Delete).
    /// </summary>
    public int DeleteCount => Operations.Count(o => o.Type == FileOperationType.Delete);

    /// <summary>
    /// Total rename operations.
    /// </summary>
    public int RenameCount => Operations.Count(o => o.Type == FileOperationType.Rename);

    /// <summary>
    /// Unique directories that will be created.
    /// Extracted from Create operation paths.
    /// </summary>
    public IEnumerable<string> Directories => Operations
        .Where(o => o.Type == FileOperationType.Create)
        .Select(o => Path.GetDirectoryName(o.Path))
        .Where(d => !string.IsNullOrEmpty(d))
        .Distinct()!;

    /// <summary>
    /// Total unique directories to create.
    /// </summary>
    public int DirectoryCount => Directories.Count();

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties - Selection
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Operations currently selected for apply.
    /// Users can toggle selection in the UI.
    /// </summary>
    public IEnumerable<FileOperation> SelectedOperations =>
        Operations.Where(o => o.IsSelected);

    /// <summary>
    /// Count of selected operations.
    /// </summary>
    public int SelectedCount => SelectedOperations.Count();

    /// <summary>
    /// Whether any operations are selected for apply.
    /// </summary>
    public bool HasSelectedOperations => SelectedCount > 0;

    /// <summary>
    /// Whether all operations are selected.
    /// </summary>
    public bool AllSelected => SelectedCount == Operations.Count;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties - Size
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Total estimated size of all files to create (in bytes).
    /// Calculated as UTF-8 byte count of all operation content.
    /// </summary>
    public long TotalSizeBytes => Operations
        .Where(o => o.Content != null)
        .Sum(o => Encoding.UTF8.GetByteCount(o.Content!));

    /// <summary>
    /// Human-readable total size (e.g., "12.5 KB").
    /// </summary>
    public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);

    /// <summary>
    /// Total line count across all operations.
    /// </summary>
    public int TotalLineCount => Operations
        .Where(o => o.Content != null)
        .Sum(o => o.LineCount);

    // ═══════════════════════════════════════════════════════════════════════
    // Query Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Check if a specific path is included in this proposal.
    /// </summary>
    /// <param name="path">The path to check (case-insensitive).</param>
    /// <returns>True if an operation exists for this path.</returns>
    public bool ContainsPath(string path) =>
        Operations.Any(o => o.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Get operation by path.
    /// </summary>
    /// <param name="path">The path to find (case-insensitive).</param>
    /// <returns>The operation, or null if not found.</returns>
    public FileOperation? GetOperation(string path) =>
        Operations.FirstOrDefault(o => o.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Get operation by ID.
    /// </summary>
    /// <param name="id">The operation ID.</param>
    /// <returns>The operation, or null if not found.</returns>
    public FileOperation? GetOperationById(Guid id) =>
        Operations.FirstOrDefault(o => o.Id == id);

    /// <summary>
    /// Get all operations for a specific directory.
    /// </summary>
    /// <param name="directory">The directory path.</param>
    /// <returns>Operations within that directory.</returns>
    public IEnumerable<FileOperation> GetOperationsInDirectory(string directory) =>
        Operations.Where(o =>
            o.Directory?.Equals(directory, StringComparison.OrdinalIgnoreCase) == true);

    /// <summary>
    /// Get operations by type.
    /// </summary>
    /// <param name="type">The operation type to filter by.</param>
    /// <returns>Operations of the specified type.</returns>
    public IEnumerable<FileOperation> GetOperationsByType(FileOperationType type) =>
        Operations.Where(o => o.Type == type);

    /// <summary>
    /// Get operations that have not been applied yet.
    /// </summary>
    public IEnumerable<FileOperation> PendingOperations =>
        Operations.Where(o => o.Status == FileOperationStatus.Pending);

    /// <summary>
    /// Get operations that have been applied.
    /// </summary>
    public IEnumerable<FileOperation> AppliedOperations =>
        Operations.Where(o => o.Status == FileOperationStatus.Applied);

    /// <summary>
    /// Get operations that failed.
    /// </summary>
    public IEnumerable<FileOperation> FailedOperations =>
        Operations.Where(o => o.Status == FileOperationStatus.Failed);

    // ═══════════════════════════════════════════════════════════════════════
    // Selection Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Select all operations.
    /// </summary>
    public void SelectAll()
    {
        foreach (var op in Operations)
        {
            op.IsSelected = true;
        }
    }

    /// <summary>
    /// Deselect all operations.
    /// </summary>
    public void DeselectAll()
    {
        foreach (var op in Operations)
        {
            op.IsSelected = false;
        }
    }

    /// <summary>
    /// Toggle selection for a specific operation.
    /// </summary>
    /// <param name="operationId">The operation ID to toggle.</param>
    public void ToggleSelection(Guid operationId)
    {
        var op = GetOperationById(operationId);
        if (op != null)
        {
            op.IsSelected = !op.IsSelected;
        }
    }

    /// <summary>
    /// Select operations by type.
    /// </summary>
    /// <param name="type">The operation type to select.</param>
    public void SelectByType(FileOperationType type)
    {
        foreach (var op in Operations.Where(o => o.Type == type))
        {
            op.IsSelected = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

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
