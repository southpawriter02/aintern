// -----------------------------------------------------------------------
// <copyright file="SearchDialog.axaml.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Code-behind for the SearchDialog providing lifecycle management
//     and search input focus handling.
//     Added in v0.2.5e.
// </summary>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// Code-behind for the SearchDialog providing lifecycle management,
/// search input focus, and dialog result handling.
/// </summary>
/// <remarks>
/// <para>
/// This dialog hosts the <see cref="SearchViewModel"/> and provides:
/// </para>
/// <list type="bullet">
///   <item><description><b>Auto-Focus:</b> Focuses search input when opened</description></item>
///   <item><description><b>Dialog Result:</b> Returns selected <see cref="SearchResult"/> on close</description></item>
///   <item><description><b>ViewModel Sync:</b> Monitors <see cref="SearchViewModel.ShouldClose"/> to close</description></item>
///   <item><description><b>Disposal:</b> Disposes ViewModel when closed</description></item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// </para>
/// <code>
/// var dialog = new SearchDialog { DataContext = viewModel };
/// var result = await dialog.ShowDialog&lt;SearchResult?&gt;(owner);
/// if (result != null) { /* Navigate to result */ }
/// </code>
/// <para>Added in v0.2.5e.</para>
/// </remarks>
/// <seealso cref="SearchViewModel"/>
public partial class SearchDialog : Window, IDisposable
{
    #region Fields

    /// <summary>
    /// Optional logger for diagnostic output.
    /// </summary>
    private readonly ILogger<SearchDialog>? _logger;

    /// <summary>
    /// Flag indicating whether the window has been disposed.
    /// </summary>
    private bool _isDisposed;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the typed ViewModel from DataContext.
    /// </summary>
    /// <value>
    /// The <see cref="SearchViewModel"/> bound to this dialog, or null if not set.
    /// </value>
    private SearchViewModel? ViewModel => DataContext as SearchViewModel;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchDialog"/> class.
    /// </summary>
    /// <remarks>
    /// Parameterless constructor for XAML designer and standard usage.
    /// </remarks>
    public SearchDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchDialog"/> class with logging.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <remarks>
    /// Use this constructor when explicit logging is needed for debugging.
    /// </remarks>
    public SearchDialog(ILogger<SearchDialog>? logger)
    {
        _logger = logger;
        _logger?.LogDebug("[INIT] SearchDialog constructor called");

        InitializeComponent();

        _logger?.LogDebug("[INIT] SearchDialog InitializeComponent completed");
    }

    #endregion

    #region Window Lifecycle

    /// <summary>
    /// Called when the window is opened and visible.
    /// Focuses the search input and subscribes to ViewModel changes.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    /// <remarks>
    /// <para>
    /// This handler:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Focuses the search TextBox for immediate typing</description></item>
    ///   <item><description>Subscribes to <see cref="SearchViewModel.ShouldClose"/> property changes</description></item>
    /// </list>
    /// </remarks>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnOpened");

        try
        {
            // Focus the search input for immediate typing.
            var searchInput = this.FindControl<TextBox>("SearchInput");
            if (searchInput != null)
            {
                searchInput.Focus();
                _logger?.LogDebug("[INFO] Search input focused");
            }
            else
            {
                _logger?.LogWarning("[WARN] SearchInput control not found");
            }

            // Subscribe to ViewModel property changes to detect ShouldClose.
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                _logger?.LogDebug("[INFO] Subscribed to ViewModel.PropertyChanged");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnOpened failed: {Message}", ex.Message);
        }
        finally
        {
            sw.Stop();
            _logger?.LogDebug("[EXIT] OnOpened - Duration: {Ms}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Called when the window is about to close.
    /// Unsubscribes from ViewModel events and sets dialog result.
    /// </summary>
    /// <param name="e">The closing event arguments.</param>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnClosing");

        try
        {
            // Unsubscribe from ViewModel events.
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _logger?.LogDebug("[INFO] Unsubscribed from ViewModel.PropertyChanged");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnClosing handler failed: {Message}", ex.Message);
        }
        finally
        {
            sw.Stop();
            _logger?.LogDebug("[EXIT] OnClosing - Duration: {Ms}ms", sw.ElapsedMilliseconds);
        }

        base.OnClosing(e);
    }

    #endregion

    #region ViewModel Event Handling

    /// <summary>
    /// Handles ViewModel property changed events.
    /// Closes the dialog when <see cref="SearchViewModel.ShouldClose"/> becomes true.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Property changed event arguments.</param>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchViewModel.ShouldClose) && ViewModel?.ShouldClose == true)
        {
            _logger?.LogDebug("[INFO] ShouldClose is true, closing dialog with result");

            // Close with the dialog result.
            Close(ViewModel.DialogResult);
        }
    }

    #endregion

    #region Keyboard Handling

    /// <summary>
    /// Handles key down events for fallback keyboard shortcuts.
    /// </summary>
    /// <param name="e">The key event arguments.</param>
    /// <remarks>
    /// <para>
    /// Most keyboard shortcuts are handled via KeyBindings in XAML.
    /// This handler provides fallback for edge cases.
    /// </para>
    /// </remarks>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        _logger?.LogDebug("[INFO] OnKeyDown - Key: {Key}", e.Key);

        // Escape is already bound via KeyBindings, but provide fallback.
        if (e.Key == Key.Escape && !e.Handled)
        {
            _logger?.LogDebug("[INFO] Escape pressed - closing via fallback");
            Close(null);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the dialog and its ViewModel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Disposes the <see cref="SearchViewModel"/> to clean up the debounce timer
    /// and cancellation token source.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _logger?.LogDebug("[DISPOSE] SearchDialog - Disposing");

        // Unsubscribe from ViewModel events.
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        // Dispose the ViewModel to clean up timers.
        if (ViewModel is IDisposable disposableViewModel)
        {
            disposableViewModel.Dispose();
        }

        _isDisposed = true;

        _logger?.LogDebug("[DISPOSE] SearchDialog - Disposal complete");
    }

    #endregion
}
