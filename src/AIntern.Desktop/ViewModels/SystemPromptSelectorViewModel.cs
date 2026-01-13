using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the system prompt quick selector in the chat header.
/// Provides a dropdown for selecting the active system prompt for new conversations.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel is used in the chat header to allow quick selection of system prompts
/// without opening the full editor. It maintains synchronization with <see cref="ISystemPromptService"/>
/// through event subscriptions and provides a simplified interface focused on selection.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item>Displays all available prompts including a "No prompt" option</item>
///   <item>Synchronizes selection with the service's CurrentPrompt</item>
///   <item>Auto-refreshes when the prompt list changes</item>
///   <item>Provides display text for the current selection</item>
/// </list>
/// </para>
/// <para>
/// <b>Event Subscriptions:</b>
/// <list type="bullet">
///   <item><see cref="ISystemPromptService.PromptListChanged"/> - Refreshes available prompts</item>
///   <item><see cref="ISystemPromptService.CurrentPromptChanged"/> - Syncs selection state</item>
/// </list>
/// </para>
/// <para>
/// <b>Lifetime:</b> Registered as singleton to share selection state across the application.
/// </para>
/// <para>Added in v0.2.4c.</para>
/// </remarks>
public sealed partial class SystemPromptSelectorViewModel : ViewModelBase, IDisposable
{
    #region Constants

    /// <summary>
    /// Display name for the "No prompt" option.
    /// </summary>
    private const string NoPromptDisplayName = "No system prompt";

    /// <summary>
    /// Description for the "No prompt" option.
    /// </summary>
    private const string NoPromptDescription = "Send messages without a system prompt";

    #endregion

    #region Fields

    /// <summary>
    /// The system prompt service for CRUD operations and state management.
    /// </summary>
    private readonly ISystemPromptService _promptService;

    /// <summary>
    /// The dispatcher for marshaling operations to the UI thread.
    /// </summary>
    private readonly IDispatcher _dispatcher;

    /// <summary>
    /// Logger for exhaustive operation tracking.
    /// </summary>
    private readonly ILogger<SystemPromptSelectorViewModel> _logger;

    /// <summary>
    /// Tracks whether the ViewModel has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Tracks whether initialization has completed.
    /// </summary>
    private bool _isInitialized;

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets the collection of available system prompts for selection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This collection includes all active prompts plus a special "No prompt" option
    /// at the beginning. The "No prompt" option has <see cref="Guid.Empty"/> as its Id.
    /// </para>
    /// <para>
    /// The collection is ordered with "No prompt" first, followed by user prompts,
    /// then templates.
    /// </para>
    /// </remarks>
    public ObservableCollection<SystemPromptViewModel> AvailablePrompts { get; } = [];

    /// <summary>
    /// Gets or sets the currently selected prompt in the dropdown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set, this triggers <see cref="SelectPromptCommand"/> to update the service.
    /// Setting to null or the "No prompt" option clears the current prompt.
    /// </para>
    /// <para>
    /// This property is synchronized with <see cref="ISystemPromptService.CurrentPrompt"/>
    /// through the <see cref="OnCurrentPromptChanged"/> event handler.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private SystemPromptViewModel? _selectedPrompt;

    /// <summary>
    /// Gets or sets whether the selector is currently loading data.
    /// </summary>
    /// <remarks>
    /// Used to show loading indicators in the UI during async operations.
    /// </remarks>
    [ObservableProperty]
    private bool _isLoading;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether a prompt is currently selected (not null and not the "No prompt" option).
    /// </summary>
    /// <remarks>
    /// Returns <c>true</c> if <see cref="SelectedPrompt"/> is set and has a non-empty Id.
    /// Used for conditional UI display.
    /// </remarks>
    public bool HasPromptSelected => SelectedPrompt != null && SelectedPrompt.Id != Guid.Empty;

    /// <summary>
    /// Gets the display text for the current selection.
    /// </summary>
    /// <remarks>
    /// Returns the selected prompt's name, or "No system prompt" if nothing is selected
    /// or the "No prompt" option is selected.
    /// </remarks>
    public string DisplayText => HasPromptSelected ? SelectedPrompt!.Name : NoPromptDisplayName;

    /// <summary>
    /// Gets the content preview for the current selection.
    /// </summary>
    /// <remarks>
    /// Returns the selected prompt's content preview, or an empty string if nothing
    /// is selected.
    /// </remarks>
    public string ContentPreview => SelectedPrompt?.ContentPreview ?? string.Empty;

    /// <summary>
    /// Gets the category of the current selection.
    /// </summary>
    /// <remarks>
    /// Returns the selected prompt's category, or an empty string if nothing is selected.
    /// </remarks>
    public string SelectedCategory => SelectedPrompt?.Category ?? string.Empty;

