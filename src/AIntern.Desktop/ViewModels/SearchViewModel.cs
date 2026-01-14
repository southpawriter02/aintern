// -----------------------------------------------------------------------
// <copyright file="SearchViewModel.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     ViewModel for the search dialog with debounced search and keyboard navigation.
//     Added in v0.2.5e.
// </summary>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Enums;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the search dialog providing full-text search with debouncing,
/// filtering, and keyboard navigation.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel manages the spotlight-style search dialog (Ctrl+K) with:
/// </para>
/// <list type="bullet">
///   <item><description><b>Debounced Search:</b> 300ms delay after typing stops</description></item>
///   <item><description><b>Cancellation:</b> Previous searches cancelled when new query entered</description></item>
///   <item><description><b>Filtering:</b> All, Conversations only, Messages only</description></item>
///   <item><description><b>Navigation:</b> Up/Down keyboard navigation with wrap-around</description></item>
///   <item><description><b>Grouped Results:</b> Separate collections for conversations and messages</description></item>
/// </list>
/// <para>
/// <b>Search Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Empty/whitespace query clears results immediately</description></item>
///   <item><description>Non-empty query starts debounce timer (300ms)</description></item>
///   <item><description>Timer elapsed triggers actual search</description></item>
///   <item><description>New query while searching cancels previous search</description></item>
/// </list>
/// <para>
/// <b>Threading:</b> Search executes on background thread, UI updates
/// marshalled to UI thread via <see cref="Dispatcher.UIThread"/>.
/// </para>
/// <para>Added in v0.2.5e.</para>
/// </remarks>
public sealed partial class SearchViewModel : ViewModelBase, IDisposable
{
    #region Constants

    /// <summary>
    /// Debounce delay in milliseconds before executing search.
    /// </summary>
    /// <value>300</value>
    private const int DebounceDelayMs = 300;

    #endregion

    #region Fields

    private readonly ISearchService _searchService;
    private readonly ILogger<SearchViewModel>? _logger;
    private readonly System.Timers.Timer _debounceTimer;
    private CancellationTokenSource? _searchCts;
    private bool _disposed;

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets or sets the search query text.
    /// </summary>
    /// <value>The current search query entered by the user.</value>
    /// <remarks>
    /// <para>
    /// Changing this property triggers the debounce timer. After 300ms of no
    /// further changes, the search executes.
    /// </para>
    /// <para>
    /// Empty or whitespace values immediately clear results without searching.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string _query = string.Empty;

    /// <summary>
    /// Gets or sets whether a search is currently in progress.
    /// </summary>
    /// <value>True if searching, false otherwise.</value>
    /// <remarks>
    /// Bound to a progress indicator in the UI.
    /// </remarks>
    [ObservableProperty]
    private bool _isSearching;

    /// <summary>
    /// Gets or sets the search status message.
    /// </summary>
    /// <value>Status text like "5 results in 12.34 ms" or "No results found".</value>
    /// <remarks>
    /// Displayed in the status bar at the bottom of the search dialog.
    /// Uses <see cref="SearchResults.Summary"/> format.
    /// </remarks>
    [ObservableProperty]
    private string _searchStatus = string.Empty;

    /// <summary>
    /// Gets or sets the currently selected search result.
    /// </summary>
    /// <value>The highlighted result, or null if no results.</value>
    /// <remarks>
    /// <para>
    /// Updated by keyboard navigation (Up/Down) and mouse selection.
    /// Pressing Enter activates this result.
    /// </para>
    /// <para>
    /// Automatically set to first result after search completes.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private SearchResult? _selectedResult;

    /// <summary>
    /// Gets or sets the current filter type for search results.
    /// </summary>
    /// <value>
    /// Null for all results, <see cref="SearchResultType.Conversation"/> for
    /// conversations only, or <see cref="SearchResultType.Message"/> for messages only.
    /// </value>
    /// <remarks>
    /// Changing this property triggers a new search with the current query.
    /// </remarks>
    [ObservableProperty]
    private SearchResultType? _filterType;

    /// <summary>
    /// Gets or sets the result that was selected when the dialog closes.
    /// </summary>
    /// <value>The selected result to navigate to, or null if cancelled.</value>
    /// <remarks>
    /// Set by <see cref="SelectResultCommand"/> before closing.
    /// The dialog returns this value to the caller.
    /// </remarks>
    [ObservableProperty]
    private SearchResult? _dialogResult;

