using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.Logging;
using AIntern.Desktop.Dialogs;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// The main application window.
/// Hosts the chat panel, model selector, conversation list, and status bar.
/// </summary>
/// <remarks>
/// <para>
/// Layout structure:
/// <list type="bullet">
/// <item>SplitView with sidebar (left) containing ModelSelector and ConversationList</item>
/// <item>Main content area (right) containing ChatView</item>
/// <item>Status bar at bottom with model info and token statistics</item>
/// </list>
/// </para>
/// <para>
/// <b>Initialization Flow:</b>
/// <list type="number">
/// <item>Constructor: InitializeComponent() called</item>
/// <item>OnOpened: ViewModel.InitializeAsync() called to load data</item>
/// </list>
/// </para>
/// <para>
/// <b>Keyboard Shortcuts (defined in XAML):</b>
/// <list type="bullet">
/// <item>Ctrl+N - Create new conversation</item>
/// <item>Ctrl+S - Save current conversation</item>
/// <item>Ctrl+B - Toggle sidebar visibility</item>
/// <item>Ctrl+F - Focus search box</item>
/// </list>
/// </para>
/// <para>
/// <b>Keyboard Shortcuts (defined in code-behind):</b>
/// <list type="bullet">
/// <item>F2 - Rename selected conversation</item>
/// </list>
/// </para>
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>
    /// Optional logger instance for diagnostics.
    /// Injected via constructor when using DI container.
    /// </summary>
    private readonly ILogger<MainWindow>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <remarks>
    /// Parameterless constructor used by XAML designer and DI container.
    /// </remarks>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class with logging.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <remarks>
    /// Use this constructor when explicit logging is needed during development.
    /// </remarks>
    public MainWindow(ILogger<MainWindow>? logger)
    {
        _logger = logger;
        _logger?.LogDebug("[INIT] MainWindow constructor called");

        InitializeComponent();

        _logger?.LogDebug("[INIT] MainWindow InitializeComponent completed");
    }

    /// <summary>
    /// Called when the window is opened and visible.
    /// Initiates async initialization of the ViewModel.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    /// <remarks>
    /// <para>
    /// This override performs post-construction initialization that requires
    /// async operations, such as loading conversations from the database.
    /// </para>
    /// <para>
    /// The async void pattern is acceptable here because:
    /// <list type="bullet">
    /// <item>OnOpened is an event handler (void return required)</item>
    /// <item>Errors are caught and logged rather than propagated</item>
    /// <item>The window is already visible - failures degrade gracefully</item>
    /// </list>
    /// </para>
    /// </remarks>
    protected override async void OnOpened(EventArgs e)
    {
        // Call base implementation first (standard pattern)
        base.OnOpened(e);

        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] MainWindow.OnOpened");

        try
        {
            // Get the ViewModel from DataContext (set by DI container)
            if (DataContext is MainWindowViewModel viewModel)
            {
                _logger?.LogDebug("[INFO] Initializing MainWindowViewModel");

                // Set up owner window provider for dialogs (v0.2.2e)
                viewModel.ConversationListViewModel.SetOwnerWindowProvider(() => this);
                _logger?.LogDebug("[INFO] Owner window provider set for ConversationListViewModel");

                // Perform async initialization (loads settings and conversations)
                await viewModel.InitializeAsync();

                _logger?.LogInformation("[INFO] MainWindow initialized successfully");
            }
            else
            {
                _logger?.LogWarning("[WARN] DataContext is not MainWindowViewModel, skipping initialization");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash - window is already visible
            _logger?.LogError(ex, "[ERROR] MainWindow initialization failed");

            // Optionally show error to user (could use notification service in future)
            // For now, errors are displayed in the status bar via SetError
        }
        finally
        {
            _logger?.LogDebug("[EXIT] MainWindow.OnOpened - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    #region Window Close Handler (v0.2.2e)

    /// <summary>
    /// Called when the window is about to close.
    /// Checks for unsaved changes and prompts the user if necessary.
    /// </summary>
    /// <param name="e">The closing event arguments.</param>
    /// <remarks>
    /// <para>
    /// If there are unsaved changes in the current conversation, this handler:
    /// <list type="bullet">
    /// <item>Cancels the close operation</item>
    /// <item>Shows the UnsavedChangesDialog</item>
    /// <item>Handles Save/DontSave/Cancel based on user choice</item>
    /// </list>
    /// </para>
    /// <para>
    /// The async void pattern is acceptable here because OnClosing is an event
    /// handler with a void return type requirement.
    /// </para>
    /// </remarks>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] MainWindow.OnClosing");

        try
        {
            // Check if there are unsaved changes
            if (DataContext is MainWindowViewModel viewModel &&
                viewModel.ChatViewModel.HasUnsavedChanges)
            {
                _logger?.LogDebug("[INFO] Unsaved changes detected, showing dialog");

                // Cancel the close to show dialog
                e.Cancel = true;

                // Get conversation title for dialog
                var conversationTitle = viewModel.ChatViewModel.ConversationTitle
                    ?? "Untitled Conversation";

                // Show unsaved changes dialog
                var result = await UnsavedChangesDialog.ShowAsync(
                    this,
                    conversationTitle,
                    _logger);

                switch (result)
                {
                    case UnsavedChangesDialog.Result.Save:
                        _logger?.LogDebug("[INFO] User chose Save, saving conversation");
                        await viewModel.ChatViewModel.SaveCommand.ExecuteAsync(null);
                        // Close after saving
                        _logger?.LogDebug("[INFO] Save completed, closing window");
                        Close();
                        break;

                    case UnsavedChangesDialog.Result.DontSave:
                        _logger?.LogDebug("[INFO] User chose Don't Save, closing without saving");
                        // Mark as no longer having unsaved changes to allow close
                        viewModel.ChatViewModel.ClearUnsavedChangesFlag();
                        Close();
                        break;

                    case UnsavedChangesDialog.Result.Cancel:
                        _logger?.LogDebug("[INFO] User chose Cancel, aborting close");
                        // Do nothing - close was already cancelled
                        break;
                }
            }
            else
            {
                _logger?.LogDebug("[SKIP] No unsaved changes, allowing close");
                base.OnClosing(e);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnClosing handler failed: {Message}", ex.Message);
            // Allow close on error to prevent user from being stuck
            base.OnClosing(e);
        }
        finally
        {
            _logger?.LogDebug("[EXIT] MainWindow.OnClosing - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    #endregion

    #region Keyboard Handler (v0.2.2e)

    /// <summary>
    /// Handles key down events for shortcuts not defined in XAML.
    /// </summary>
    /// <param name="e">The key event arguments.</param>
    /// <remarks>
    /// <para>
    /// This handler provides F2 support for renaming conversations.
    /// Other shortcuts (Ctrl+N, Ctrl+S, Ctrl+B, Ctrl+F) are defined in XAML.
    /// </para>
    /// <para>
    /// F2 is handled in code-behind because it requires access to the
    /// ConversationListViewModel to invoke BeginRename on the selected item.
    /// </para>
    /// </remarks>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        _logger?.LogDebug("[INFO] MainWindow.OnKeyDown - Key: {Key}", e.Key);

        try
        {
            // F2: Begin rename of selected conversation
            if (e.Key == Key.F2)
            {
                _logger?.LogDebug("[INFO] F2 pressed, attempting to begin rename");

                if (DataContext is MainWindowViewModel viewModel)
                {
                    var selectedConversation = viewModel.ConversationListViewModel.SelectedConversation;
                    if (selectedConversation != null)
                    {
                        _logger?.LogDebug(
                            "[INFO] Beginning rename for conversation: {ConversationId}",
                            selectedConversation.Id);

                        viewModel.ConversationListViewModel.RenameConversationCommand.Execute(selectedConversation);
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        _logger?.LogDebug("[SKIP] No conversation selected for F2 rename");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnKeyDown handler failed: {Message}", ex.Message);
        }

        base.OnKeyDown(e);
    }

    #endregion
}
