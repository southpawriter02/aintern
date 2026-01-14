// -----------------------------------------------------------------------
// <copyright file="ExportDialog.axaml.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Code-behind for the ExportDialog window.
//     Added in v0.2.5f.
// </summary>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;
using Avalonia.Controls;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// Code-behind for the export dialog window.
/// </summary>
/// <remarks>
/// <para>
/// This class manages the dialog lifecycle:
/// </para>
/// <list type="bullet">
///   <item><description><b>OnOpened:</b> Subscribes to PropertyChanged, calls InitializeAsync</description></item>
///   <item><description><b>PropertyChanged:</b> Monitors ShouldClose to close dialog</description></item>
///   <item><description><b>OnClosed:</b> Unsubscribes from events, disposes ViewModel</description></item>
/// </list>
/// <para>Added in v0.2.5f.</para>
/// </remarks>
public partial class ExportDialog : Window, IDisposable
{
    #region Fields

    private bool _disposed;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportDialog"/> class.
    /// </summary>
    public ExportDialog()
    {
        InitializeComponent();
    }

    #endregion

    #region Window Lifecycle

    /// <summary>
    /// Called when the dialog is opened.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    /// <remarks>
    /// <para>
    /// Subscribes to the ViewModel's PropertyChanged event to monitor
    /// <see cref="ExportViewModel.ShouldClose"/>.
    /// </para>
    /// <para>
    /// Calls <see cref="ExportViewModel.InitializeAsync"/> to load the initial preview.
    /// </para>
    /// </remarks>
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is ExportViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            await viewModel.InitializeAsync();
        }
    }

    /// <summary>
    /// Called when the dialog is closed.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    /// <remarks>
    /// <para>
    /// Unsubscribes from the ViewModel's PropertyChanged event
    /// and disposes the ViewModel.
    /// </para>
    /// </remarks>
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is ExportViewModel viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            viewModel.Dispose();
        }

        base.OnClosed(e);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the ViewModel's PropertyChanged event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    /// <remarks>
    /// <para>
    /// When <see cref="ExportViewModel.ShouldClose"/> becomes true,
    /// closes the dialog.
    /// </para>
    /// </remarks>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExportViewModel.ShouldClose) &&
            DataContext is ExportViewModel { ShouldClose: true })
        {
            Close();
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases resources used by this dialog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Disposes the ViewModel if not already disposed.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (DataContext is ExportViewModel viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            viewModel.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
