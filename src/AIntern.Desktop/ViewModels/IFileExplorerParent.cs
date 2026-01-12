namespace AIntern.Desktop.ViewModels;

/// <summary>
/// Interface for parent ViewModel that manages file tree items.
/// Enables testing FileTreeItemViewModel independently.
/// </summary>
public interface IFileExplorerParent
{
    /// <summary>
    /// Loads children for a directory item.
    /// </summary>
    Task<IReadOnlyList<FileTreeItemViewModel>> LoadChildrenForItemAsync(FileTreeItemViewModel item);

    /// <summary>
    /// Renames a file or folder.
    /// </summary>
    Task RenameItemAsync(FileTreeItemViewModel item, string newName);

    /// <summary>
    /// Called when an item's expansion state changes.
    /// </summary>
    void OnItemExpansionChanged(FileTreeItemViewModel item);

    /// <summary>
    /// Gets the relative path from the workspace root.
    /// </summary>
    string GetRelativePath(string absolutePath);

    /// <summary>
    /// Shows an error message to the user.
    /// </summary>
    void ShowError(string message);
}
