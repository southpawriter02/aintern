using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Models;
using AIntern.Core.Utilities;
using AIntern.Desktop.Utilities;
using System.Collections.ObjectModel;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for a file or folder in the file explorer tree.
/// Supports lazy loading, inline rename, filtering, and icon resolution.
/// </summary>
public partial class FileTreeItemViewModel : ViewModelBase
{
    private readonly IFileExplorerParent _parent;
    private bool _childrenLoaded;

    #region Observable Properties

    /// <summary>Display name of the file or folder.</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Absolute path to the item.</summary>
    [ObservableProperty]
    private string _path = string.Empty;

    /// <summary>Type of item (file, directory, symbolic link).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirectory))]
    [NotifyPropertyChangedFor(nameof(IsFile))]
    [NotifyPropertyChangedFor(nameof(ShowExpander))]
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

    /// <summary>Child items (for directories).</summary>
    public ObservableCollection<FileTreeItemViewModel> Children { get; } = [];

    /// <summary>Whether this directory has children (for expansion indicator).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowExpander))]
    private bool _hasChildren;

    /// <summary>Depth level in the tree (for indentation).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IndentMargin))]
    private int _depth;

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

    /// <summary>Indentation margin for tree hierarchy.</summary>
    public double IndentMargin => Depth * 16; // 16px per level

    /// <summary>Relative path from workspace root (for display/tooltips).</summary>
    public string RelativePath => _parent.GetRelativePath(Path);

    #endregion

    #region Constructor

    public FileTreeItemViewModel(IFileExplorerParent parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    /// <summary>Creates a FileTreeItemViewModel from a FileSystemItem.</summary>
    public static FileTreeItemViewModel FromFileSystemItem(
        FileSystemItem item,
        IFileExplorerParent parent,
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

    /// <summary>Called when IsExpanded changes.</summary>
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

    /// <summary>Loads children for this directory.</summary>
    public async Task LoadChildrenAsync()
    {
        if (_childrenLoaded || !IsDirectory)
            return;

        IsLoading = true;
        try
        {
            var children = await _parent.LoadChildrenForItemAsync(this);

            Children.Clear();
            foreach (var child in children)
            {
                Children.Add(child);
            }

            _childrenLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Forces a reload of children on next expansion.</summary>
    public void InvalidateChildren()
    {
        _childrenLoaded = false;
        Children.Clear();
    }

    /// <summary>Forces an immediate reload of children.</summary>
    public async Task RefreshChildrenAsync()
    {
        InvalidateChildren();

        if (IsExpanded)
        {
            await LoadChildrenAsync();
        }
    }

    /// <summary>Collapses this item and all descendants.</summary>
    public void CollapseAll()
    {
        IsExpanded = false;
        foreach (var child in Children)
        {
            child.CollapseAll();
        }
    }

    /// <summary>Expands this item and optionally all descendants.</summary>
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

    /// <summary>Starts inline rename mode.</summary>
    public void BeginRename()
    {
        EditingName = Name;
        IsRenaming = true;
    }

    /// <summary>Commits the rename operation.</summary>
    [RelayCommand]
    public async Task CommitRenameAsync()
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

        // Validation: same name
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
            Name = newName;
            Path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path) ?? "", newName);
            IsRenaming = false;
        }
        catch (Exception ex)
        {
            _parent.ShowError($"Rename failed: {ex.Message}");
            // Stay in rename mode for retry
        }
    }

    /// <summary>Cancels the rename operation.</summary>
    [RelayCommand]
    public void CancelRename()
    {
        IsRenaming = false;
        EditingName = Name;
    }

    #endregion

    #region Filtering

    /// <summary>Checks if this item matches the given filter.</summary>
    public bool MatchesFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        return Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Applies filter to this item and its children.
    /// Returns true if this item or any descendant matches.
    /// </summary>
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
            // Use base SetProperty to avoid triggering OnIsExpandedChanged
            SetProperty(ref _isExpanded, true, nameof(IsExpanded));
            OnPropertyChanged(nameof(IconKey));
        }

        return IsVisible;
    }

    /// <summary>Clears all filter state.</summary>
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
            // Special folder icons
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

        // File icon by extension
        return FileIconProvider.GetIconKeyForExtension(Extension);
    }

    #endregion

    #region Equality / Comparison

    public override bool Equals(object? obj)
    {
        return obj is FileTreeItemViewModel other &&
               string.Equals(Path, other.Path, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Path);
    }

    public override string ToString()
    {
        return $"{(IsDirectory ? "📁" : "📄")} {Name}";
    }

    #endregion
}
