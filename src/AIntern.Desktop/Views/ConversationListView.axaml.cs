// ============================================================================
// ConversationListView.axaml.cs
// AIntern.Desktop - Conversation List UI (v0.2.2c)
// ============================================================================
// Code-behind for ConversationListView. Handles keyboard and pointer events
// that require imperative code rather than XAML bindings.
// ============================================================================

using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// Code-behind for the <see cref="ConversationListView"/> user control.
/// </summary>
/// <remarks>
/// <para>
/// This class handles events that require imperative code, specifically:
/// <list type="bullet">
///   <item>Pointer events for conversation selection</item>
///   <item>Keyboard events for search box and inline rename</item>
///   <item>Focus management for rename operations</item>
/// </list>
/// </para>
/// <para>
/// All event handlers follow the exhaustive logging pattern with [ENTER]/[EXIT]
/// markers and Stopwatch timing for performance monitoring.
/// </para>
/// <para>
/// The logger is optional (nullable) since code-behind classes don't have
/// constructor injection. Logging calls use the null-conditional operator.
/// </para>
/// </remarks>
public partial class ConversationListView : UserControl
{
    #region Fields

    /// <summary>
    /// Optional logger for diagnostic output.
    /// </summary>
    /// <remarks>
    /// May be null if logging is not configured. All logging calls should use
    /// the null-conditional operator (?.) to handle this case gracefully.
    /// </remarks>
    private ILogger<ConversationListView>? _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationListView"/> class.
    /// </summary>
    public ConversationListView()
    {
        InitializeComponent();
        _logger?.LogDebug("[INIT] ConversationListView created");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the logger instance for diagnostic output.
    /// </summary>
    /// <param name="logger">The logger instance to use, or null to disable logging.</param>
    /// <remarks>
    /// Call this method after construction if logging is desired.
    /// Typically called from the view's initialization or DI setup.
    /// </remarks>
    public void SetLogger(ILogger<ConversationListView>? logger)
    {
        _logger = logger;
        _logger?.LogDebug("[INIT] ConversationListView logger configured");
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles pointer pressed events on conversation items.
    /// </summary>
    /// <param name="sender">The source Border element.</param>
    /// <param name="e">Pointer event arguments.</param>
    /// <remarks>
    /// <para>
    /// Executes <see cref="ConversationListViewModel.SelectConversationCommand"/>
    /// when the user left-clicks on a conversation item.
    /// </para>
    /// <para>
    /// Right-click is handled by the context menu and is not processed here.
    /// </para>
    /// </remarks>
    private void OnConversationPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnConversationPointerPressed");

        try
        {
            // Only handle left button clicks
            var point = e.GetCurrentPoint(sender as Control);
            if (point.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            {
                _logger?.LogDebug("[SKIP] OnConversationPointerPressed - not left button");
                return;
            }

            // Get the conversation summary from the Border's DataContext
            if (sender is not Border { DataContext: ConversationSummaryViewModel summary })
            {
                _logger?.LogDebug("[SKIP] OnConversationPointerPressed - invalid sender or DataContext");
                return;
            }

            // Get the parent ViewModel to execute the command
            if (DataContext is not ConversationListViewModel viewModel)
            {
                _logger?.LogWarning("[WARN] OnConversationPointerPressed - parent DataContext is not ConversationListViewModel");
                return;
            }

            // Execute the select command
            _logger?.LogDebug("[INFO] Selecting conversation: {Id} - {Title}", summary.Id, summary.Title);

            if (viewModel.SelectConversationCommand.CanExecute(summary))
            {
                viewModel.SelectConversationCommand.Execute(summary);
                _logger?.LogInformation("User selected conversation: {Id}", summary.Id);
            }

            // Mark the event as handled to prevent bubbling
            e.Handled = true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnConversationPointerPressed failed");
        }
        finally
        {
            _logger?.LogDebug("[EXIT] OnConversationPointerPressed - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Handles key down events in the rename TextBox.
    /// </summary>
    /// <param name="sender">The source TextBox element.</param>
    /// <param name="e">Key event arguments.</param>
    /// <remarks>
    /// <para>
    /// Key handling:
    /// <list type="bullet">
    ///   <item><b>Enter</b>: Confirms the rename operation</item>
    ///   <item><b>Escape</b>: Cancels the rename operation</item>
    /// </list>
    /// </para>
    /// </remarks>
    private void OnRenameKeyDown(object? sender, KeyEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnRenameKeyDown - Key: {Key}", e.Key);

        try
        {
            if (sender is not TextBox { DataContext: ConversationSummaryViewModel summary })
            {
                _logger?.LogDebug("[SKIP] OnRenameKeyDown - invalid sender or DataContext");
                return;
            }

            if (DataContext is not ConversationListViewModel viewModel)
            {
                _logger?.LogWarning("[WARN] OnRenameKeyDown - parent DataContext is not ConversationListViewModel");
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                    _logger?.LogDebug("[INFO] Enter pressed - confirming rename for: {Id}", summary.Id);
                    if (viewModel.ConfirmRenameCommand.CanExecute(summary))
                    {
                        viewModel.ConfirmRenameCommand.Execute(summary);
                        _logger?.LogInformation("User confirmed rename for conversation: {Id}", summary.Id);
                    }
                    e.Handled = true;
                    break;

                case Key.Escape:
                    _logger?.LogDebug("[INFO] Escape pressed - cancelling rename for: {Id}", summary.Id);
                    if (viewModel.CancelRenameCommand.CanExecute(summary))
                    {
                        viewModel.CancelRenameCommand.Execute(summary);
                        _logger?.LogInformation("User cancelled rename for conversation: {Id}", summary.Id);
                    }
                    e.Handled = true;
                    break;

                default:
                    _logger?.LogDebug("[SKIP] OnRenameKeyDown - unhandled key: {Key}", e.Key);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnRenameKeyDown failed");
        }
        finally
        {
            _logger?.LogDebug("[EXIT] OnRenameKeyDown - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Handles lost focus events on the rename TextBox.
    /// </summary>
    /// <param name="sender">The source TextBox element.</param>
    /// <param name="e">Routed event arguments.</param>
    /// <remarks>
    /// <para>
    /// When focus leaves the rename TextBox (e.g., clicking elsewhere),
    /// the rename operation is automatically confirmed. This provides
    /// intuitive behavior matching common application patterns.
    /// </para>
    /// <para>
    /// Note: If the user presses Escape, <see cref="OnRenameKeyDown"/> handles
    /// the cancellation before this event fires.
    /// </para>
    /// </remarks>
    private void OnRenameLostFocus(object? sender, RoutedEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnRenameLostFocus");

        try
        {
            if (sender is not TextBox { DataContext: ConversationSummaryViewModel summary })
            {
                _logger?.LogDebug("[SKIP] OnRenameLostFocus - invalid sender or DataContext");
                return;
            }

            // Only process if still in rename mode (wasn't cancelled by Escape key)
            if (!summary.IsRenaming)
            {
                _logger?.LogDebug("[SKIP] OnRenameLostFocus - not in rename mode");
                return;
            }

            if (DataContext is not ConversationListViewModel viewModel)
            {
                _logger?.LogWarning("[WARN] OnRenameLostFocus - parent DataContext is not ConversationListViewModel");
                return;
            }

            _logger?.LogDebug("[INFO] Focus lost - auto-confirming rename for: {Id}", summary.Id);

            if (viewModel.ConfirmRenameCommand.CanExecute(summary))
            {
                viewModel.ConfirmRenameCommand.Execute(summary);
                _logger?.LogInformation("Auto-confirmed rename on focus loss for conversation: {Id}", summary.Id);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnRenameLostFocus failed");
        }
        finally
        {
            _logger?.LogDebug("[EXIT] OnRenameLostFocus - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Handles key down events in the search TextBox.
    /// </summary>
    /// <param name="sender">The source TextBox element.</param>
    /// <param name="e">Key event arguments.</param>
    /// <remarks>
    /// <para>
    /// Key handling:
    /// <list type="bullet">
    ///   <item><b>Escape</b>: Clears the search query</item>
    /// </list>
    /// </para>
    /// </remarks>
    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnSearchKeyDown - Key: {Key}", e.Key);

        try
        {
            if (e.Key == Key.Escape)
            {
                if (DataContext is ConversationListViewModel viewModel)
                {
                    _logger?.LogDebug("[INFO] Escape pressed - clearing search");

                    if (viewModel.ClearSearchCommand.CanExecute(null))
                    {
                        viewModel.ClearSearchCommand.Execute(null);
                        _logger?.LogInformation("User cleared search with Escape key");
                    }

                    e.Handled = true;
                }
            }
            else
            {
                _logger?.LogDebug("[SKIP] OnSearchKeyDown - unhandled key: {Key}", e.Key);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnSearchKeyDown failed");
        }
        finally
        {
            _logger?.LogDebug("[EXIT] OnSearchKeyDown - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    #endregion
}