    /// <summary>
    /// Gets or sets whether the dialog should close.
    /// </summary>
    /// <value>True to trigger dialog close.</value>
    /// <remarks>
    /// The view monitors this property to close the dialog.
    /// </remarks>
    [ObservableProperty]
    private bool _shouldClose;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the collection of conversation search results.
    /// </summary>
    /// <value>Observable collection of results where <see cref="SearchResult.IsConversationResult"/> is true.</value>
    /// <remarks>
    /// Bound to the Conversations group in the search results list.
    /// </remarks>
    public ObservableCollection<SearchResult> ConversationResults { get; } = new();

    /// <summary>
    /// Gets the collection of message search results.
    /// </summary>
    /// <value>Observable collection of results where <see cref="SearchResult.IsMessageResult"/> is true.</value>
    /// <remarks>
    /// Bound to the Messages group in the search results list.
    /// </remarks>
    public ObservableCollection<SearchResult> MessageResults { get; } = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchViewModel"/> class.
    /// </summary>
    /// <param name="searchService">The search service for executing FTS5 queries.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="searchService"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Sets up the debounce timer with 300ms interval. The timer is configured
    /// to not auto-reset - it fires once per query change sequence.
    /// </para>
    /// <para>
    /// The ViewModel is typically created fresh for each dialog instance
    /// (registered as transient in DI).
    /// </para>
    /// </remarks>
    public SearchViewModel(
        ISearchService searchService,
        ILogger<SearchViewModel>? logger = null)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger;

        // Setup debounce timer
        _debounceTimer = new System.Timers.Timer(DebounceDelayMs);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += OnDebounceElapsed;

