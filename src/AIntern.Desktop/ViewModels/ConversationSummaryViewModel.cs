using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel representing a single conversation in the conversation list.
/// </summary>
public sealed partial class ConversationSummaryViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private DateTime _updatedAt;

    [ObservableProperty]
    private int _messageCount;

    [ObservableProperty]
    private string? _preview;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets a human-readable relative time string (e.g., "5 minutes ago", "Yesterday").
    /// </summary>
    public string RelativeTime => GetRelativeTimeString(UpdatedAt);

    public ConversationSummaryViewModel()
    {
    }

    public ConversationSummaryViewModel(ConversationSummary summary)
    {
        Id = summary.Id;
        Title = summary.Title;
        UpdatedAt = summary.UpdatedAt;
        MessageCount = summary.MessageCount;
        Preview = summary.FirstMessagePreview;
    }

    public void Update(ConversationSummary summary)
    {
        Title = summary.Title;
        UpdatedAt = summary.UpdatedAt;
        MessageCount = summary.MessageCount;
        Preview = summary.FirstMessagePreview;
        OnPropertyChanged(nameof(RelativeTime));
    }

    private static string GetRelativeTimeString(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 2)
            return "Yesterday";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays} days ago";
        if (diff.TotalDays < 30)
            return $"{(int)(diff.TotalDays / 7)} weeks ago";

        return dateTime.ToLocalTime().ToString("MMM d, yyyy");
    }
}
