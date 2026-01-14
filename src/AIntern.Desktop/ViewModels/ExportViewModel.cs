// -----------------------------------------------------------------------
// <copyright file="ExportViewModel.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     ViewModel for the export dialog with format selection, options, and live preview.
//     Added in v0.2.5f.
// </summary>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Enums;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the export dialog providing format selection, configurable options,
/// and live preview of the export output.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel manages the export dialog (Ctrl+E) with:
/// </para>
/// <list type="bullet">
///   <item><description><b>Format Selection:</b> Markdown, JSON, PlainText, HTML via RadioButtons</description></item>
///   <item><description><b>Options:</b> Timestamps, system prompt, metadata, token counts via CheckBoxes</description></item>
///   <item><description><b>Live Preview:</b> Updates when any option changes</description></item>
///   <item><description><b>Export:</b> Opens save dialog, writes file, closes dialog</description></item>
/// </list>
/// <para>
/// <b>Preview Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Preview is generated on initialization</description></item>
///   <item><description>Preview updates when format or any option changes</description></item>
///   <item><description>Preview is truncated to 500 characters maximum</description></item>
/// </list>
/// <para>
/// <b>Export Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Calls IExportService to generate full export content</description></item>
///   <item><description>Opens native file save dialog via IStorageProvider</description></item>
///   <item><description>Writes content to selected file</description></item>
///   <item><description>Sets ShouldClose to true on success or cancel</description></item>
/// </list>
/// <para>Added in v0.2.5f.</para>
/// </remarks>
public sealed partial class ExportViewModel : ViewModelBase, IDisposable
{
    #region Constants

    /// <summary>
    /// Maximum length for the preview text in characters.
    /// </summary>
    private const int PreviewMaxLength = 500;

    #endregion

    #region Fields

    private readonly IExportService _exportService;
    private readonly IStorageProvider _storageProvider;
    private readonly Guid _conversationId;
    private readonly ILogger? _logger;
    private CancellationTokenSource? _previewCts;
    private bool _disposed;

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets or sets the selected export format.
    /// </summary>
    /// <value>The current export format. Default: <see cref="ExportFormat.Markdown"/>.</value>
    /// <remarks>
    /// <para>
    /// Changing this property triggers an async preview update.
    /// </para>
    /// <para>
    /// Bound to RadioButtons via <see cref="Converters.EnumBoolConverter"/>.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private ExportFormat _selectedFormat = ExportFormat.Markdown;

    /// <summary>
    /// Gets or sets whether to include message timestamps in the export.
    /// </summary>
    /// <value>True to include timestamps. Default: true.</value>
    /// <remarks>
    /// Changing this property triggers an async preview update.
    /// </remarks>
    [ObservableProperty]
    private bool _includeTimestamps = true;

    /// <summary>
    /// Gets or sets whether to include the system prompt in the export.
    /// </summary>
    /// <value>True to include the system prompt. Default: true.</value>
    /// <remarks>
    /// Changing this property triggers an async preview update.
    /// </remarks>
    [ObservableProperty]
    private bool _includeSystemPrompt = true;

    /// <summary>
    /// Gets or sets whether to include conversation metadata in the export.
    /// </summary>
    /// <value>True to include metadata (dates). Default: true.</value>
    /// <remarks>
    /// Changing this property triggers an async preview update.
    /// </remarks>
    [ObservableProperty]
    private bool _includeMetadata = true;

    /// <summary>
    /// Gets or sets whether to include token counts per message in the export.
    /// </summary>
    /// <value>True to include token counts. Default: false.</value>
    /// <remarks>
    /// Changing this property triggers an async preview update.
    /// </remarks>
    [ObservableProperty]
    private bool _includeTokenCounts = false;

    /// <summary>
    /// Gets or sets the preview text showing a sample of the export output.
    /// </summary>
    /// <value>The preview content, truncated to <see cref="PreviewMaxLength"/> characters.</value>
    /// <remarks>
    /// <para>
    /// Displayed in a monospace font in the dialog.
    /// </para>
    /// <para>
    /// If preview generation fails, contains an error message.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string _preview = string.Empty;

