namespace AIntern.Desktop.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;

/// <summary>
/// Represents a single recent workspace item.
/// </summary>
/// <remarks>Added in v0.3.5e.</remarks>
public partial class RecentWorkspaceItemViewModel : ViewModelBase
{
    /// <summary>
    /// Workspace ID.
    /// </summary>
    [ObservableProperty]
    private Guid _id;

    /// <summary>
    /// Display name.
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Root directory path.
    /// </summary>
    [ObservableProperty]
    private string _rootPath = string.Empty;

    /// <summary>
    /// Last accessed timestamp.
    /// </summary>
    [ObservableProperty]
    private DateTime _lastAccessedAt;

    /// <summary>
    /// Whether workspace is pinned.
    /// </summary>
    [ObservableProperty]
    private bool _isPinned;

    /// <summary>
    /// Path shortened with ~ for home directory.
    /// </summary>
    public string DisplayPath => ShortenPath(RootPath);

    /// <summary>
    /// Relative time string.
    /// </summary>
    public string TimeAgo => FormatTimeAgo(LastAccessedAt);

    /// <summary>
    /// Pin button tooltip.
    /// </summary>
    public string PinTooltip => IsPinned ? "Unpin" : "Pin to top";

    /// <summary>
    /// Creates a new RecentWorkspaceItemViewModel from a workspace.
    /// </summary>
    public RecentWorkspaceItemViewModel(Workspace workspace)
    {
        Id = workspace.Id;
        Name = workspace.DisplayName;
        RootPath = workspace.RootPath;
        LastAccessedAt = workspace.LastAccessedAt;
        IsPinned = workspace.IsPinned;
    }

    /// <summary>
    /// Creates empty RecentWorkspaceItemViewModel for testing.
    /// </summary>
    public RecentWorkspaceItemViewModel() { }

    /// <summary>
    /// Shortens path by replacing home directory with ~.
    /// </summary>
    public static string ShortenPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(home) && path.StartsWith(home, StringComparison.OrdinalIgnoreCase))
            return "~" + path[home.Length..];
        return path;
    }

    /// <summary>
    /// Formats time as a relative string.
    /// </summary>
    public static string FormatTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;

        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)}w ago";

        return dateTime.ToString("MMM d");
    }
}
