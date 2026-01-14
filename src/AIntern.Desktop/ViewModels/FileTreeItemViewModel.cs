namespace AIntern.Desktop.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Models;
using AIntern.Core.Utilities;
using AIntern.Desktop.Utilities;
using System.Collections.ObjectModel;
using System.Diagnostics;

/// <summary>
/// ViewModel for a file or folder in the file explorer tree.
/// Supports lazy loading, inline rename, filtering, and icon resolution.
/// </summary>
/// <remarks>Added in v0.3.2a.</remarks>
public partial class FileTreeItemViewModel : ViewModelBase
{
    private readonly IFileTreeItemParent _parent;
    private bool _childrenLoaded;

    #region Observable Properties

    /// <summary>Display name of the file or folder.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconKey))]
    private string _name = string.Empty;

    /// <summary>Absolute path to the item.</summary>
    [ObservableProperty]
    private string _path = string.Empty;

    /// <summary>Type of item (file, directory, symbolic link).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirectory))]
    [NotifyPropertyChangedFor(nameof(IsFile))]
    [NotifyPropertyChangedFor(nameof(ShowExpander))]
    [NotifyPropertyChangedFor(nameof(IconKey))]
    private FileSystemItemType _itemType;

    /// <summary>Whether this folder is expanded in the tree.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconKey))]
    private bool _isExpanded;

    /// <summary>Whether this item is currently selected.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Whether children are currently being loaded.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Whether the item is in inline rename mode.</summary>
    [ObservableProperty]
    private bool _isRenaming;

    /// <summary>The name being edited during rename.</summary>
    [ObservableProperty]
    private string _editingName = string.Empty;

    /// <summary>Whether the file has unsaved changes (editor integration).</summary>
    [ObservableProperty]
    private bool _isModified;

    /// <summary>Whether this item is visible after filtering.</summary>
    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>Whether to highlight this item (matches filter).</summary>
    [ObservableProperty]
    private bool _isHighlighted;

    /// <summary>Whether this directory has children (for expansion indicator).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowExpander))]
    private bool _hasChildren;

    /// <summary>Depth level in the tree (for indentation).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IndentMargin))]
    private int _depth;

    #endregion

    #region Collections

    /// <summary>Child items (for directories).</summary>
    public ObservableCollection<FileTreeItemViewModel> Children { get; } = [];

    #endregion

    #region Computed Properties

    /// <summary>Whether this is a directory.</summary>
    public bool IsDirectory => ItemType == FileSystemItemType.Directory;

    /// <summary>Whether this is a file.</summary>
    public bool IsFile => ItemType == FileSystemItemType.File;

    /// <summary>File extension including dot (e.g., ".cs").</summary>
    public string Extension => IsFile ? System.IO.Path.GetExtension(Path) : string.Empty;

    /// <summary>Detected programming language.</summary>
    public string? Language => IsFile ? LanguageDetector.DetectByFileName(Name) : null;

    /// <summary>Icon key based on item type, extension, and expansion state.</summary>
    public string IconKey => GetIconKey();

    /// <summary>Whether children have been loaded (for lazy loading).</summary>
    public bool ChildrenLoaded => _childrenLoaded;

    /// <summary>Whether to show the expansion arrow.</summary>
    public bool ShowExpander => IsDirectory && HasChildren;

    /// <summary>Indentation margin for tree hierarchy (16px per level).</summary>
    public double IndentMargin => Depth * 16;

    /// <summary>Relative path from workspace root (for display/tooltips).</summary>
    public string RelativePath => _parent.GetRelativePath(Path);

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new FileTreeItemViewModel.
    /// </summary>
    /// <param name="parent">Parent ViewModel for callbacks.</param>
    public FileTreeItemViewModel(IFileTreeItemParent parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    /// <summary>
    /// Creates a FileTreeItemViewModel from a FileSystemItem model.
    /// </summary>
    /// <param name="item">Source model.</param>
    /// <param name="parent">Parent ViewModel for callbacks.</param>
    /// <param name="depth">Tree depth (0 = root level).</param>
    /// <returns>Configured ViewModel instance.</returns>
    public static FileTreeItemViewModel FromFileSystemItem(
        FileSystemItem item,
        IFileTreeItemParent parent,
        int depth = 0)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(parent);

        return new FileTreeItemViewModel(parent)
        {
            Name = item.Name,
            Path = item.Path,
            ItemType = item.Type,
            HasChildren = item.HasChildren,
            Depth = depth,
            _childrenLoaded = false
        };
    }

    #endregion

    #region Expansion / Lazy Loading