    /// <summary>
    /// Gets or sets whether an export operation is currently in progress.
    /// </summary>
    /// <value>True if exporting, false otherwise.</value>
    /// <remarks>
    /// <para>
    /// When true, the Export button is disabled to prevent multiple exports.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isExporting;

    /// <summary>
    /// Gets or sets whether the dialog should close.
    /// </summary>
    /// <value>True to trigger dialog close.</value>
    /// <remarks>
    /// <para>
    /// The view monitors this property to close the dialog.
    /// </para>
    /// <para>
    /// Set to true after successful export or when Cancel is clicked.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _shouldClose;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportViewModel"/> class.
    /// </summary>
    /// <param name="exportService">The export service for generating export content.</param>
    /// <param name="storageProvider">The storage provider for file save dialogs.</param>
    /// <param name="conversationId">The ID of the conversation to export.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exportService"/> or <paramref name="storageProvider"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The ViewModel is typically created fresh for each dialog instance
    /// (registered as transient in DI).
    /// </para>
    /// <para>
    /// After construction, call <see cref="InitializeAsync"/> to load the initial preview.
    /// </para>
    /// </remarks>
    public ExportViewModel(
        IExportService exportService,
        IStorageProvider storageProvider,
        Guid conversationId,
        ILogger? logger = null)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        _conversationId = conversationId;
        _logger = logger;

        _logger?.LogDebug("[INIT] ExportViewModel created - ConversationId: {Id}", _conversationId);
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the ViewModel by loading the initial preview.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Called by the view after the dialog opens.
    /// </para>
    /// <para>
    /// Generates the initial preview with default options.
    /// </para>
    /// </remarks>
    public async Task InitializeAsync()
    {
        _logger?.LogDebug("[ENTER] InitializeAsync");
        var sw = Stopwatch.StartNew();

        await UpdatePreviewAsync();

        _logger?.LogDebug("[EXIT] InitializeAsync in {Ms}ms", sw.ElapsedMilliseconds);
    }

    #endregion

    #region Property Changed Handlers

    /// <summary>
    /// Called when <see cref="SelectedFormat"/> changes.
    /// </summary>
    /// <param name="value">The new format value.</param>
    partial void OnSelectedFormatChanged(ExportFormat value)
    {
        _logger?.LogDebug("[INFO] SelectedFormat changed to: {Format}", value);
        _ = UpdatePreviewAsync();
    }

    /// <summary>
    /// Called when <see cref="IncludeTimestamps"/> changes.
    /// </summary>
    /// <param name="value">The new value.</param>
    partial void OnIncludeTimestampsChanged(bool value)
    {
        _logger?.LogDebug("[INFO] IncludeTimestamps changed to: {Value}", value);
        _ = UpdatePreviewAsync();
    }

    /// <summary>
    /// Called when <see cref="IncludeSystemPrompt"/> changes.
    /// </summary>
    /// <param name="value">The new value.</param>
    partial void OnIncludeSystemPromptChanged(bool value)
    {
        _logger?.LogDebug("[INFO] IncludeSystemPrompt changed to: {Value}", value);
        _ = UpdatePreviewAsync();
    }

    /// <summary>
    /// Called when <see cref="IncludeMetadata"/> changes.
    /// </summary>
    /// <param name="value">The new value.</param>
    partial void OnIncludeMetadataChanged(bool value)
    {
        _logger?.LogDebug("[INFO] IncludeMetadata changed to: {Value}", value);
        _ = UpdatePreviewAsync();
    }

