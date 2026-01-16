namespace AIntern.Desktop.Services;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;

/// <summary>
/// Avalonia implementation of clipboard service (v0.4.1g).
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    private readonly ILogger<ClipboardService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardService"/> class.
    /// </summary>
    public ClipboardService(ILogger<ClipboardService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the clipboard instance from the current application.
    /// </summary>
    private IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }

        _logger?.LogWarning("[WARN] Clipboard not available - not running in desktop mode");
        return null;
    }

    /// <inheritdoc/>
    public async Task SetTextAsync(string text)
    {
        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(text);
            _logger?.LogDebug("[INFO] Copied {Length} characters to clipboard", text.Length);
        }
        else
        {
            _logger?.LogWarning("[WARN] Failed to copy to clipboard - clipboard not available");
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetTextAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            return await clipboard.GetTextAsync();
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<bool> ContainsTextAsync()
    {
        var text = await GetTextAsync();
        return !string.IsNullOrEmpty(text);
    }

    /// <inheritdoc/>
    public async Task ClearAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            await clipboard.ClearAsync();
            _logger?.LogDebug("[INFO] Clipboard cleared");
        }
    }
}
