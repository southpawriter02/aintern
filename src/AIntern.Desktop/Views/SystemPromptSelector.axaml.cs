using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Views;

/// <summary>
/// Code-behind for the SystemPromptSelector UserControl.
/// Provides a quick dropdown selector for choosing the active system prompt in the chat header.
/// </summary>
/// <remarks>
/// <para>
/// This control is designed to be embedded in the ChatView header, providing quick access
/// to system prompt selection without requiring the user to open the full editor window.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item>ComboBox dropdown with all available prompts including "No prompt" option</item>
///   <item>Default prompt indicator (star icon)</item>
///   <item>Category display for template prompts</item>
///   <item>Edit button that raises an event to open the editor window</item>
/// </list>
/// </para>
/// <para>
/// <b>Event Flow:</b>
/// The Edit button raises an <see cref="EditButtonClick"/> routed event that bubbles up
/// to the parent view, allowing the parent to handle opening the SystemPromptEditorWindow.
/// This design keeps the control decoupled from window management.
/// </para>
/// <para>
/// <b>Data Binding:</b>
/// Expects a <see cref="ViewModels.SystemPromptSelectorViewModel"/> as its DataContext.
/// All prompt selection logic is handled by the ViewModel.
/// </para>
/// <para>Added in v0.2.4e.</para>
/// </remarks>
public partial class SystemPromptSelector : UserControl
{
    #region Routed Events

    /// <summary>
    /// Identifies the <see cref="EditButtonClick"/> routed event.
    /// </summary>
    /// <remarks>
    /// This event uses the Bubble routing strategy so parent controls can handle it.
    /// </remarks>
    public static readonly RoutedEvent<RoutedEventArgs> EditButtonClickEvent =
        RoutedEvent.Register<SystemPromptSelector, RoutedEventArgs>(
            nameof(EditButtonClick),
            RoutingStrategies.Bubble);

    /// <summary>
    /// Occurs when the Edit button is clicked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event bubbles up to parent controls, allowing them to handle opening
    /// the SystemPromptEditorWindow. This design keeps the selector control focused
    /// solely on selection, while the parent (typically ChatView) handles navigation.
    /// </para>
    /// <para>
    /// Example handler in parent:
    /// <code>
    /// selector.EditButtonClick += (s, e) => OpenSystemPromptEditor();
    /// </code>
    /// </para>
    /// </remarks>
    public event EventHandler<RoutedEventArgs>? EditButtonClick
    {
        add => AddHandler(EditButtonClickEvent, value);
        remove => RemoveHandler(EditButtonClickEvent, value);
    }

    #endregion

    #region Fields

    /// <summary>
    /// Logger for exhaustive operation tracking.
    /// </summary>
    private readonly ILogger<SystemPromptSelector>? _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemPromptSelector"/> class.
    /// </summary>
    /// <remarks>
    /// Default constructor used by XAML instantiation.
    /// </remarks>
    public SystemPromptSelector() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemPromptSelector"/> class
    /// with optional logging support.
    /// </summary>
    /// <param name="logger">Optional logger for operation tracking.</param>
    /// <remarks>
    /// <para>
    /// This constructor allows dependency injection of a logger for detailed
    /// operation tracking during development and debugging.
    /// </para>
    /// </remarks>
    public SystemPromptSelector(ILogger<SystemPromptSelector>? logger)
    {
        var sw = Stopwatch.StartNew();
        _logger = logger;

        _logger?.LogDebug("[INIT] SystemPromptSelector construction started");

        InitializeComponent();

        // Wire up the Edit button click handler.
        // The button is defined in XAML with x:Name="EditButton".
        if (this.FindControl<Button>("EditButton") is { } editButton)
        {
            editButton.Click += OnEditButtonClick;
            _logger?.LogDebug("[INIT] EditButton click handler attached");
        }
        else
        {
            _logger?.LogWarning("[INIT] EditButton not found in template");
        }

        sw.Stop();
        _logger?.LogDebug("[INIT] SystemPromptSelector construction completed - {ElapsedMs}ms",
            sw.ElapsedMilliseconds);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the Edit button click by raising the <see cref="EditButtonClick"/> routed event.
    /// </summary>
    /// <param name="sender">The button that was clicked.</param>
    /// <param name="e">Event arguments.</param>
    /// <remarks>
    /// <para>
    /// This method raises the <see cref="EditButtonClickEvent"/> routed event, allowing
    /// parent controls to handle opening the SystemPromptEditorWindow.
    /// </para>
    /// <para>
    /// The event uses Bubble routing strategy, so it will propagate up through the
    /// visual tree until handled.
    /// </para>
    /// </remarks>
    private void OnEditButtonClick(object? sender, RoutedEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnEditButtonClick");

        try
        {
            // Raise the routed event to notify parent controls.
            var args = new RoutedEventArgs(EditButtonClickEvent, this);
            RaiseEvent(args);

            _logger?.LogDebug("[INFO] EditButtonClick event raised");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnEditButtonClick failed: {Message}", ex.Message);
        }
        finally
        {
            sw.Stop();
            _logger?.LogDebug("[EXIT] OnEditButtonClick - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    #endregion
}
