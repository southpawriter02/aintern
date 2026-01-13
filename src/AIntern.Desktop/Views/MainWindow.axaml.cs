using System.Diagnostics;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
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
/// <item>Ctrl+B - Toggle sidebar visibility</item>
/// <item>Ctrl+F - Focus search box</item>
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
}