    /// <summary>
    /// Called when <see cref="IncludeTokenCounts"/> changes.
    /// </summary>
    /// <param name="value">The new value.</param>
    partial void OnIncludeTokenCountsChanged(bool value)
    {
        _logger?.LogDebug("[INFO] IncludeTokenCounts changed to: {Value}", value);
        _ = UpdatePreviewAsync();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Exports the conversation to a file.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Performs the following steps:
    /// </para>
    /// <list type="number">
    ///   <item><description>Sets <see cref="IsExporting"/> to true</description></item>
    ///   <item><description>Calls <see cref="IExportService.ExportAsync"/> to generate content</description></item>
    ///   <item><description>Opens the native save file dialog</description></item>
    ///   <item><description>Writes the content to the selected file</description></item>
    ///   <item><description>Sets <see cref="ShouldClose"/> to true</description></item>
    /// </list>
    /// <para>
    /// If the user cancels the file dialog, the dialog remains open.
    /// If an error occurs, <see cref="ViewModelBase.ErrorMessage"/> is set.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task ExportAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] ExportAsync");

        try
        {
            IsExporting = true;
            ClearError();

            var options = CreateOptions();
            _logger?.LogDebug("[INFO] Export options: {Options}", options.LogSummary);

            // Generate export content
            var result = await _exportService.ExportAsync(_conversationId, options);

            if (!result.Success)
            {
                _logger?.LogWarning("[WARN] Export failed: {Error}", result.ErrorMessage);
                SetError(result.ErrorMessage ?? "Export failed");
                return;
            }

            // Show save file picker
            var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Conversation",
                SuggestedFileName = result.SuggestedFileName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(GetFormatName(SelectedFormat))
                    {
                        Patterns = new[] { $"*{_exportService.GetFileExtension(SelectedFormat)}" },
                        MimeTypes = new[] { result.MimeType }
                    }
                }
            });

            if (file is null)
            {
                _logger?.LogDebug("[SKIP] Export cancelled - no file selected");
                return;
            }

            // Write content to file
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(result.Content);

            _logger?.LogInformation("[INFO] Exported to {File} in {Ms}ms",
                file.Name, sw.ElapsedMilliseconds);

            ShouldClose = true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] ExportAsync - {Message}", ex.Message);
            SetError($"Export failed: {ex.Message}");
        }
        finally
        {
            IsExporting = false;
            _logger?.LogDebug("[EXIT] ExportAsync in {Ms}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Cancels the export and closes the dialog.
    /// </summary>
    /// <remarks>
    /// Sets <see cref="ShouldClose"/> to true without exporting.
    /// </remarks>
    [RelayCommand]
    private void Cancel()
    {
        _logger?.LogDebug("[ENTER] Cancel");
        ShouldClose = true;
        _logger?.LogDebug("[EXIT] Cancel");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates the preview with current options.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task UpdatePreviewAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] UpdatePreviewAsync");

        // Cancel any previous preview generation
        _previewCts?.Cancel();
        _previewCts = new CancellationTokenSource();
        var ct = _previewCts.Token;

        try
        {
            ClearError();
            var options = CreateOptions();

            var preview = await _exportService.GeneratePreviewAsync(
                _conversationId,
                options,
                PreviewMaxLength,
                ct);

            if (ct.IsCancellationRequested)
            {
                _logger?.LogDebug("[SKIP] UpdatePreviewAsync - Cancelled");
                return;
            }

            Preview = preview;
            _logger?.LogDebug("[EXIT] UpdatePreviewAsync in {Ms}ms", sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("[SKIP] UpdatePreviewAsync - Cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] UpdatePreviewAsync - {Message}", ex.Message);
            Preview = $"Error generating preview: {ex.Message}";
        }
    }

    /// <summary>
    /// Creates an <see cref="ExportOptions"/> instance from current property values.
    /// </summary>
    /// <returns>An <see cref="ExportOptions"/> with current settings.</returns>
    private ExportOptions CreateOptions() => new()
    {
        Format = SelectedFormat,
        IncludeTimestamps = IncludeTimestamps,
        IncludeSystemPrompt = IncludeSystemPrompt,
        IncludeMetadata = IncludeMetadata,
        IncludeTokenCounts = IncludeTokenCounts
    };

    /// <summary>
    /// Gets a human-readable name for the export format.
    /// </summary>
    /// <param name="format">The export format.</param>
    /// <returns>A human-readable format name.</returns>
    private static string GetFormatName(ExportFormat format) => format switch
    {
        ExportFormat.Markdown => "Markdown",
        ExportFormat.Json => "JSON",
        ExportFormat.PlainText => "Plain Text",
        ExportFormat.Html => "HTML",
        _ => "File"
    };

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases resources used by this ViewModel.
    /// </summary>
    /// <remarks>
    /// Cancels any pending preview generation.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger?.LogDebug("[INFO] ExportViewModel disposing");

        _previewCts?.Cancel();
        _previewCts?.Dispose();

        _disposed = true;

        _logger?.LogDebug("[INFO] ExportViewModel disposed");
    }

    #endregion
}
