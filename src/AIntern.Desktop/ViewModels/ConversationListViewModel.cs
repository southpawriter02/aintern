namespace AIntern.Desktop.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Enums;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

/// <summary>
/// ViewModel for the conversation list in the sidebar.
/// </summary>
/// <remarks>
/// <para>
/// Manages loading, searching, grouping, and CRUD operations for conversations.
/// Bridges the <see cref="IConversationService"/> with the sidebar UI.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item>Grouped display: Organizes conversations by date (Today, Yesterday, etc.)</item>
/// <item>Debounced search: 300ms delay to prevent excessive API calls</item>
/// <item>Event-driven updates: Subscribes to service events for real-time UI sync</item>
/// <item>Thread-safe: Uses <see cref="IDispatcher"/> for UI thread marshalling</item>
/// </list>
/// </para>
/// <para>
/// Commands available:
/// <list type="bullet">
/// <item><see cref="LoadConversationsCommand"/> - Initial load and refresh</item>
/// <item><see cref="CreateNewConversationCommand"/> - Create new conversation</item>
/// <item><see cref="SelectConversationCommand"/> - Select and load conversation</item>
/// <item><see cref="DeleteConversationCommand"/> - Delete conversation</item>
/// <item><see cref="RenameConversationCommand"/> - Begin inline rename</item>
/// <item><see cref="ConfirmRenameCommand"/> - Confirm rename</item>
/// <item><see cref="CancelRenameCommand"/> - Cancel rename</item>
/// <item><see cref="ArchiveConversationCommand"/> - Archive conversation</item>
/// <item><see cref="TogglePinCommand"/> - Toggle pin status</item>
/// <item><see cref="ClearSearchCommand"/> - Clear search query</item>
/// </list>
/// </para>
/// </remarks>
public partial class ConversationListViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly IConversationService _conversationService;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<ConversationListViewModel> _logger;

    private CancellationTokenSource? _searchCts;
    private bool _isDisposed;

    /// <summary>
    /// Search debounce delay in milliseconds.
    /// </summary>
    /// <remarks>
    /// 300ms provides a good balance between responsiveness and
    /// reducing unnecessary API calls during rapid typing.
    /// </remarks>
    private const int SearchDebounceMs = 300;

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets or sets the conversation groups organized by date.
    /// </summary>
    /// <remarks>
    /// Each group contains conversations from a specific date range.
    /// Groups are ordered: Today → Yesterday → Previous 7 Days → etc.
    /// </remarks>
    [ObservableProperty]
    private ObservableCollection<ConversationGroupViewModel> _groups = new();

    /// <summary>
    /// Gets or sets the currently selected conversation.
    /// </summary>
    /// <remarks>
    /// Updates when user selects a conversation or when service fires
    /// <see cref="IConversationService.ConversationChanged"/> event.
    /// </remarks>
    [ObservableProperty]
    private ConversationSummaryViewModel? _selectedConversation;

    /// <summary>
    /// Gets or sets the search query text.
    /// </summary>
    /// <remarks>
    /// Changes trigger debounced search after <see cref="SearchDebounceMs"/>.
    /// Empty string shows all recent conversations.
    /// </remarks>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Gets or sets whether conversations are being loaded.
    /// </summary>
    /// <remarks>
    /// True during initial load, refresh, and search operations.
    /// Bind to show loading spinner in UI.
    /// </remarks>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets whether the conversation list is empty.
    /// </summary>
    /// <remarks>
    /// True when no conversations exist or search returns no results.
    /// Bind to show empty state message in UI.
    /// </remarks>
    [ObservableProperty]
    private bool _isEmpty;

    /// <summary>
    /// Gets or sets whether a search is currently active.
    /// </summary>
    /// <remarks>
    /// True when <see cref="SearchQuery"/> is non-empty.
    /// Used to show search results mode in UI.
    /// </remarks>
    [ObservableProperty]
    private bool _isSearching;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationListViewModel"/> class.
    /// </summary>
    /// <param name="conversationService">The conversation service for data operations.</param>
    /// <param name="dispatcher">The dispatcher for UI thread marshalling.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any parameter is null.
    /// </exception>
    public ConversationListViewModel(
        IConversationService conversationService,
        IDispatcher dispatcher,
        ILogger<ConversationListViewModel> logger)
    {
        _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("[INIT] ConversationListViewModel created");

        // Subscribe to service events
        _conversationService.ConversationListChanged += OnConversationListChanged;
        _conversationService.ConversationChanged += OnConversationChanged;

        _logger.LogDebug("[INIT] Subscribed to service events");
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the ViewModel and loads conversations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// Call this method after constructing the ViewModel to populate
    /// the conversation list. Typically called from view's Loaded event.
    /// </remarks>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] InitializeAsync");

        await LoadConversationsAsync();

        _logger.LogDebug("[EXIT] InitializeAsync completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Loads conversations from the database.
    /// </summary>
    [RelayCommand]
    private async Task LoadConversationsAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] LoadConversationsAsync");

        try
        {
            IsLoading = true;
            ClearError();

            // Fetch conversations from service
            var conversations = await _conversationService.GetRecentConversationsAsync();
            _logger.LogDebug("[INFO] Fetched {Count} conversations", conversations.Count);

            // Update UI on dispatcher thread
            await _dispatcher.InvokeAsync(() =>
            {
                UpdateGroups(conversations);
                UpdateSelection();
            });

            _logger.LogInformation("Loaded {Count} conversations", conversations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] Failed to load conversations");
            SetError($"Failed to load conversations: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _logger.LogDebug("[EXIT] LoadConversationsAsync completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    [RelayCommand]
    private async Task CreateNewConversationAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] CreateNewConversationAsync");

        try
        {
            ClearError();
            await _conversationService.CreateNewConversationAsync();
            _logger.LogInformation("Created new conversation");
            // Selection will be updated via ConversationChanged event
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] Failed to create conversation");
            SetError($"Failed to create conversation: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("[EXIT] CreateNewConversationAsync completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Selects and loads a conversation.
    /// </summary>
    /// <param name="summary">The conversation to select.</param>
    [RelayCommand]
    private async Task SelectConversationAsync(ConversationSummaryViewModel? summary)
    {
        if (summary == null)
        {
            _logger.LogDebug("[SKIP] SelectConversationAsync - summary is null");
            return;
        }

        // Skip if already selected
        if (summary.Id == _conversationService.CurrentConversation.Id)
        {
            _logger.LogDebug("[SKIP] SelectConversationAsync - already selected: {Id}", summary.Id);
            return;
        }

        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SelectConversationAsync - Id: {Id}", summary.Id);

        try
        {
            IsLoading = true;
            ClearError();

            await _conversationService.LoadConversationAsync(summary.Id);
            SelectedConversation = summary;

            _logger.LogInformation("Selected conversation: {Id} - {Title}", summary.Id, summary.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] Failed to load conversation {Id}", summary.Id);
            SetError($"Failed to load conversation: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _logger.LogDebug("[EXIT] SelectConversationAsync completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Deletes a conversation.
    /// </summary>
    /// <param name="summary">The conversation to delete.</param>
    [RelayCommand]
    private async Task DeleteConversationAsync(ConversationSummaryViewModel summary)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] DeleteConversationAsync - Id: {Id}", summary.Id);

        try
        {
            ClearError();
            await _conversationService.DeleteConversationAsync(summary.Id);
            _logger.LogInformation("Deleted conversation: {Id}", summary.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] Failed to delete conversation {Id}", summary.Id);
            SetError($"Failed to delete conversation: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("[EXIT] DeleteConversationAsync completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Begins renaming a conversation.
    /// </summary>
    /// <param name="summary">The conversation to rename.</param>
    [RelayCommand]
    private void RenameConversation(ConversationSummaryViewModel summary)
    {
        _logger.LogDebug("[ENTER] RenameConversation - Id: {Id}", summary.Id);
        summary.BeginRename();
        _logger.LogDebug("[EXIT] RenameConversation - IsRenaming: {IsRenaming}", summary.IsRenaming);
    }

    /// <summary>
    /// Confirms the rename operation.
    /// </summary>
    /// <param name="summary">The conversation being renamed.</param>
    [RelayCommand]
    private async Task ConfirmRenameAsync(ConversationSummaryViewModel summary)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] ConfirmRenameAsync - Id: {Id}, NewTitle: {NewTitle}",
            summary.Id, summary.EditingTitle);

        // No change - just cancel
        if (summary.EditingTitle == summary.Title)
        {
            summary.CancelRename();
            _logger.LogDebug("[SKIP] ConfirmRenameAsync - title unchanged");
            return;
        }

        try
        {
            ClearError();
            await _conversationService.RenameConversationAsync(summary.Id, summary.EditingTitle);
            summary.Title = summary.EditingTitle;
            _logger.LogInformation("Renamed conversation {Id} to: {Title}", summary.Id, summary.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] Failed to rename conversation {Id}", summary.Id);
            SetError($"Failed to rename conversation: {ex.Message}");
        }
        finally
        {
            summary.IsRenaming = false;
            _logger.LogDebug("[EXIT] ConfirmRenameAsync completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Cancels the rename operation.
    /// </summary>
    /// <param name="summary">The conversation being renamed.</param>
    [RelayCommand]
    private void CancelRename(ConversationSummaryViewModel summary)
    {
        _logger.LogDebug("[ENTER] CancelRename - Id: {Id}", summary.Id);
        summary.CancelRename();
        _logger.LogDebug("[EXIT] CancelRename - reverted to: {Title}", summary.Title);
    }

    /// <summary>
    /// Archives a conversation.
    /// </summary>
    /// <param name="summary">The conversation to archive.</param>
    [RelayCommand]
    private async Task ArchiveConversationAsync(ConversationSummaryViewModel summary)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] ArchiveConversationAsync - Id: {Id}", summary.Id);

        try
        {
            ClearError();
            await _conversationService.ArchiveConversationAsync(summary.Id);
            _logger.LogInformation("Archived conversation: {Id}", summary.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] Failed to archive conversation {Id}", summary.Id);
            SetError($"Failed to archive conversation: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("[EXIT] ArchiveConversationAsync completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Toggles the pin status of a conversation.
    /// </summary>
    /// <param name="summary">The conversation to toggle.</param>
    [RelayCommand]
    private async Task TogglePinAsync(ConversationSummaryViewModel summary)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] TogglePinAsync - Id: {Id}, CurrentIsPinned: {IsPinned}",
            summary.Id, summary.IsPinned);

        try
        {
            ClearError();

            if (summary.IsPinned)
            {
                await _conversationService.UnpinConversationAsync(summary.Id);
                summary.IsPinned = false;
                _logger.LogInformation("Unpinned conversation: {Id}", summary.Id);
            }
            else
            {
                await _conversationService.PinConversationAsync(summary.Id);
                summary.IsPinned = true;
                _logger.LogInformation("Pinned conversation: {Id}", summary.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] Failed to update pin status for {Id}", summary.Id);
            SetError($"Failed to update pin status: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("[EXIT] TogglePinAsync completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Clears the search query.
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        _logger.LogDebug("[ENTER] ClearSearch");
        SearchQuery = string.Empty;
        _logger.LogDebug("[EXIT] ClearSearch");
    }

    #endregion

    #region Search

    /// <summary>
    /// Called when SearchQuery changes. Implements debounced search.
    /// </summary>
    /// <param name="value">The new search query value.</param>
    partial void OnSearchQueryChanged(string value)
    {
        _logger.LogDebug("[ENTER] OnSearchQueryChanged - Query: '{Query}'", value);

        // Cancel any pending search
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        // Start debounced search
        _ = SearchWithDebounceAsync(value, _searchCts.Token);

        _logger.LogDebug("[EXIT] OnSearchQueryChanged - debounce started");
    }

    /// <summary>
    /// Performs a debounced search operation.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task SearchWithDebounceAsync(string query, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SearchWithDebounceAsync - Query: '{Query}'", query);

        try
        {
            // Wait for debounce period
            await Task.Delay(SearchDebounceMs, ct);

            if (ct.IsCancellationRequested)
            {
                _logger.LogDebug("[CANCELLED] SearchWithDebounceAsync - debounce cancelled");
                return;
            }

            // Update searching state
            IsSearching = !string.IsNullOrWhiteSpace(query);

            // Perform search
            var conversations = await _conversationService.SearchConversationsAsync(query, ct);
            _logger.LogDebug("[INFO] Search returned {Count} results", conversations.Count);

            if (ct.IsCancellationRequested)
            {
                _logger.LogDebug("[CANCELLED] SearchWithDebounceAsync - search cancelled");
                return;
            }

            // Update UI on dispatcher thread
            await _dispatcher.InvokeAsync(() => UpdateGroups(conversations));

            _logger.LogInformation("Search for '{Query}' returned {Count} results", query, conversations.Count);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled - ignore
            _logger.LogDebug("[CANCELLED] SearchWithDebounceAsync - operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] Search failed for query '{Query}'", query);
            SetError($"Search failed: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("[EXIT] SearchWithDebounceAsync completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Updates the Groups collection with new conversation data.
    /// </summary>
    /// <param name="conversations">The conversations to display.</param>
    /// <remarks>
    /// Called on UI thread via dispatcher. Clears existing groups and
    /// rebuilds from the provided data, organizing by date group.
    /// </remarks>
    private void UpdateGroups(IReadOnlyList<ConversationSummary> conversations)
    {
        _logger.LogDebug("[ENTER] UpdateGroups - Count: {Count}", conversations.Count);

        Groups.Clear();

        if (!conversations.Any())
        {
            IsEmpty = true;
            _logger.LogDebug("[EXIT] UpdateGroups - list is empty");
            return;
        }

        IsEmpty = false;

        // Group by date
        var grouped = conversations
            .GroupBy(c => c.GetDateGroup())
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var groupVm = new ConversationGroupViewModel
            {
                DateGroup = group.Key,
                Title = ConversationGroupViewModel.GetTitleForGroup(group.Key),
                IsExpanded = true
            };

            // Within group: pinned first, then by UpdatedAt descending
            foreach (var conv in group
                .OrderByDescending(c => c.IsPinned)
                .ThenByDescending(c => c.UpdatedAt))
            {
                groupVm.Conversations.Add(new ConversationSummaryViewModel
                {
                    Id = conv.Id,
                    Title = conv.Title,
                    UpdatedAt = conv.UpdatedAt,
                    MessageCount = conv.MessageCount,
                    Preview = conv.Preview,
                    IsPinned = conv.IsPinned,
                    ModelName = conv.ModelName
                });
            }

            Groups.Add(groupVm);
            _logger.LogDebug("[INFO] Added group '{Title}' with {Count} conversations",
                groupVm.Title, groupVm.Count);
        }

        _logger.LogDebug("[EXIT] UpdateGroups - created {GroupCount} groups", Groups.Count);
    }

    /// <summary>
    /// Updates the selection state based on current conversation.
    /// </summary>
    /// <remarks>
    /// Sets <see cref="ConversationSummaryViewModel.IsSelected"/> to true
    /// for the item matching the current conversation ID.
    /// </remarks>
    private void UpdateSelection()
    {
        var currentId = _conversationService.CurrentConversation?.Id ?? Guid.Empty;
        _logger.LogDebug("[ENTER] UpdateSelection - CurrentId: {Id}", currentId);

        foreach (var group in Groups)
        {
            foreach (var conv in group.Conversations)
            {
                conv.IsSelected = conv.Id == currentId;
                if (conv.IsSelected)
                {
                    SelectedConversation = conv;
                    _logger.LogDebug("[INFO] Selected: {Id} - {Title}", conv.Id, conv.Title);
                }
            }
        }

        _logger.LogDebug("[EXIT] UpdateSelection");
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the ConversationListChanged event from the service.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments.</param>
    private async void OnConversationListChanged(object? sender, ConversationListChangedEventArgs e)
    {
        _logger.LogDebug("[EVENT] ConversationListChanged - Type: {Type}, AffectedId: {Id}",
            e.ChangeType, e.AffectedConversationId);

        await _dispatcher.InvokeAsync(async () =>
        {
            await LoadConversationsAsync();
        });
    }

    /// <summary>
    /// Handles the ConversationChanged event from the service.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments.</param>
    private void OnConversationChanged(object? sender, ConversationChangedEventArgs e)
    {
        _logger.LogDebug("[EVENT] ConversationChanged - Type: {Type}, ConversationId: {Id}",
            e.ChangeType, e.Conversation?.Id);

        _ = _dispatcher.InvokeAsync(() =>
        {
            // Update selection when conversation changes
            if (e.ChangeType == ConversationChangeType.Loaded ||
                e.ChangeType == ConversationChangeType.Created)
            {
                UpdateSelection();
            }
        });
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes of the ViewModel, unsubscribing from events.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        _logger.LogDebug("[ENTER] Dispose");

        _isDisposed = true;

        // Unsubscribe from events
        _conversationService.ConversationListChanged -= OnConversationListChanged;
        _conversationService.ConversationChanged -= OnConversationChanged;

        // Cancel and dispose search CTS
        _searchCts?.Cancel();
        _searchCts?.Dispose();

        _logger.LogDebug("[EXIT] Dispose - cleaned up");
    }

    #endregion
}
