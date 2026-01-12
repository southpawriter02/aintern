using System.Text;
using System.Text.RegularExpressions;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace AIntern.Services;

/// <summary>
/// Provides file system operations with workspace-aware features (v0.3.1d).
/// </summary>
public sealed class FileSystemService : IFileSystemService
{
    private readonly ILogger<FileSystemService> _logger;

    /// <summary>
    /// Binary file signatures for detection.
    /// </summary>
    private static readonly byte[][] BinarySignatures =
    [
        [0x89, 0x50, 0x4E, 0x47],       // PNG
        [0xFF, 0xD8, 0xFF],             // JPEG
        [0x47, 0x49, 0x46],             // GIF
        [0x25, 0x50, 0x44, 0x46],       // PDF
        [0x50, 0x4B, 0x03, 0x04],       // ZIP/DOCX/XLSX
        [0x7F, 0x45, 0x4C, 0x46],       // ELF
        [0x4D, 0x5A],                   // Windows EXE/DLL
        [0x52, 0x61, 0x72, 0x21],       // RAR
        [0x1F, 0x8B],                   // GZIP
        [0x42, 0x5A, 0x68],             // BZIP2
        [0x37, 0x7A, 0xBC, 0xAF],       // 7z
        [0x00, 0x00, 0x01, 0x00],       // ICO
        [0x00, 0x00, 0x00],             // Various video formats start with nulls
    ];

