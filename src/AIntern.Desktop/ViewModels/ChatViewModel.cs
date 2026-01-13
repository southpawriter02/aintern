using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Exceptions;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the chat interface panel.
/// Manages user input, message display, and streaming response generation.
/// </summary>
/// <remarks>
/// <para>
/// Supports streaming text generation with real-time UI updates via <see cref="IDispatcher"/>.
/// </para>
/// <para>
/// Integrates with:
/// <list type="bullet">
/// <item><see cref="ILlmService"/> for text generation</item>
/// <item><see cref="IConversationService"/> for message history and persistence</item>
/// <item><see cref="ISettingsService"/> for inference parameters</item>
/// <item><see cref="IDispatcher"/> for UI thread marshalling</item>
/// </list>
/// </para>
/// <para>
/// <b>Event Subscriptions:</b>
/// <list type="bullet">
/// <item><see cref="IConversationService.ConversationChanged"/> - Refreshes UI when conversation loads</item>
/// <item><see cref="IConversationService.SaveStateChanged"/> - Updates save status indicator</item>
/// </list>
/// </para>
/// </remarks>
public partial class ChatViewModel : ViewModelBase, IDisposable
{
    #region Fields

    // Service dependencies for LLM operations, conversation state, and settings
    private readonly ILlmService _llmService;
    private readonly IConversationService _conversationService;
    private readonly ISettingsService _settingsService;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<ChatViewModel>? _logger;

    // Cancellation token source for the current generation (allows cancellation)
    private CancellationTokenSource? _generationCts;

    // Tracks whether this instance has been disposed
    private bool _isDisposed;

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets or sets the current user input text.
    /// </summary>
    [ObservableProperty]
    private string _userInput = string.Empty;

    /// <summary>
    /// Gets or sets the collection of chat messages displayed in the UI.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ChatMessageViewModel> _messages = new();

    /// <summary>
    /// Gets or sets whether a response is currently being generated.
    /// </summary>
    [ObservableProperty]
    private bool _isGenerating;

    /// <summary>
    /// Gets or sets the number of tokens generated in the current response.
    /// </summary>
    [ObservableProperty]
    private int _tokenCount;

    /// <summary>
    /// Gets or sets whether the send command can be executed.
    /// </summary>
    [ObservableProperty]
    private bool _canSend;

    /// <summary>
    /// Gets or sets the current conversation title.
    /// </summary>
    /// <remarks>
    /// Updated when a conversation is loaded or the title is changed via
    /// <see cref="IConversationService.ConversationChanged"/> event.
    /// </remarks>
    [ObservableProperty]
    private string _conversationTitle = "New Conversation";

