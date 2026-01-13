namespace AIntern.Desktop.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Enums;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

/// <summary>
/// ViewModel for a group of conversations organized by date.
/// </summary>
/// <remarks>
/// <para>
/// Represents a collapsible group in the conversation list sidebar.
/// Groups are organized by <see cref="DateGroup"/> (Today, Yesterday, etc.).
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><see cref="DateGroup"/> - The grouping category</item>
/// <item><see cref="Title"/> - Display title ("Today", "Yesterday", etc.)</item>
/// <item><see cref="IsExpanded"/> - Collapse/expand state</item>
/// <item><see cref="Conversations"/> - Child items</item>
/// <item><see cref="Count"/> - Number of conversations (computed)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var group = new ConversationGroupViewModel
/// {
///     DateGroup = DateGroup.Today,
///     Title = "Today"
/// };
/// group.Conversations.Add(summaryVm);
/// </code>
/// </example>
public partial class ConversationGroupViewModel : ViewModelBase
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationGroupViewModel"/> class.
    /// </summary>
    public ConversationGroupViewModel()
    {
        // Subscribe to collection changes to update Count
        Conversations.CollectionChanged += OnConversationsChanged;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the date group category for this group.
    /// </summary>
    /// <remarks>
    /// Determines the sort order of groups in the list.
    /// See <see cref="AIntern.Core.Enums.DateGroup"/> for values.
    /// </remarks>
    public DateGroup DateGroup { get; init; }

    /// <summary>
    /// Gets or sets the display title for this group.
    /// </summary>
    /// <remarks>
    /// Localized display string. Standard values:
    /// <list type="bullet">
    /// <item>"Today"</item>
    /// <item>"Yesterday"</item>
    /// <item>"Previous 7 Days"</item>
    /// <item>"Previous 30 Days"</item>
    /// <item>"Older"</item>
    /// </list>
    /// </remarks>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// Gets or sets whether the group is expanded (vs collapsed).
    /// </summary>
    /// <remarks>
    /// When false, the group header is visible but conversations are hidden.
    /// Default is true (expanded). State persists during session only.
    /// </remarks>
    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>
    /// Gets the collection of conversations in this group.
    /// </summary>
    /// <remarks>
    /// Ordered by <see cref="ConversationSummaryViewModel.IsPinned"/> (pinned first),
    /// then by <see cref="ConversationSummaryViewModel.UpdatedAt"/> (most recent first).
    /// </remarks>
    public ObservableCollection<ConversationSummaryViewModel> Conversations { get; } = new();

    /// <summary>
    /// Gets the number of conversations in this group.
    /// </summary>
    /// <remarks>
    /// Displayed in group header as badge, e.g., "Today (3)".
    /// Automatically updated when <see cref="Conversations"/> changes.
    /// </remarks>
    public int Count => Conversations.Count;

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles changes to the Conversations collection.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void OnConversationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Notify Count property has changed when collection changes
        OnPropertyChanged(nameof(Count));
    }

    #endregion

    #region Static Factory

    /// <summary>
    /// Gets the display title for a <see cref="DateGroup"/> value.
    /// </summary>
    /// <param name="group">The date group.</param>
    /// <returns>The localized display title.</returns>
    public static string GetTitleForGroup(DateGroup group) => group switch
    {
        DateGroup.Today => "Today",
        DateGroup.Yesterday => "Yesterday",
        DateGroup.Previous7Days => "Previous 7 Days",
        DateGroup.Previous30Days => "Previous 30 Days",
        DateGroup.Older => "Older",
        _ => "Other"
    };

    #endregion
}
