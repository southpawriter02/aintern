using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using AIntern.Core.Interfaces;
using AIntern.Desktop.Views.Dialogs;

namespace AIntern.Desktop.Services;

/// <summary>
/// DialogService implementation using Avalonia dialogs.
/// </summary>
public sealed class DialogService : IDialogService
{
    private Window? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var window = GetMainWindow();
        if (window == null) return;

        var dialog = new MessageDialog
        {
            Title = title,
            Message = message,
            DialogIconType = MessageDialogIcon.Error,
            Buttons = new[] { "OK" }
        };

        await dialog.ShowDialog(window);
    }

    public async Task ShowInfoAsync(string title, string message)
    {
        var window = GetMainWindow();
        if (window == null) return;

        var dialog = new MessageDialog
        {
            Title = title,
            Message = message,
            DialogIconType = MessageDialogIcon.Information,
            Buttons = new[] { "OK" }
        };

        await dialog.ShowDialog(window);
    }

    public async Task<string?> ShowConfirmDialogAsync(
        string title,
        string message,
        IEnumerable<string> options)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var dialog = new MessageDialog
        {
            Title = title,
            Message = message,
            DialogIconType = MessageDialogIcon.Question,
            Buttons = options.ToArray()
        };

        return await dialog.ShowDialog<string?>(window);
    }

    public async Task<string?> ShowSaveDialogAsync(
        string title,
        string suggestedName,
        IReadOnlyList<(string Name, string[] Extensions)> filters)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;
        var fileTypes = filters.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Extensions
        }).ToList();

        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            FileTypeChoices = fileTypes
        });

        return result?.Path.LocalPath;
    }

    public async Task<string?> ShowOpenFileDialogAsync(
        string title,
        IReadOnlyList<(string Name, string[] Extensions)> filters,
        bool allowMultiple = false)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;
        var fileTypes = filters.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Extensions
        }).ToList();

        var results = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = fileTypes
        });

        return results.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<string?> ShowFolderPickerAsync(string title)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;
        var results = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return results.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<int?> ShowGoToLineDialogAsync(int maxLine, int currentLine)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var dialog = new GoToLineDialog
        {
            MaxLine = maxLine,
            CurrentLine = currentLine
        };

        return await dialog.ShowDialog<int?>(window);
    }
}