        _logger?.LogDebug("[INIT] SearchViewModel created");
    }

    #endregion

    #region Property Changed Handlers

    /// <summary>
    /// Called when <see cref="Query"/> changes.
    /// </summary>
    /// <param name="value">The new query value.</param>
    /// <remarks>
    /// Stops any existing debounce timer and either clears results (for empty query)
    /// or starts a new debounce countdown.
    /// </remarks>
    partial void OnQueryChanged(string value)
    {
        _debounceTimer.Stop();

        if (string.IsNullOrWhiteSpace(value))
        {
            _logger?.LogDebug("[INFO] Query cleared - clearing results");
            ClearResults();
        }
        else
        {
            _logger?.LogDebug("[INFO] Query changed, starting debounce timer");
            _debounceTimer.Start();
        }
    }

    /// <summary>
    /// Called when <see cref="FilterType"/> changes.
    /// </summary>
    /// <param name="value">The new filter type.</param>
    /// <remarks>
    /// Triggers a new search with the updated filter if there's a valid query.
    /// </remarks>
    partial void OnFilterTypeChanged(SearchResultType? value)
    {
        _logger?.LogDebug("[INFO] Filter changed to: {Filter}", value?.ToString() ?? "All");

        if (!string.IsNullOrWhiteSpace(Query))
        {
            // Re-search with new filter
            _debounceTimer.Stop();
            _ = ExecuteSearchAsync();
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Sets the filter type and triggers a re-search.
    /// </summary>
    /// <param name="filterValue">
    /// The filter value as string: empty/null for All, "Conversation", or "Message".
    /// </param>
    /// <remarks>
    /// Used by filter tab buttons in the UI. The string parameter allows
    /// binding from XAML CommandParameter.
    /// </remarks>
    [RelayCommand]
    private void SetFilter(string? filterValue)
    {
        _logger?.LogDebug("[ENTER] SetFilter - Value: {Value}", filterValue ?? "null");

        FilterType = filterValue switch
        {
            "Conversation" => SearchResultType.Conversation,
            "Message" => SearchResultType.Message,
            _ => null
        };

        _logger?.LogDebug("[EXIT] SetFilter");
    }

    /// <summary>
    /// Navigates to the previous result in the list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Moves selection up through the combined results list (conversations
    /// then messages). Wraps to the bottom when at the top.
    /// </para>
    /// <para>
    /// Bound to Up arrow key in the search dialog.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void NavigateUp()
    {
        var allResults = GetAllResults();
        if (allResults.Count == 0)
        {
            return;
        }

        var currentIndex = SelectedResult is null ? 0 : allResults.IndexOf(SelectedResult);
        var newIndex = currentIndex <= 0 ? allResults.Count - 1 : currentIndex - 1;
        SelectedResult = allResults[newIndex];

        _logger?.LogDebug("[INFO] NavigateUp - Selected index: {Index}", newIndex);
    }

    /// <summary>
    /// Navigates to the next result in the list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Moves selection down through the combined results list (conversations
    /// then messages). Wraps to the top when at the bottom.
    /// </para>
    /// <para>
    /// Bound to Down arrow key in the search dialog.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void NavigateDown()
    {
        var allResults = GetAllResults();
        if (allResults.Count == 0)
        {
            return;
        }

        var currentIndex = SelectedResult is null ? -1 : allResults.IndexOf(SelectedResult);
        var newIndex = currentIndex >= allResults.Count - 1 ? 0 : currentIndex + 1;
        SelectedResult = allResults[newIndex];

        _logger?.LogDebug("[INFO] NavigateDown - Selected index: {Index}", newIndex);
    }

    /// <summary>
    /// Selects the current result and closes the dialog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sets <see cref="DialogResult"/> to the current selection and
    /// <see cref="ShouldClose"/> to true to trigger dialog close.
    /// </para>
    /// <para>
    /// Bound to Enter key in the search dialog.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void SelectResult()
    {
        _logger?.LogDebug("[ENTER] SelectResult");

        if (SelectedResult is not null)
        {
            DialogResult = SelectedResult;
            _logger?.LogInformation("[INFO] Result selected: {Type} - {Title}",
                SelectedResult.TypeLabel, SelectedResult.Title);
        }

        ShouldClose = true;

        _logger?.LogDebug("[EXIT] SelectResult");
    }

    /// <summary>
    /// Closes the dialog without selecting a result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sets <see cref="ShouldClose"/> to true while leaving
    /// <see cref="DialogResult"/> as null.
    /// </para>
    /// <para>
    /// Bound to Escape key in the search dialog.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void Close()
    {
        _logger?.LogDebug("[ENTER] Close");

        ShouldClose = true;

        _logger?.LogDebug("[EXIT] Close");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles debounce timer elapsed event.
    /// </summary>
    private async void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
    {
        _debounceTimer.Stop();
        await ExecuteSearchAsync();
    }

    /// <summary>
    /// Executes the search operation.
    /// </summary>
    private async Task ExecuteSearchAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] ExecuteSearchAsync - Query: {Query}, Filter: {Filter}",
            Query, FilterType?.ToString() ?? "All");

        // Cancel any previous search
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        try
        {
            IsSearching = true;

            // Build the query based on filter type
            var searchQuery = FilterType switch
            {
                SearchResultType.Conversation => SearchQuery.ConversationsOnly(Query),
                SearchResultType.Message => SearchQuery.MessagesOnly(Query),
                _ => SearchQuery.Simple(Query)
            };

            // Execute search
            var results = await _searchService.SearchAsync(searchQuery, ct);

            // Check for cancellation before updating UI
            if (ct.IsCancellationRequested)
            {
                _logger?.LogDebug("[SKIP] ExecuteSearchAsync - Cancelled");
                return;
            }

            // Update UI on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ConversationResults.Clear();
                MessageResults.Clear();

                foreach (var result in results.Results.Where(r => r.IsConversationResult))
                {
                    ConversationResults.Add(result);
                }

                foreach (var result in results.Results.Where(r => r.IsMessageResult))
                {
                    MessageResults.Add(result);
                }

                SearchStatus = results.Summary;
                SelectedResult = ConversationResults.FirstOrDefault() ?? MessageResults.FirstOrDefault();
            });

            _logger?.LogDebug("[EXIT] ExecuteSearchAsync - {Count} results in {Ms}ms",
                results.TotalCount, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("[SKIP] ExecuteSearchAsync - Cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] ExecuteSearchAsync - {Message}", ex.Message);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SearchStatus = "Search failed";
                SetError($"Search failed: {ex.Message}");
            });
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Clears all search results and status.
    /// </summary>
    private void ClearResults()
    {
        ConversationResults.Clear();
        MessageResults.Clear();
        SearchStatus = string.Empty;
        SelectedResult = null;
        ClearError();
    }

    /// <summary>
    /// Gets all results as a single list (conversations first, then messages).
    /// </summary>
    /// <returns>Combined list of all search results.</returns>
    private List<SearchResult> GetAllResults()
    {
        return ConversationResults.Concat(MessageResults).ToList();
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases resources used by this ViewModel.
    /// </summary>
    /// <remarks>
    /// Disposes the debounce timer and cancellation token source.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger?.LogDebug("[INFO] SearchViewModel disposing");

        _debounceTimer.Stop();
        _debounceTimer.Elapsed -= OnDebounceElapsed;
        _debounceTimer.Dispose();

        _searchCts?.Cancel();
        _searchCts?.Dispose();

        _disposed = true;

        _logger?.LogDebug("[INFO] SearchViewModel disposed");
    }

    #endregion
}