    /// <summary>
    /// Called when IsExpanded changes. Triggers lazy loading if needed.
    /// </summary>
    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !_childrenLoaded && IsDirectory)
        {
            // Fire-and-forget load (UI will update via binding)
            _ = LoadChildrenAsync();
        }

        // Notify parent to persist expanded state
        _parent.OnItemExpansionChanged(this);
    }

    /// <summary>
    /// Loads children for this directory asynchronously.
    /// </summary>
    public async Task LoadChildrenAsync()
    {
        if (_childrenLoaded || !IsDirectory)
            return;

        IsLoading = true;
        var sw = Stopwatch.StartNew();

        try
        {
            var children = await _parent.LoadChildrenForItemAsync(this);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Children.Clear();
                foreach (var child in children)
                {
                    Children.Add(child);
                }
            });

            _childrenLoaded = true;
            Debug.WriteLine($"[FileTreeItem] Loaded {children.Count} children for {Name} in {sw.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FileTreeItem] Failed to load children for {Name}: {ex.Message}");
            _childrenLoaded = false;
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Invalidates cached children, forcing reload on next expansion.
    /// </summary>
    public void InvalidateChildren()
    {
        _childrenLoaded = false;
        Children.Clear();
    }

    /// <summary>
    /// Forces an immediate reload of children.
    /// </summary>
    public async Task RefreshChildrenAsync()
    {
        InvalidateChildren();

        if (IsExpanded)
        {
            await LoadChildrenAsync();
        }
    }

    /// <summary>
    /// Collapses this item and all descendants recursively.
    /// </summary>
    public void CollapseAll()
    {
        IsExpanded = false;
        foreach (var child in Children)
        {
            child.CollapseAll();
        }
    }

    /// <summary>
    /// Expands this item and optionally all descendants.
    /// </summary>
    /// <param name="recursive">Whether to expand descendant directories.</param>
    /// <param name="maxDepth">Maximum depth to expand (prevents infinite expansion).</param>
    public async Task ExpandAllAsync(bool recursive = true, int maxDepth = 3)
    {
        if (!IsDirectory || Depth >= maxDepth)
            return;

        IsExpanded = true;
        await LoadChildrenAsync();

        if (recursive)
        {
            foreach (var child in Children.Where(c => c.IsDirectory))
            {
                await child.ExpandAllAsync(recursive, maxDepth);
            }
        }
    }

    #endregion

    #region Inline Rename

    /// <summary>
    /// Starts inline rename mode.
    /// </summary>
    public void BeginRename()
    {
        EditingName = Name;
        IsRenaming = true;
    }

    /// <summary>
    /// Commits the rename operation after validation.
    /// </summary>
    [RelayCommand]
    private async Task CommitRenameAsync()
    {
        if (!IsRenaming)
            return;

        var newName = EditingName.Trim();

        // Validation: empty name
        if (string.IsNullOrEmpty(newName))
        {
            CancelRename();
            return;
        }

        // Validation: unchanged name
        if (newName == Name)
        {
            CancelRename();
            return;
        }

        // Validation: invalid characters
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        if (newName.IndexOfAny(invalidChars) >= 0)
        {
            _parent.ShowError("Name contains invalid characters");
            return; // Stay in rename mode for correction
        }

        try
        {
            await _parent.RenameItemAsync(this, newName);
            IsRenaming = false;
        }
        catch (Exception ex)
        {
            _parent.ShowError($"Rename failed: {ex.Message}");
            // Stay in rename mode for retry
        }
    }

    /// <summary>
    /// Cancels the rename operation and reverts to original name.
    /// </summary>
    [RelayCommand]
    private void CancelRename()
    {
        IsRenaming = false;
        EditingName = Name;
    }

    #endregion

    #region Filtering

    /// <summary>
    /// Checks if this item's name matches the given filter.
    /// </summary>
    /// <param name="filter">Filter string to match against.</param>
    /// <returns>True if name contains filter (case-insensitive).</returns>
    public bool MatchesFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        return Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Applies filter to this item and its children recursively.
    /// Sets visibility and highlight states, auto-expands matching parents.
    /// </summary>
    /// <param name="filter">Filter string to apply.</param>
    /// <returns>True if this item or any descendant matches.</returns>
    public bool ApplyFilter(string filter)
    {
        // Empty filter: show everything, no highlights
        if (string.IsNullOrWhiteSpace(filter))
        {
            IsVisible = true;
            IsHighlighted = false;

            foreach (var child in Children)
            {
                child.ApplyFilter(filter);
            }

            return true;
        }

        // Check self
        var selfMatches = MatchesFilter(filter);

        // Check children recursively
        var childMatches = false;
        foreach (var child in Children)
        {
            if (child.ApplyFilter(filter))
            {
                childMatches = true;
            }
        }

        // Update visibility and highlight
        IsVisible = selfMatches || childMatches;
        IsHighlighted = selfMatches;

        // Auto-expand if children match but we're collapsed
        if (childMatches && !IsExpanded && IsDirectory)
        {
            IsExpanded = true;
        }

        return IsVisible;
    }

    /// <summary>
    /// Clears all filter state, making all items visible and unhighlighted.
    /// </summary>
    public void ClearFilter()
    {
        IsVisible = true;
        IsHighlighted = false;

        foreach (var child in Children)
        {
            child.ClearFilter();
        }
    }

    #endregion

    #region Icon Resolution

    private string GetIconKey()
    {
        if (IsDirectory)
        {
            // Special folder icons based on name
            var lowerName = Name.ToLowerInvariant();
            return lowerName switch
            {
                "src" or "source" => IsExpanded ? "folder-src-open" : "folder-src",
                "test" or "tests" => IsExpanded ? "folder-test-open" : "folder-test",
                "docs" or "documentation" => IsExpanded ? "folder-docs-open" : "folder-docs",
                "assets" or "images" => IsExpanded ? "folder-images-open" : "folder-images",
                "node_modules" => "folder-node",
                ".git" => "folder-git",
                "bin" or "obj" => "folder-dist",
                _ => IsExpanded ? "folder-open" : "folder"
            };
        }

        // Check for special file names first
        var specialIcon = FileIconProvider.GetIconKeyForSpecialFile(Name);
        if (specialIcon != null)
            return specialIcon;

        // File icon by extension
        return FileIconProvider.GetIconKeyForExtension(Extension);
    }

    #endregion

    #region Equality / Comparison

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is FileTreeItemViewModel other &&
               string.Equals(Path, other.Path, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Path);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{(IsDirectory ? "📁" : "📄")} {Name}";
    }

    #endregion
}
