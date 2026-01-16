namespace AIntern.Core.Interfaces;

/// <summary>
/// Abstraction for clipboard operations (v0.4.1g).
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Copy text to the system clipboard.
    /// </summary>
    /// <param name="text">The text to copy.</param>
    Task SetTextAsync(string text);

    /// <summary>
    /// Get text from the system clipboard.
    /// </summary>
    /// <returns>The clipboard text, or null if empty.</returns>
    Task<string?> GetTextAsync();

    /// <summary>
    /// Check if clipboard contains text.
    /// </summary>
    /// <returns>True if clipboard has text content.</returns>
    Task<bool> ContainsTextAsync();

    /// <summary>
    /// Clear the clipboard.
    /// </summary>
    Task ClearAsync();
}