    /// <summary>
    /// Gets whether the selected prompt is a built-in template.
    /// </summary>
    /// <remarks>
    /// Returns <c>true</c> if the selected prompt is a built-in template.
    /// </remarks>
    public bool IsBuiltInSelected => SelectedPrompt?.IsBuiltIn ?? false;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemPromptSelectorViewModel"/> class.
    /// </summary>
    /// <param name="promptService">The system prompt service for operations.</param>
    /// <param name="dispatcher">The dispatcher for UI thread marshaling.</param>
    /// <param name="logger">The logger for operation tracking.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="promptService"/>, <paramref name="dispatcher"/>,
    /// or <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor subscribes to service events for automatic synchronization.
    /// Call <see cref="InitializeAsync"/> after construction to load initial data.
    /// </para>
    /// <para>
    /// Event subscriptions are cleaned up in <see cref="Dispose"/>.
    /// </para>
    /// </remarks>
    public SystemPromptSelectorViewModel(
        ISystemPromptService promptService,
        IDispatcher dispatcher,
        ILogger<SystemPromptSelectorViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(promptService);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(logger);

        _promptService = promptService;
        _dispatcher = dispatcher;
        _logger = logger;

        // Subscribe to service events for automatic synchronization.
        _promptService.PromptListChanged += OnPromptListChanged;
        _promptService.CurrentPromptChanged += OnCurrentPromptChanged;

        _logger.LogDebug("[INIT] SystemPromptSelectorViewModel created");
    }

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Initializes the ViewModel by loading available prompts and syncing selection.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// This method should be called after the ViewModel is constructed and before
    /// it is used by the UI. It loads the prompt list and synchronizes the selection
    /// with the service's current prompt.
    /// </para>
    /// <para>
    /// This method is idempotent - calling it multiple times has no additional effect.
    /// </para>
    /// </remarks>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            _logger.LogDebug("[SKIP] InitializeAsync - Already initialized");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] InitializeAsync");

