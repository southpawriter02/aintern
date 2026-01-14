namespace AIntern.Desktop.ViewModels;

using AIntern.Core.Models;

/// <summary>
/// Interface for parent ViewModels that host file tree items.
/// Enables child items to trigger parent operations.
/// </summary>
/// <remarks>Added in v0.3.2a.</remarks>
public interface IFileTreeItemParent
{
    /// <summary>
    /// Loads children for the specified directory item.
    /// </summary>
    /// <param name="item">The directory item to load children for.</param>
    /// <returns>Collection of child items.</returns>
    Task<IReadOnlyList<FileTreeItemViewModel>> LoadChildrenForItemAsync(FileTreeItemViewModel item);

    /// <summary>
    /// Renames an item in the file system.
    /// </summary>
    /// <param name="item">The item to rename.</param>
    /// <param name="newName">The new name.</param>
    Task RenameItemAsync(FileTreeItemViewModel item, string newName);

    /// <summary>
    /// Called when an item's expansion state changes.
    /// </summary>
    /// <param name="item">The item that was expanded or collapsed.</param>
    void OnItemExpansionChanged(FileTreeItemViewModel item);

    /// <summary>
    /// Displays an error message to the user.
    /// </summary>
    /// <param name="message">The error message.</param>
    void ShowError(string message);

    /// <summary>
    /// Gets the relative path from workspace root.
    /// </summary>
    /// <param name="absolutePath">Absolute file path.</param>
    /// <returns>Relative path string.</returns>
    string GetRelativePath(string absolutePath);
}
