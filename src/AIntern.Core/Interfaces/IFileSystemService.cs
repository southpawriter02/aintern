using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Provides file system operations with workspace-aware features.
/// </summary>
public interface IFileSystemService
{
    #region Directory Operations

    /// <summary>
    /// Gets the contents of a directory, sorted by type (folders first) then name.
    /// </summary>
    /// <param name="path">Absolute path to the directory.</param>
    /// <param name="includeHidden">Whether to include hidden files/directories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file system items.</returns>
    Task<IReadOnlyList<FileSystemItem>> GetDirectoryContentsAsync(
        string path,
        bool includeHidden = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific file or directory.
    /// </summary>
    /// <param name="path">Absolute path to the item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File system item information.</returns>
    Task<FileSystemItem> GetItemInfoAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new directory.
    /// </summary>
    /// <param name="path">Absolute path for the new directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created directory information.</returns>
    Task<FileSystemItem> CreateDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a directory and all its contents recursively.
    /// </summary>
    /// <param name="path">Absolute path to the directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default);

    #endregion

    #region File Operations

    /// <summary>
    /// Reads a file's content as text.
    /// </summary>
    /// <param name="path">Absolute path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File content as string.</returns>
    Task<string> ReadFileAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a file's content as bytes.
    /// </summary>
    /// <param name="path">Absolute path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File content as byte array.</returns>
    Task<byte[]> ReadFileBytesAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes text content to a file (creates directories if needed).
    /// </summary>
    /// <param name="path">Absolute path to the file.</param>
    /// <param name="content">Content to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new empty file.
    /// </summary>
    /// <param name="path">Absolute path for the new file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created file information.</returns>
    Task<FileSystemItem> CreateFileAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="path">Absolute path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteFileAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a file or directory.
    /// </summary>
    /// <param name="path">Current absolute path.</param>
    /// <param name="newName">New name (not full path).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Renamed item information.</returns>
    Task<FileSystemItem> RenameAsync(
        string path,
        string newName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file to a new location.
    /// </summary>
    /// <param name="sourcePath">Source file path.</param>
    /// <param name="destinationPath">Destination file path.</param>
    /// <param name="overwrite">Whether to overwrite if destination exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Copied file information.</returns>
    Task<FileSystemItem> CopyFileAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file or directory to a new location.
    /// </summary>
    /// <param name="sourcePath">Source path.</param>
    /// <param name="destinationPath">Destination path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Moved item information.</returns>
    Task<FileSystemItem> MoveAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default);

    #endregion

    #region Existence Checks

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    /// <param name="path">Absolute path to check.</param>
    /// <returns>True if the file exists.</returns>
    Task<bool> FileExistsAsync(string path);

    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    /// <param name="path">Absolute path to check.</param>
    /// <returns>True if the directory exists.</returns>
    Task<bool> DirectoryExistsAsync(string path);

    #endregion

    #region File Watching

    /// <summary>
    /// Starts watching a directory for changes with debounced callbacks.
    /// </summary>
    /// <param name="path">Absolute path to watch.</param>
    /// <param name="onChange">Callback invoked when changes occur.</param>
    /// <param name="includeSubdirectories">Whether to watch subdirectories.</param>
    /// <returns>Disposable to stop watching.</returns>
    IDisposable WatchDirectory(
        string path,
        Action<FileSystemChangeEvent> onChange,
        bool includeSubdirectories = true);

    #endregion

    #region Utilities

    /// <summary>
    /// Gets the relative path from a base path to a full path.
    /// </summary>
    /// <param name="fullPath">Full absolute path.</param>
    /// <param name="basePath">Base path to make relative to.</param>
    /// <returns>Relative path.</returns>
    string GetRelativePath(string fullPath, string basePath);

    /// <summary>
    /// Determines if a file is likely a text file (vs binary).
    /// </summary>
    /// <param name="path">Absolute path to the file.</param>
    /// <returns>True if likely a text file.</returns>
    bool IsTextFile(string path);

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    /// <param name="path">Absolute path to the file.</param>
    /// <returns>File size in bytes.</returns>
    long GetFileSize(string path);

    /// <summary>
    /// Gets the line count of a text file.
    /// </summary>
    /// <param name="path">Absolute path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of lines in the file.</returns>
    Task<int> GetLineCountAsync(string path, CancellationToken cancellationToken = default);

    #endregion

    #region Ignore Patterns

    /// <summary>
    /// Determines if a path should be ignored based on gitignore-style patterns.
    /// </summary>
    /// <param name="path">Absolute path to check.</param>
    /// <param name="basePath">Base path for relative matching.</param>
    /// <param name="ignorePatterns">List of gitignore patterns.</param>
    /// <returns>True if the path should be ignored.</returns>
    bool ShouldIgnore(string path, string basePath, IReadOnlyList<string> ignorePatterns);

    /// <summary>
    /// Loads .gitignore patterns from a workspace plus common defaults.
    /// </summary>
    /// <param name="workspacePath">Root path of the workspace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined list of ignore patterns.</returns>
    Task<IReadOnlyList<string>> LoadGitIgnorePatternsAsync(
        string workspacePath,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Event data for file system change notifications.
/// </summary>
public sealed class FileSystemChangeEvent
{
    /// <summary>
    /// Absolute path that changed.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Old path (for rename events only).
    /// </summary>
    public string? OldPath { get; init; }

    /// <summary>
    /// Type of change.
    /// </summary>
    public required FileSystemChangeType ChangeType { get; init; }

    /// <summary>
    /// When the change occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the path is a directory.
    /// </summary>
    public bool IsDirectory { get; init; }
}

/// <summary>
/// Type of file system change.
/// </summary>
public enum FileSystemChangeType
{
    /// <summary>File or directory was created.</summary>
    Created,

    /// <summary>File content was modified.</summary>
    Modified,

    /// <summary>File or directory was deleted.</summary>
    Deleted,

    /// <summary>File or directory was renamed.</summary>
    Renamed
}