        try
        {
            IsLoading = true;

            // Load available prompts.
            await LoadPromptsAsync();

            // Sync selection with service current prompt.
            SyncSelectionWithService();

            _isInitialized = true;

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] InitializeAsync - Loaded {Count} prompts, Duration: {Ms}ms",
                AvailablePrompts.Count,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EXIT] InitializeAsync - Failed");
            SetError($"Failed to load prompts: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Selects a prompt and updates the service's current prompt.
    /// </summary>
    /// <param name="prompt">The prompt to select, or null to clear selection.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// This command is called when the user selects a prompt from the dropdown.
    /// It updates the service's current prompt, which persists the selection.
    /// </para>
    /// <para>
    /// Selecting the "No prompt" option (Id = <see cref="Guid.Empty"/>) clears the
    /// current prompt by passing null to the service.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task SelectPromptAsync(SystemPromptViewModel? prompt)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] SelectPromptAsync - PromptId: {Id}, PromptName: {Name}",
            prompt?.Id,
            prompt?.Name ?? "(null)");

        try
        {
            ClearError();

            // Determine the ID to set (null for "No prompt" option or null selection).
            Guid? promptId = (prompt == null || prompt.Id == Guid.Empty) ? null : prompt.Id;

            // Update service - this persists the selection and fires CurrentPromptChanged.
            await _promptService.SetCurrentPromptAsync(promptId);

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] SelectPromptAsync - Set CurrentPrompt to: {Id}, Duration: {Ms}ms",
                promptId,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EXIT] SelectPromptAsync - Failed");
            SetError($"Failed to select prompt: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes the list of available prompts from the service.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// This command can be used to manually refresh the prompt list.
    /// Normally, the list auto-refreshes through the <see cref="OnPromptListChanged"/> handler.
    /// </remarks>
    [RelayCommand]
    private async Task RefreshPromptsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] RefreshPromptsAsync");

        try
        {
            IsLoading = true;
            ClearError();

            await LoadPromptsAsync();
            SyncSelectionWithService();

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] RefreshPromptsAsync - Refreshed {Count} prompts, Duration: {Ms}ms",
                AvailablePrompts.Count,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EXIT] RefreshPromptsAsync - Failed");
            SetError($"Failed to refresh prompts: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads all available prompts from the service and populates the collection.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// This method clears and repopulates <see cref="AvailablePrompts"/> with:
    /// <list type="number">
    ///   <item>A "No prompt" option at the beginning</item>
    ///   <item>All active prompts from the service</item>
    /// </list>
    /// </para>
    /// <para>
    /// All collection modifications are marshaled to the UI thread via <see cref="IDispatcher"/>.
    /// </para>
    /// </remarks>
    private async Task LoadPromptsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] LoadPromptsAsync");

        try
        {
            // Get all prompts from service.
            var allPrompts = await _promptService.GetAllPromptsAsync();

            await _dispatcher.InvokeAsync(() =>
            {
                AvailablePrompts.Clear();

                // Add "No prompt" option first.
                AvailablePrompts.Add(new SystemPromptViewModel
                {
                    Id = Guid.Empty,
                    Name = NoPromptDisplayName,
                    Content = string.Empty,
                    Description = NoPromptDescription,
                    Category = string.Empty,
                    IsBuiltIn = false,
                    IsDefault = false
                });

                // Add all active prompts.
                foreach (var prompt in allPrompts)
                {
                    AvailablePrompts.Add(new SystemPromptViewModel(prompt));
                }
            });

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] LoadPromptsAsync - Loaded {Count} prompts (including 'No prompt'), Duration: {Ms}ms",
                AvailablePrompts.Count,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EXIT] LoadPromptsAsync - Failed");
            throw;
        }
    }

    /// <summary>
    /// Synchronizes the <see cref="SelectedPrompt"/> with the service's current prompt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Finds the matching ViewModel in <see cref="AvailablePrompts"/> based on the
    /// service's <see cref="ISystemPromptService.CurrentPrompt"/> and sets it as selected.
    /// </para>
    /// <para>
    /// If no current prompt is set in the service, selects the "No prompt" option.
    /// </para>
    /// </remarks>
    private void SyncSelectionWithService()
    {
        _logger.LogDebug("[ENTER] SyncSelectionWithService");

        var currentPrompt = _promptService.CurrentPrompt;

        if (currentPrompt == null)
        {
            // No current prompt - select "No prompt" option.
            SelectedPrompt = AvailablePrompts.FirstOrDefault(p => p.Id == Guid.Empty);
            _logger.LogDebug("[EXIT] SyncSelectionWithService - Selected 'No prompt' option");
        }
        else
        {
            // Find matching ViewModel.
            var match = AvailablePrompts.FirstOrDefault(p => p.Id == currentPrompt.Id);

            if (match != null)
            {
                SelectedPrompt = match;
                _logger.LogDebug(
                    "[EXIT] SyncSelectionWithService - Selected prompt: {Name}",
                    match.Name);
            }
            else
            {
                // Current prompt not in list (possibly deleted) - select "No prompt".
                SelectedPrompt = AvailablePrompts.FirstOrDefault(p => p.Id == Guid.Empty);
                _logger.LogDebug(
                    "[EXIT] SyncSelectionWithService - Current prompt {Id} not found, selected 'No prompt'",
                    currentPrompt.Id);
            }
        }
    }

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Called when <see cref="SelectedPrompt"/> changes.
    /// </summary>
    /// <param name="value">The new selected prompt.</param>
    /// <remarks>
    /// Notifies computed properties that depend on the selection.
    /// </remarks>
    partial void OnSelectedPromptChanged(SystemPromptViewModel? value)
    {
        _logger.LogDebug(
            "[INFO] SelectedPrompt changed to: {Name} (Id: {Id})",
            value?.Name ?? "(null)",
            value?.Id);

        // Notify computed properties.
        OnPropertyChanged(nameof(HasPromptSelected));
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(ContentPreview));
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(IsBuiltInSelected));
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the <see cref="ISystemPromptService.PromptListChanged"/> event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments containing change details.</param>
    /// <remarks>
    /// <para>
    /// Refreshes the prompt list when any mutation occurs (create, update, delete, etc.).
    /// </para>
    /// <para>
    /// This handler is async void because it's an event handler. Exceptions are
    /// logged but not propagated.
    /// </para>
    /// </remarks>
    private async void OnPromptListChanged(object? sender, PromptListChangedEventArgs e)
    {
        _logger.LogDebug(
            "[EVENT] PromptListChanged - Type: {Type}, AffectedId: {Id}",
            e.ChangeType,
            e.AffectedPromptId);

        try
        {
            await LoadPromptsAsync();
            SyncSelectionWithService();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT] Failed to handle PromptListChanged");
        }
    }

    /// <summary>
    /// Handles the <see cref="ISystemPromptService.CurrentPromptChanged"/> event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments containing old and new prompts.</param>
    /// <remarks>
    /// <para>
    /// Synchronizes the selection when the current prompt changes externally
    /// (e.g., from the editor or another ViewModel).
    /// </para>
    /// <para>
    /// This handler runs on the service's thread, so selection updates are
    /// performed inline (the property setter handles UI thread marshaling if needed).
    /// </para>
    /// </remarks>
    private void OnCurrentPromptChanged(object? sender, CurrentPromptChangedEventArgs e)
    {
        _logger.LogDebug(
            "[EVENT] CurrentPromptChanged - NewPrompt: {NewName}, PreviousPrompt: {PrevName}",
            e.NewPrompt?.Name ?? "(null)",
            e.PreviousPrompt?.Name ?? "(null)");

        // Sync selection on UI thread.
        _dispatcher.InvokeAsync(() => SyncSelectionWithService());
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases resources and unsubscribes from events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unsubscribes from <see cref="ISystemPromptService"/> events to prevent memory leaks.
    /// </para>
    /// <para>
    /// This method is safe to call multiple times.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("[DISPOSE] SystemPromptSelectorViewModel - Unsubscribing from events");

        // Unsubscribe from service events.
        _promptService.PromptListChanged -= OnPromptListChanged;
        _promptService.CurrentPromptChanged -= OnCurrentPromptChanged;

        _disposed = true;

        _logger.LogDebug("[DISPOSE] SystemPromptSelectorViewModel - Disposed");
    }

    #endregion
}