    /// <summary>
    /// Gets or sets whether there are unsaved changes in the current conversation.
    /// </summary>
    /// <remarks>
    /// Updated via <see cref="IConversationService.SaveStateChanged"/> event.
    /// Used to display save indicators in the UI.
    /// </remarks>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>
    /// Gets or sets the save status text for display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Possible values:
    /// <list type="bullet">
    /// <item><c>"Saving..."</c> - Save operation in progress</item>
    /// <item><c>"Saved"</c> - All changes saved successfully</item>
    /// <item><c>"Save failed"</c> - Save operation failed</item>
    /// <item><c>null</c> - No status to display (initial state)</item>
    /// </list>
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string? _saveStatus;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatViewModel"/> class.
    /// </summary>
    /// <param name="llmService">The LLM service for text generation.</param>
    /// <param name="conversationService">The conversation service for message history.</param>
    /// <param name="settingsService">The settings service for inference parameters.</param>
    /// <param name="dispatcher">The dispatcher for UI thread operations.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public ChatViewModel(
        ILlmService llmService,
        IConversationService conversationService,
        ISettingsService settingsService,
        IDispatcher dispatcher,
        ILogger<ChatViewModel>? logger = null)
    {
        var sw = Stopwatch.StartNew();

        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger;

        _logger?.LogDebug("[INIT] ChatViewModel construction started");

        // Re-evaluate CanSend whenever input or generation state changes
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(UserInput) or nameof(IsGenerating))
            {
                UpdateCanSend();
            }
        };

        // Also re-evaluate when model state changes (load/unload)
        _llmService.ModelStateChanged += (_, _) => UpdateCanSend();

        // Subscribe to conversation service events for state synchronization
        _conversationService.ConversationChanged += OnConversationChanged;
        _conversationService.SaveStateChanged += OnSaveStateChanged;

        // Load current conversation state into the UI
        RefreshFromConversation();

        _logger?.LogDebug("[INIT] ChatViewModel construction completed - {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    #endregion

    /// <summary>
    /// Updates the <see cref="CanSend"/> property based on current state.
    /// </summary>
    private void UpdateCanSend()
    {
        // Can only send if: has input, not generating, and model is loaded
        CanSend = !string.IsNullOrWhiteSpace(UserInput)
                  && !IsGenerating
                  && _llmService.IsModelLoaded;
    }

    /// <summary>
    /// Sends the current user input and generates a streaming response.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        // Guard: ensure we have valid input
        if (string.IsNullOrWhiteSpace(UserInput)) return;
        
        // Guard: ensure model is loaded
        if (!_llmService.IsModelLoaded)
        {
            SetError("Please load a model first.");
            return;
        }

        // Clear any previous errors and capture the user message
        ClearError();
        var userMessage = UserInput.Trim();
        UserInput = string.Empty; // Clear input field immediately for UX

        // Create and display the user message
        var userMessageVm = new ChatMessageViewModel
        {
            Role = MessageRole.User,
            Content = userMessage
        };
        Messages.Add(userMessageVm);
        
        // Also add to the conversation service for context tracking
        _conversationService.AddMessage(userMessageVm.ToChatMessage());

        // Create a placeholder for the assistant response (will be filled by streaming)
        var assistantMessageVm = new ChatMessageViewModel
        {
            Role = MessageRole.Assistant,
            IsStreaming = true // Shows typing indicator in UI
        };
        Messages.Add(assistantMessageVm);

        // Set up cancellation token for this generation
        _generationCts = new CancellationTokenSource();
        IsGenerating = true;
        TokenCount = 0;

        try
        {
            // Build inference options from current settings
            var settings = _settingsService.CurrentSettings;
            var options = new InferenceOptions(
                MaxTokens: settings.MaxTokens,       // Maximum response length
                Temperature: settings.Temperature,   // Randomness (0 = deterministic)
                TopP: settings.TopP                  // Nucleus sampling threshold
            );

            // Get the full conversation context for the model
            var conversation = _conversationService.GetMessages();

            // Stream tokens from the LLM and update UI in real-time
            await foreach (var token in _llmService.GenerateStreamingAsync(
                conversation, options, _generationCts.Token))
            {
                // Must update UI on the UI thread (Avalonia requirement)
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    assistantMessageVm.AppendContent(token);
                    TokenCount++;
                });
            }

            // Mark streaming as complete (removes typing indicator)
            assistantMessageVm.CompleteStreaming();

            // Save the completed response to conversation history
            _conversationService.AddMessage(assistantMessageVm.ToChatMessage());
        }
        catch (OperationCanceledException)
        {
            // User clicked cancel - mark message accordingly
            assistantMessageVm.MarkAsCancelled();
        }
        catch (InferenceException ex)
        {
            // Known inference error - display to user
            SetError($"Generation failed: {ex.Message}");
            assistantMessageVm.CompleteStreaming();
            
            // Show error in message if no content was generated
            if (string.IsNullOrEmpty(assistantMessageVm.Content))
            {
                assistantMessageVm.Content = "[Error: Generation failed]";
            }
        }
        catch (Exception ex)
        {
            // Unexpected error - log and display generic message
            SetError($"Unexpected error: {ex.Message}");
            assistantMessageVm.CompleteStreaming();
            
            if (string.IsNullOrEmpty(assistantMessageVm.Content))
            {
                assistantMessageVm.Content = "[Error occurred]";
            }
        }
        finally
        {
            // Always clean up generation state
            IsGenerating = false;
            _generationCts?.Dispose();
            _generationCts = null;
        }
    }

    /// <summary>
    /// Cancels the current text generation operation.
    /// </summary>
    [RelayCommand]
    private void CancelGeneration()
    {
        // Signal the generation task to stop
        _generationCts?.Cancel();
    }

    /// <summary>
    /// Clears all messages from the chat and resets the conversation.
    /// </summary>
    [RelayCommand]
    private void ClearChat()
    {
        // Clear UI message list
        Messages.Clear();

        // Clear conversation service history
        _conversationService.ClearConversation();

        // Clear any displayed errors
        ClearError();
    }

    /// <summary>
    /// Explicitly saves the current conversation to the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This command allows manual triggering of a save operation via the Ctrl+S
    /// keyboard shortcut. While auto-save handles most persistence, users may
    /// want to manually save to ensure changes are committed immediately.
    /// </para>
    /// <para>
    /// The command is a no-op if there are no unsaved changes, preventing
    /// unnecessary database writes.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task SaveAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] SaveAsync - HasUnsavedChanges: {HasUnsavedChanges}", HasUnsavedChanges);

        // Skip if no changes to save
        if (!HasUnsavedChanges)
        {
            _logger?.LogDebug("[SKIP] SaveAsync - No unsaved changes to save");
            return;
        }

        try
        {
            _logger?.LogInformation("[INFO] Manual save initiated by user");
            await _conversationService.SaveCurrentConversationAsync();
            _logger?.LogDebug("[INFO] Manual save completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] Manual save failed: {Message}", ex.Message);
            SetError($"Save failed: {ex.Message}");
        }

        _logger?.LogDebug("[EXIT] SaveAsync - {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Clears the unsaved changes flag without saving.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used when the user explicitly chooses to discard changes (e.g., clicking
    /// "Don't Save" in the unsaved changes dialog when closing the window).
    /// </para>
    /// <para>
    /// This method only clears the local flag; it does not affect the conversation
    /// service state. The conversation will still be considered "dirty" by the service,
    /// but this ViewModel will allow operations that check <see cref="HasUnsavedChanges"/>
    /// to proceed.
    /// </para>
    /// </remarks>
    public void ClearUnsavedChangesFlag()
    {
        _logger?.LogDebug("[INFO] ClearUnsavedChangesFlag called - clearing HasUnsavedChanges flag");
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Handles the Enter key press to send a message.
    /// Called from the view's key binding.
    /// </summary>
    public void HandleEnterKey()
    {
        // Only send if conditions are met (has input, not generating, model loaded)
        if (CanSend)
        {
            SendMessageCommand.Execute(null);
        }
    }

    #region Event Handlers

    /// <summary>
    /// Handles conversation state changes from the <see cref="IConversationService"/>.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments containing change details.</param>
    /// <remarks>
    /// <para>
    /// Responds to the following change types:
    /// <list type="bullet">
    /// <item><see cref="ConversationChangeType.Created"/> - Refreshes UI for new conversation</item>
    /// <item><see cref="ConversationChangeType.Loaded"/> - Refreshes UI with loaded messages</item>
    /// <item><see cref="ConversationChangeType.Cleared"/> - Clears message display</item>
    /// <item><see cref="ConversationChangeType.TitleChanged"/> - Updates title display only</item>
    /// <item><see cref="ConversationChangeType.Saved"/> - Updates save status indicator</item>
    /// </list>
    /// </para>
    /// <para>
    /// All UI updates are marshalled to the UI thread via <see cref="IDispatcher"/>.
    /// </para>
    /// </remarks>
    private void OnConversationChanged(object? sender, ConversationChangedEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnConversationChanged - ChangeType: {ChangeType}, ConversationId: {ConversationId}",
            e.ChangeType, e.Conversation.Id);

        // Marshal UI updates to the UI thread
        _dispatcher.InvokeAsync(() =>
        {
            switch (e.ChangeType)
            {
                case ConversationChangeType.Created:
                    _logger?.LogInformation("[INFO] New conversation created: {ConversationId}", e.Conversation.Id);
                    RefreshFromConversation();
                    break;

                case ConversationChangeType.Loaded:
                    _logger?.LogInformation("[INFO] Conversation loaded: {ConversationId} with {MessageCount} messages",
                        e.Conversation.Id, e.Conversation.Messages.Count);
                    RefreshFromConversation();
                    break;

                case ConversationChangeType.Cleared:
                    _logger?.LogInformation("[INFO] Conversation cleared: {ConversationId}", e.Conversation.Id);
                    RefreshFromConversation();
                    break;

                case ConversationChangeType.TitleChanged:
                    _logger?.LogDebug("[INFO] Conversation title changed to: {Title}", e.Conversation.Title);
                    ConversationTitle = e.Conversation.Title;
                    break;

                case ConversationChangeType.Saved:
                    _logger?.LogDebug("[INFO] Conversation saved: {ConversationId}", e.Conversation.Id);
                    SaveStatus = "Saved";
                    break;

                default:
                    _logger?.LogDebug("[INFO] Unhandled conversation change type: {ChangeType}", e.ChangeType);
                    break;
            }
        });

        _logger?.LogDebug("[EXIT] OnConversationChanged - {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Handles save state changes from the <see cref="IConversationService"/>.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments containing save state details.</param>
    /// <remarks>
    /// <para>
    /// Updates <see cref="HasUnsavedChanges"/> and <see cref="SaveStatus"/> properties
    /// based on the current save operation state.
    /// </para>
    /// <para>
    /// Save status transitions:
    /// <list type="bullet">
    /// <item>IsSaving=true → "Saving..."</item>
    /// <item>Error != null → "Save failed"</item>
    /// <item>HasUnsavedChanges=false → "Saved"</item>
    /// <item>Otherwise → null (no status displayed)</item>
    /// </list>
    /// </para>
    /// </remarks>
    private void OnSaveStateChanged(object? sender, SaveStateChangedEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnSaveStateChanged - IsSaving: {IsSaving}, HasUnsavedChanges: {HasUnsavedChanges}, HasError: {HasError}",
            e.IsSaving, e.HasUnsavedChanges, e.Error != null);

        // Marshal UI updates to the UI thread
        _dispatcher.InvokeAsync(() =>
        {
            // Update the unsaved changes flag
            HasUnsavedChanges = e.HasUnsavedChanges;

            // Determine save status text based on state
            if (e.IsSaving)
            {
                SaveStatus = "Saving...";
                _logger?.LogDebug("[INFO] Save operation in progress");
            }
            else if (e.Error != null)
            {
                SaveStatus = "Save failed";
                _logger?.LogWarning("[WARN] Save operation failed: {ErrorMessage}", e.Error);
            }
            else if (!e.HasUnsavedChanges)
            {
                SaveStatus = "Saved";
                _logger?.LogDebug("[INFO] All changes saved successfully");
            }
            else
            {
                // Has unsaved changes but not currently saving - clear status
                SaveStatus = null;
            }
        });

        _logger?.LogDebug("[EXIT] OnSaveStateChanged - {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Refreshes the UI state from the current conversation in the service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called when:
    /// <list type="bullet">
    /// <item>ViewModel is constructed (initial load)</item>
    /// <item>New conversation is created</item>
    /// <item>Existing conversation is loaded</item>
    /// <item>Conversation is cleared</item>
    /// </list>
    /// </para>
    /// <para>
    /// Clears existing messages and repopulates from the conversation service,
    /// ensuring UI state matches persisted state.
    /// </para>
    /// </remarks>
    private void RefreshFromConversation()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] RefreshFromConversation");

        // Clear existing message ViewModels
        Messages.Clear();

        // Get current conversation from service
        var conversation = _conversationService.CurrentConversation;

        // Update title from conversation
        ConversationTitle = conversation.Title;
        _logger?.LogDebug("[INFO] Loaded conversation title: {Title}", ConversationTitle);

        // Update unsaved changes state
        HasUnsavedChanges = _conversationService.HasUnsavedChanges;

        // Create ViewModels for each message in the conversation
        foreach (var message in conversation.Messages)
        {
            var messageVm = new ChatMessageViewModel(message);
            Messages.Add(messageVm);
        }

        _logger?.LogInformation("[INFO] Refreshed UI with {MessageCount} messages from conversation {ConversationId}",
            Messages.Count, conversation.Id);

        // Update command state (CanSend depends on model state, not message count)
        UpdateCanSend();

        _logger?.LogDebug("[EXIT] RefreshFromConversation - {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases resources used by this ViewModel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Performs the following cleanup:
    /// <list type="bullet">
    /// <item>Unsubscribes from <see cref="IConversationService"/> events</item>
    /// <item>Cancels any in-progress generation</item>
    /// <item>Disposes the cancellation token source</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method is safe to call multiple times; subsequent calls are ignored.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        // Guard against multiple disposal
        if (_isDisposed) return;

        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] ChatViewModel.Dispose");

        // Unsubscribe from conversation service events to prevent memory leaks
        _conversationService.ConversationChanged -= OnConversationChanged;
        _conversationService.SaveStateChanged -= OnSaveStateChanged;
        _logger?.LogDebug("[INFO] Unsubscribed from IConversationService events");

        // Cancel any in-progress generation
        if (_generationCts != null)
        {
            _logger?.LogDebug("[INFO] Cancelling in-progress generation");
            _generationCts.Cancel();
            _generationCts.Dispose();
            _generationCts = null;
        }

        _isDisposed = true;
        _logger?.LogDebug("[EXIT] ChatViewModel.Dispose - {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    #endregion
}
