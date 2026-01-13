namespace AIntern.Desktop.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for a single conversation item in the sidebar list.
/// </summary>
/// <remarks>
/// <para>
/// Represents a lightweight view of a conversation for list display.
/// Contains display properties, selection state, and inline rename functionality.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item>Identity: <see cref="Id"/> - Unique conversation identifier</item>
/// <item>Display: <see cref="Title"/>, <see cref="Preview"/>, <see cref="MessageCount"/></item>
/// <item>State: <see cref="IsSelected"/>, <see cref="IsPinned"/></item>
/// <item>Rename: <see cref="IsRenaming"/>, <see cref="EditingTitle"/></item>
/// <item>Computed: <see cref="RelativeTime"/>, <see cref="MessageCountText"/></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var summary = new ConversationSummaryViewModel
/// {
///     Id = conversation.Id,
///     Title = conversation.Title,
///     UpdatedAt = conversation.UpdatedAt,
///     MessageCount = conversation.MessageCount
/// };
/// 
/// // Bind in XAML:
/// // Text="{Binding Title}"
/// // Text="{Binding RelativeTime}"
/// </code>
/// </example>
public partial class ConversationSummaryViewModel : ViewModelBase
{
    #region Identity

    /// <summary>
    /// Gets the unique identifier for this conversation.
    /// </summary>
    /// <remarks>
    /// Immutable after creation. Used to identify the conversation
    /// in all service operations (load, delete, rename, etc.).
    /// </remarks>
    public Guid Id { get; init; }

    #endregion

    #region Display Properties

    /// <summary>
    /// Gets or sets the conversation title.
    /// </summary>
    /// <remarks>
    /// Auto-generated from first user message if not explicitly set.
    /// Maximum 200 characters (matches database constraint).
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string _title = string.Empty;

    /// <summary>
    /// Gets or sets when the conversation was last updated.
    /// </summary>
    /// <remarks>
    /// Updated whenever a message is added, edited, or removed.
    /// Used for sorting and <see cref="RelativeTime"/> calculation.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RelativeTime))]
    private DateTime _updatedAt;

    /// <summary>
    /// Gets or sets the total message count.
    /// </summary>
    /// <remarks>
    /// Denormalized from database for display efficiency.
    /// Includes all message roles (System, User, Assistant).
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MessageCountText))]
    private int _messageCount;

    /// <summary>
    /// Gets or sets preview text (typically first user message).
    /// </summary>
    /// <remarks>
    /// Truncated to approximately 100 characters for display.
    /// May be null for empty conversations.
    /// </remarks>
    [ObservableProperty]
    private string? _preview;

    /// <summary>
    /// Gets or sets the model name used for this conversation.
    /// </summary>
    /// <remarks>
    /// Extracted from model path for display (e.g., "llama-3.2-8b").
    /// May be null if no model was ever selected.
    /// </remarks>
    [ObservableProperty]
    private string? _modelName;

    #endregion

    #region State Properties

    /// <summary>
    /// Gets or sets whether this conversation is currently selected.
    /// </summary>
    /// <remarks>
    /// Only one conversation should be selected at a time.
    /// Selection is managed by <see cref="ConversationListViewModel"/>.
    /// </remarks>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets or sets whether this conversation is pinned.
    /// </summary>
    /// <remarks>
    /// Pinned conversations appear at the top of their date group.
    /// Toggle via context menu or keyboard shortcut.
    /// </remarks>
    [ObservableProperty]
    private bool _isPinned;

    #endregion

    #region Rename State

    /// <summary>
    /// Gets or sets whether the user is currently renaming this conversation.
    /// </summary>
    /// <remarks>
    /// When true, the UI shows a TextBox instead of TextBlock for the title.
    /// Set by <see cref="ConversationListViewModel.RenameConversationCommand"/>.
    /// </remarks>
    [ObservableProperty]
    private bool _isRenaming;

    /// <summary>
    /// Gets or sets the title being edited during rename.
    /// </summary>
    /// <remarks>
    /// Initialized to current <see cref="Title"/> when rename begins.
    /// Applied on confirm, discarded on cancel.
    /// </remarks>
    [ObservableProperty]
    private string _editingTitle = string.Empty;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the title to display, truncating if necessary.
    /// </summary>
    /// <remarks>
    /// Returns the full title, limited to 50 characters for UI display.
    /// Ellipsis added if truncated.
    /// </remarks>
    public string DisplayTitle => Title.Length > 50
        ? Title[..47] + "..."
        : Title;

    /// <summary>
    /// Gets a human-readable relative time string.
    /// </summary>
    /// <remarks>
    /// Formats relative to current time:
    /// <list type="bullet">
    /// <item>"Just now" - Less than 1 minute</item>
    /// <item>"5m ago" - Minutes (1-59)</item>
    /// <item>"2h ago" - Hours (1-23)</item>
    /// <item>"3d ago" - Days (1-6)</item>
    /// <item>"2w ago" - Weeks (1-3)</item>
    /// <item>"Jan 15" - Older than 30 days</item>
    /// </list>
    /// </remarks>
    public string RelativeTime => GetRelativeTime(UpdatedAt);

    /// <summary>
    /// Gets a human-readable message count string.
    /// </summary>
    /// <remarks>
    /// Pluralized appropriately:
    /// <list type="bullet">
    /// <item>"No messages" - Zero count</item>
    /// <item>"1 message" - Singular</item>
    /// <item>"12 messages" - Plural</item>
    /// </list>
    /// </remarks>
    public string MessageCountText => MessageCount switch
    {
        0 => "No messages",
        1 => "1 message",
        _ => $"{MessageCount:N0} messages"
    };

    #endregion

    #region Helper Methods

    /// <summary>
    /// Formats a DateTime as a human-readable relative time string.
    /// </summary>
    /// <param name="dateTime">The date/time to format (UTC).</param>
    /// <returns>A relative time string like "5m ago" or "Jan 15".</returns>
    private static string GetRelativeTime(DateTime dateTime)
    {
        // Ensure we're comparing UTC times
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        // Format based on time difference
        if (diff.TotalMinutes < 1)
            return "Just now";

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";

        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";

        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";

        if (diff.TotalDays < 30)
            return $"{(int)(diff.TotalDays / 7)}w ago";

        // For older dates, show abbreviated date
        return dateTime.ToString("MMM d");
    }

    #endregion

    #region Methods

    /// <summary>
    /// Begins the inline rename operation.
    /// </summary>
    /// <remarks>
    /// Called by <see cref="ConversationListViewModel"/> when user
    /// initiates rename via context menu or F2 key.
    /// </remarks>
    public void BeginRename()
    {
        EditingTitle = Title;
        IsRenaming = true;
    }

    /// <summary>
    /// Cancels the inline rename operation, discarding changes.
    /// </summary>
    public void CancelRename()
    {
        IsRenaming = false;
        EditingTitle = Title;
    }

    #endregion
}
