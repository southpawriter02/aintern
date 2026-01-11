using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the conversation list sidebar.
/// Handles loading, searching, and managing conversations.
/// </summary>
public sealed partial class ConversationListViewModel : ObservableObject, IDisposable
{
    private readonly IConversationService _conversationService;
    private readonly System.Timers.Timer _searchDebounceTimer;
    private string _pendingSearchQuery = string.Empty;
    private bool _disposed;

    private const int SearchDebounceMs = 300;

    [ObservableProperty]
    private ObservableCollection<ConversationSummaryViewModel> _conversations = new();

    [ObservableProperty]
    private ConversationSummaryViewModel? _selectedConversation;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    public ConversationListViewModel(IConversationService conversationService)
    {
        _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));

        _searchDebounceTimer = new System.Timers.Timer(SearchDebounceMs);
        _searchDebounceTimer.AutoReset = false;
        _searchDebounceTimer.Elapsed += async (_, _) => await ExecuteSearchAsync();

        _conversationService.ConversationListChanged += OnConversationListChanged;
    }

    partial void OnSearchQueryChanged(string value)
    {
        _pendingSearchQuery = value;
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    partial void OnSelectedConversationChanged(ConversationSummaryViewModel? value)
    {
        // Clear previous selection
        foreach (var conv in Conversations)
        {
            conv.IsSelected = false;
        }

        if (value is not null)
        {
            value.IsSelected = true;
        }
    }

    [RelayCommand]
    private async Task LoadConversationsAsync()
    {
        IsLoading = true;
        try
        {
            var summaries = await _conversationService.GetRecentConversationsAsync();

            Conversations.Clear();
            foreach (var summary in summaries)
            {
                Conversations.Add(new ConversationSummaryViewModel(summary));
            }

            IsEmpty = Conversations.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateNewConversationAsync()
    {
        await _conversationService.CreateNewConversationAsync();
        await LoadConversationsAsync();

        // Select the new conversation (should be first in list)
        if (Conversations.Count > 0)
        {
            SelectedConversation = Conversations[0];
        }
    }

    [RelayCommand]
    private async Task SelectConversationAsync(ConversationSummaryViewModel? conversation)
    {
        if (conversation is null || conversation.Id == SelectedConversation?.Id)
        {
            return;
        }

        IsLoading = true;
        try
        {
            await _conversationService.LoadConversationAsync(conversation.Id);
            SelectedConversation = conversation;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteConversationAsync(ConversationSummaryViewModel? conversation)
    {
        if (conversation is null)
        {
            return;
        }

        await _conversationService.DeleteConversationAsync(conversation.Id);

        // Remove from list
        Conversations.Remove(conversation);
        IsEmpty = Conversations.Count == 0;

        // If we deleted the selected conversation, select another
        if (SelectedConversation?.Id == conversation.Id)
        {
            SelectedConversation = Conversations.FirstOrDefault();
            if (SelectedConversation is not null)
            {
                await SelectConversationAsync(SelectedConversation);
            }
        }
    }

    [RelayCommand]
    private async Task RenameConversationAsync(ConversationSummaryViewModel? conversation)
    {
        if (conversation is null)
        {
            return;
        }

        // This would typically be triggered by UI with a new title
        // For now, just mark for potential rename dialog
        await Task.CompletedTask;
    }

    public async Task RenameConversationAsync(Guid conversationId, string newTitle)
    {
        await _conversationService.RenameConversationAsync(conversationId, newTitle);

        // Update local view model
        var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation is not null)
        {
            conversation.Title = newTitle;
        }
    }

    private async Task ExecuteSearchAsync()
    {
        var query = _pendingSearchQuery;

        IsLoading = true;
        try
        {
            var summaries = string.IsNullOrWhiteSpace(query)
                ? await _conversationService.GetRecentConversationsAsync()
                : await _conversationService.SearchConversationsAsync(query);

            Conversations.Clear();
            foreach (var summary in summaries)
            {
                Conversations.Add(new ConversationSummaryViewModel(summary));
            }

            IsEmpty = Conversations.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OnConversationListChanged(object? sender, ConversationListChangedEventArgs e)
    {
        // Refresh the list when conversations change
        await LoadConversationsAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _conversationService.ConversationListChanged -= OnConversationListChanged;
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Dispose();
        _disposed = true;
    }
}