    /// <summary>
    /// Additional text extensions not covered by LanguageDetector.
    /// </summary>
    private static readonly HashSet<string> AdditionalTextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".log", ".csv", ".tsv", ".diff", ".patch", ".lock"
    };

    /// <summary>
    /// Default patterns to always ignore.
    /// </summary>
    private static readonly string[] DefaultIgnorePatterns =
    [
        ".git/",
        ".vs/",
        ".idea/",
        ".vscode/",
        "node_modules/",
        "bin/",
        "obj/",
        "packages/",
        "*.user",
        "*.suo",
        ".DS_Store",
        "Thumbs.db",
        "*.swp",
        "*.swo",
        "*~"
    ];

    public FileSystemService(ILogger<FileSystemService> logger)
    {
        _logger = logger;
    }

    #region Directory Operations

    public async Task<IReadOnlyList<FileSystemItem>> GetDirectoryContentsAsync(
        string path,
        bool includeHidden = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        return await Task.Run(() =>
        {
            var items = new List<FileSystemItem>();
            var dirInfo = new DirectoryInfo(path);

            // Get directories
            foreach (var dir in dirInfo.EnumerateDirectories())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!includeHidden && IsHidden(dir)) continue;

                var hasChildren = false;
                try { hasChildren = dir.EnumerateFileSystemInfos().Any(); }
                catch { /* Access denied - assume no children */ }

                items.Add(FileSystemItem.FromDirectoryInfo(dir, hasChildren));
            }

            // Get files
            foreach (var file in dirInfo.EnumerateFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!includeHidden && IsHidden(file)) continue;
                items.Add(FileSystemItem.FromFileInfo(file));
            }

            // Sort: directories first, then alphabetically by name
            return items
                .OrderByDescending(i => i.IsDirectory)
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }, cancellationToken);
    }

    public Task<FileSystemItem> GetItemInfoAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Task.Run(() =>
        {
            if (Directory.Exists(path))
            {
                var dirInfo = new DirectoryInfo(path);
                var hasChildren = false;
                try { hasChildren = dirInfo.EnumerateFileSystemInfos().Any(); }
                catch { /* Access denied */ }
                return FileSystemItem.FromDirectoryInfo(dirInfo, hasChildren);
            }

            if (File.Exists(path))
            {
                return FileSystemItem.FromFileInfo(new FileInfo(path));
            }

            throw new FileNotFoundException($"Path not found: {path}");
        }, cancellationToken);
    }

    public Task<FileSystemItem> CreateDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Task.Run(() =>
        {
            var dirInfo = Directory.CreateDirectory(path);
            _logger.LogDebug("Created directory: {Path}", path);
            return FileSystemItem.FromDirectoryInfo(dirInfo, hasChildren: false);
        }, cancellationToken);
    }

    public Task DeleteDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Task.Run(() =>
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            Directory.Delete(path, recursive: true);
            _logger.LogDebug("Deleted directory: {Path}", path);
        }, cancellationToken);
    }

    #endregion

    #region File Operations

    public async Task<string> ReadFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}", path);

        return await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
    }

    public async Task<byte[]> ReadFileBytesAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}", path);

        return await File.ReadAllBytesAsync(path, cancellationToken);
    }

    public async Task WriteFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogDebug("Created directory for file: {Directory}", directory);
        }

        await File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken);
        _logger.LogDebug("Wrote file: {Path}", path);
    }

    public Task<FileSystemItem> CreateFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Task.Run(() =>
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create empty file
            using (File.Create(path)) { }

            _logger.LogDebug("Created file: {Path}", path);
            return FileSystemItem.FromFileInfo(new FileInfo(path));
        }, cancellationToken);
    }

    public Task DeleteFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Task.Run(() =>
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}", path);

            File.Delete(path);
            _logger.LogDebug("Deleted file: {Path}", path);
        }, cancellationToken);
    }

    public Task<FileSystemItem> RenameAsync(
        string path,
        string newName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        return Task.Run(() =>
        {
            var parentDir = Path.GetDirectoryName(path)
                ?? throw new ArgumentException("Cannot determine parent directory", nameof(path));
            var newPath = Path.Combine(parentDir, newName);

            if (Directory.Exists(path))
            {
                Directory.Move(path, newPath);
                _logger.LogDebug("Renamed directory: {OldPath} -> {NewPath}", path, newPath);
                var dirInfo = new DirectoryInfo(newPath);
                var hasChildren = false;
                try { hasChildren = dirInfo.EnumerateFileSystemInfos().Any(); }
                catch { /* Access denied */ }
                return FileSystemItem.FromDirectoryInfo(dirInfo, hasChildren);
            }

            if (File.Exists(path))
            {
                File.Move(path, newPath);
                _logger.LogDebug("Renamed file: {OldPath} -> {NewPath}", path, newPath);
                return FileSystemItem.FromFileInfo(new FileInfo(newPath));
            }

            throw new FileNotFoundException($"Path not found: {path}");
        }, cancellationToken);
    }

    public Task<FileSystemItem> CopyFileAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        return Task.Run(() =>
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"Source file not found: {sourcePath}", sourcePath);

            // Ensure destination directory exists
            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(sourcePath, destinationPath, overwrite);
            _logger.LogDebug("Copied file: {Source} -> {Dest}", sourcePath, destinationPath);
            return FileSystemItem.FromFileInfo(new FileInfo(destinationPath));
        }, cancellationToken);
    }

    public Task<FileSystemItem> MoveAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        return Task.Run(() =>
        {
            // Ensure destination directory exists
            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            if (Directory.Exists(sourcePath))
            {
                Directory.Move(sourcePath, destinationPath);
                _logger.LogDebug("Moved directory: {Source} -> {Dest}", sourcePath, destinationPath);
                var dirInfo = new DirectoryInfo(destinationPath);
                var hasChildren = false;
                try { hasChildren = dirInfo.EnumerateFileSystemInfos().Any(); }
                catch { /* Access denied */ }
                return FileSystemItem.FromDirectoryInfo(dirInfo, hasChildren);
            }

            if (File.Exists(sourcePath))
            {
                File.Move(sourcePath, destinationPath);
                _logger.LogDebug("Moved file: {Source} -> {Dest}", sourcePath, destinationPath);
                return FileSystemItem.FromFileInfo(new FileInfo(destinationPath));
            }

            throw new FileNotFoundException($"Source path not found: {sourcePath}");
        }, cancellationToken);
    }

    #endregion

    #region Existence Checks

    public Task<bool> FileExistsAsync(string path)
    {
        return Task.FromResult(File.Exists(path));
    }

    public Task<bool> DirectoryExistsAsync(string path)
    {
        return Task.FromResult(Directory.Exists(path));
    }

    #endregion

    #region File Watching

    public IDisposable WatchDirectory(
        string path,
        Action<FileSystemChangeEvent> onChange,
        bool includeSubdirectories = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(onChange);

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        var watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = NotifyFilters.FileName
                         | NotifyFilters.DirectoryName
                         | NotifyFilters.LastWrite
                         | NotifyFilters.Size
        };

        // Debounce timer (200ms window)
        var debounceTimer = new System.Timers.Timer(200) { AutoReset = false };
        var pendingEvents = new Dictionary<string, FileSystemChangeEvent>(StringComparer.OrdinalIgnoreCase);
        var lockObj = new object();

        debounceTimer.Elapsed += (s, e) =>
        {
            List<FileSystemChangeEvent> events;
            lock (lockObj)
            {
                events = pendingEvents.Values.ToList();
                pendingEvents.Clear();
            }

            foreach (var evt in events)
            {
                try
                {
                    onChange(evt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in file watcher callback for {Path}", evt.Path);
                }
            }
        };

        void QueueEvent(FileSystemChangeEvent evt)
        {
            lock (lockObj)
            {
                pendingEvents[evt.Path] = evt;
            }
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        watcher.Created += (s, e) => QueueEvent(new FileSystemChangeEvent
        {
            Path = e.FullPath,
            ChangeType = FileSystemChangeType.Created,
            IsDirectory = Directory.Exists(e.FullPath)
        });

        watcher.Deleted += (s, e) => QueueEvent(new FileSystemChangeEvent
        {
            Path = e.FullPath,
            ChangeType = FileSystemChangeType.Deleted,
            IsDirectory = false // Can't tell after deletion
        });

        watcher.Changed += (s, e) => QueueEvent(new FileSystemChangeEvent
        {
            Path = e.FullPath,
            ChangeType = FileSystemChangeType.Modified,
            IsDirectory = Directory.Exists(e.FullPath)
        });

        watcher.Renamed += (s, e) => QueueEvent(new FileSystemChangeEvent
        {
            Path = e.FullPath,
            OldPath = e.OldFullPath,
            ChangeType = FileSystemChangeType.Renamed,
            IsDirectory = Directory.Exists(e.FullPath)
        });

        watcher.Error += (s, e) =>
            _logger.LogError(e.GetException(), "File watcher error for {Path}", path);

        watcher.EnableRaisingEvents = true;
        _logger.LogDebug("Started watching directory: {Path}", path);

        return new FileWatcherDisposable(watcher, debounceTimer, () =>
            _logger.LogDebug("Stopped watching directory: {Path}", path));
    }

    private sealed class FileWatcherDisposable : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly System.Timers.Timer _timer;
        private readonly Action _onDispose;
        private bool _disposed;

        public FileWatcherDisposable(FileSystemWatcher watcher, System.Timers.Timer timer, Action onDispose)
        {
            _watcher = watcher;
            _timer = timer;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _watcher.EnableRaisingEvents = false;
            _timer.Stop();
            _watcher.Dispose();
            _timer.Dispose();
            _onDispose();
        }
    }

    #endregion

    #region Utilities

    public string GetRelativePath(string fullPath, string basePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);

        return Path.GetRelativePath(basePath, fullPath);
    }

    public bool IsTextFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        // Check extension first (fast path)
        var extension = Path.GetExtension(path);
        if (!string.IsNullOrEmpty(extension))
        {
            // Check LanguageDetector supported extensions
            if (LanguageDetector.IsExtensionSupported(extension))
                return true;

            // Check additional text extensions
            if (AdditionalTextExtensions.Contains(extension))
                return true;
        }

        // Check file name for special files
        var fileName = Path.GetFileName(path);
        if (LanguageDetector.DetectByFileName(fileName) is not null)
            return true;

        // Check file header for binary signatures
        try
        {
            using var stream = File.OpenRead(path);

            // Empty file is considered text
            if (stream.Length == 0)
                return true;

            var buffer = new byte[8];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);

            // Check binary signatures
            foreach (var signature in BinarySignatures)
            {
                if (bytesRead >= signature.Length)
                {
                    var match = true;
                    for (var i = 0; i < signature.Length; i++)
                    {
                        if (buffer[i] != signature[i])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match) return false;
                }
            }

            // Check for null bytes in first 8KB (common in binary files)
            stream.Position = 0;
            var checkSize = (int)Math.Min(8192, stream.Length);
            var checkBuffer = new byte[checkSize];
            bytesRead = stream.Read(checkBuffer, 0, checkSize);

            for (var i = 0; i < bytesRead; i++)
            {
                if (checkBuffer[i] == 0)
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if file is text: {Path}", path);
            return false;
        }
    }

    public long GetFileSize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}", path);

        return new FileInfo(path).Length;
    }

    public async Task<int> GetLineCountAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}", path);

        var lineCount = 0;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is not null)
        {
            lineCount++;
        }

        return lineCount;
    }

    #endregion

    #region Ignore Patterns

    public bool ShouldIgnore(string path, string basePath, IReadOnlyList<string> ignorePatterns)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(basePath))
            return false;

        if (ignorePatterns.Count == 0)
            return false;

        var relativePath = GetRelativePath(path, basePath);
        var isDirectory = Directory.Exists(path);

        // Normalize path separators to forward slash
        relativePath = relativePath.Replace('\\', '/');
        if (isDirectory && !relativePath.EndsWith('/'))
            relativePath += '/';

        var shouldIgnore = false;

        foreach (var pattern in ignorePatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern.StartsWith('#'))
                continue;

            var trimmed = pattern.Trim();
            var isNegation = trimmed.StartsWith('!');
            if (isNegation)
                trimmed = trimmed[1..];

            try
            {
                var regex = ConvertGitIgnorePatternToRegex(trimmed);
                if (regex.IsMatch(relativePath))
                {
                    shouldIgnore = !isNegation;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid gitignore pattern: {Pattern}", pattern);
            }
        }

        return shouldIgnore;
    }

    public async Task<IReadOnlyList<string>> LoadGitIgnorePatternsAsync(
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        var patterns = new List<string>();

        // Load root .gitignore
        var gitignorePath = Path.Combine(workspacePath, ".gitignore");
        if (File.Exists(gitignorePath))
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(gitignorePath, cancellationToken);
                patterns.AddRange(lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#')));
                _logger.LogDebug("Loaded {Count} patterns from .gitignore", lines.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading .gitignore: {Path}", gitignorePath);
            }
        }

        // Add default patterns
        patterns.AddRange(DefaultIgnorePatterns);

        return patterns.Distinct().ToList();
    }

    private static Regex ConvertGitIgnorePatternToRegex(string pattern)
    {
        var regexPattern = new StringBuilder();

        var isDirectoryOnly = pattern.EndsWith('/');
        if (isDirectoryOnly)
            pattern = pattern[..^1];

        var hasLeadingSlash = pattern.StartsWith('/');
        if (hasLeadingSlash)
            pattern = pattern[1..];

        // Check if pattern contains path separator (anchored pattern)
        var containsSlash = pattern.Contains('/');

        // Escape regex special characters except * and ?
        var escaped = Regex.Escape(pattern);

        // Convert gitignore wildcards to regex
        escaped = escaped
            .Replace("\\*\\*", "\u0001")  // Placeholder for **
            .Replace("\\*", "[^/]*")      // * matches any chars except /
            .Replace("\\?", "[^/]")       // ? matches single char except /
            .Replace("\u0001", ".*");     // ** matches anything including /

        if (hasLeadingSlash || containsSlash)
        {
            // Anchored to root
            regexPattern.Append('^').Append(escaped);
        }
        else
        {
            // Match anywhere in path
            regexPattern.Append("(^|/)").Append(escaped);
        }

        if (isDirectoryOnly)
        {
            regexPattern.Append("/$");
        }
        else
        {
            regexPattern.Append("(/|$)");
        }

        return new Regex(regexPattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    #endregion

    #region Helpers

    private static bool IsHidden(FileSystemInfo info)
    {
        // Check hidden attribute
        if (info.Attributes.HasFlag(FileAttributes.Hidden))
            return true;

        // Check if name starts with dot (Unix-style hidden)
        return info.Name.StartsWith('.');
    }

    #endregion
}
