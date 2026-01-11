using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

public partial class SearchViewModel : ViewModelBase, IDisposable
{
    private readonly ISearchService _searchService;
    private readonly System.Timers.Timer _debounceTimer;
    private CancellationTokenSource? _searchCts;
    private bool _disposed;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _searchStatus = string.Empty;

    [ObservableProperty]
    private SearchResult? _selectedResult;

    [ObservableProperty]
    private SearchResultType? _filterType;

    public ObservableCollection<SearchResult> ConversationResults { get; } = [];
    public ObservableCollection<SearchResult> MessageResults { get; } = [];

    public event EventHandler<Guid>? NavigateToConversation;
    public event EventHandler? CloseRequested;

    public SearchViewModel(ISearchService searchService)
    {
        _searchService = searchService;

        _debounceTimer = new System.Timers.Timer(300) { AutoReset = false };
        _debounceTimer.Elapsed += OnDebounceElapsed;
    }

    partial void OnQueryChanged(string value)
    {
        _debounceTimer.Stop();

        if (string.IsNullOrWhiteSpace(value))
        {
            ConversationResults.Clear();
            MessageResults.Clear();
            SearchStatus = string.Empty;
            return;
        }

        _debounceTimer.Start();
    }

    partial void OnFilterTypeChanged(SearchResultType? value)
    {
        // Re-trigger search when filter changes
        if (!string.IsNullOrWhiteSpace(Query))
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    private async void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
    {
        await ExecuteSearchAsync();
    }

    private async Task ExecuteSearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        try
        {
            IsSearching = true;
            SearchStatus = "Searching...";

            var query = FilterType switch
            {
                SearchResultType.Conversation => SearchQuery.Conversations(Query),
                SearchResultType.Message => SearchQuery.Messages(Query),
                _ => SearchQuery.All(Query)
            };

            var results = await _searchService.SearchAsync(query, _searchCts.Token);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                ConversationResults.Clear();
                MessageResults.Clear();

                foreach (var result in results.Conversations)
                    ConversationResults.Add(result);

                foreach (var result in results.Messages)
                    MessageResults.Add(result);

                SearchStatus = $"Found {results.TotalCount} results in {results.SearchDuration.TotalMilliseconds:F0}ms";

                // Auto-select first result if none selected
                if (SelectedResult is null && (ConversationResults.Count > 0 || MessageResults.Count > 0))
                {
                    SelectedResult = ConversationResults.FirstOrDefault() ?? MessageResults.FirstOrDefault();
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when a new search cancels the previous one
        }
        catch (Exception ex)
        {
            SearchStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void SelectResult(SearchResult? result)
    {
        if (result is null) return;

        var conversationId = result.Type == SearchResultType.Conversation
            ? result.Id
            : result.ConversationId!.Value;

        NavigateToConversation?.Invoke(this, conversationId);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SetFilter(SearchResultType? type)
    {
        FilterType = type;
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void NavigateUp()
    {
        var allResults = ConversationResults.Concat(MessageResults).ToList();
        if (allResults.Count == 0) return;

        var currentIndex = SelectedResult is null ? 0 : allResults.IndexOf(SelectedResult);
        SelectedResult = allResults[Math.Max(0, currentIndex - 1)];
    }

    [RelayCommand]
    private void NavigateDown()
    {
        var allResults = ConversationResults.Concat(MessageResults).ToList();
        if (allResults.Count == 0) return;

        var currentIndex = SelectedResult is null ? -1 : allResults.IndexOf(SelectedResult);
        SelectedResult = allResults[Math.Min(allResults.Count - 1, currentIndex + 1)];
    }

    public void Dispose()
    {
        if (_disposed) return;

        _debounceTimer.Elapsed -= OnDebounceElapsed;
        _debounceTimer.Dispose();
        _searchCts?.Cancel();
        _searchCts?.Dispose();

        _disposed = true;
    }
}
