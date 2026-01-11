using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

public partial class ExportViewModel : ViewModelBase
{
    private readonly IExportService _exportService;
    private readonly IStorageProvider _storageProvider;
    private readonly Guid _conversationId;

    [ObservableProperty]
    private ExportFormat _selectedFormat = ExportFormat.Markdown;

    [ObservableProperty]
    private bool _includeTimestamps = true;

    [ObservableProperty]
    private bool _includeSystemPrompt = true;

    [ObservableProperty]
    private bool _includeTokenCounts;

    [ObservableProperty]
    private bool _includeMetadata = true;

    [ObservableProperty]
    private string _preview = string.Empty;

    [ObservableProperty]
    private bool _isExporting;

    public event EventHandler? CloseRequested;

    public ExportViewModel(
        IExportService exportService,
        IStorageProvider storageProvider,
        Guid conversationId)
    {
        _exportService = exportService;
        _storageProvider = storageProvider;
        _conversationId = conversationId;
    }

    public async Task InitializeAsync()
    {
        await UpdatePreviewAsync();
    }

    partial void OnSelectedFormatChanged(ExportFormat value) => _ = UpdatePreviewAsync();
    partial void OnIncludeTimestampsChanged(bool value) => _ = UpdatePreviewAsync();
    partial void OnIncludeSystemPromptChanged(bool value) => _ = UpdatePreviewAsync();
    partial void OnIncludeTokenCountsChanged(bool value) => _ = UpdatePreviewAsync();
    partial void OnIncludeMetadataChanged(bool value) => _ = UpdatePreviewAsync();

    private async Task UpdatePreviewAsync()
    {
        try
        {
            Preview = await _exportService.GeneratePreviewAsync(
                _conversationId,
                CreateOptions(),
                maxLength: 500);
        }
        catch (Exception ex)
        {
            Preview = $"Error generating preview: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            IsExporting = true;
            ClearError();

            var result = await _exportService.ExportAsync(_conversationId, CreateOptions());

            if (!result.Success)
            {
                SetError(result.ErrorMessage ?? "Export failed");
                return;
            }

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
                // User cancelled the save dialog
                return;
            }

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(result.Content);

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private ExportOptions CreateOptions() => new()
    {
        Format = SelectedFormat,
        IncludeTimestamps = IncludeTimestamps,
        IncludeSystemPrompt = IncludeSystemPrompt,
        IncludeTokenCounts = IncludeTokenCounts,
        IncludeMetadata = IncludeMetadata
    };

    private static string GetFormatName(ExportFormat format) => format switch
    {
        ExportFormat.Markdown => "Markdown",
        ExportFormat.Json => "JSON",
        ExportFormat.PlainText => "Plain Text",
        ExportFormat.Html => "HTML",
        _ => "File"
    };
}
