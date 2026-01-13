// ============================================================================
// PinTextConverter.cs
// AIntern.Desktop - Conversation List UI (v0.2.2c)
// ============================================================================
// Converts a boolean IsPinned value to localized "Pin" or "Unpin" text for
// context menu display. Implements IValueConverter for XAML data binding.
// ============================================================================

using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts a boolean <c>IsPinned</c> value to the appropriate context menu text.
/// </summary>
/// <remarks>
/// <para>
/// This converter is used in the conversation list context menu to display
/// either "Pin" or "Unpin" based on the current pinned state of a conversation.
/// </para>
/// <para>
/// <b>Conversion Logic:</b>
/// <list type="bullet">
///   <item><c>true</c> → "Unpin" (conversation is currently pinned)</item>
///   <item><c>false</c> → "Pin" (conversation is not pinned)</item>
///   <item><c>null</c> or non-boolean → "Pin" (fallback behavior)</item>
/// </list>
/// </para>
/// <para>
/// <b>XAML Usage:</b>
/// <code>
/// &lt;MenuItem Header="{Binding IsPinned, Converter={x:Static converters:PinTextConverter.Instance}}" /&gt;
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In XAML:
/// xmlns:converters="using:AIntern.Desktop.Converters"
///
/// &lt;MenuItem Header="{Binding IsPinned,
///                     Converter={x:Static converters:PinTextConverter.Instance}}"
///           Command="{Binding $parent[ItemsControl].DataContext.TogglePinCommand}"
///           CommandParameter="{Binding}" /&gt;
/// </code>
/// </example>
public sealed class PinTextConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="PinTextConverter"/>.
    /// </summary>
    /// <remarks>
    /// Using a singleton pattern allows XAML to reference this converter via
    /// <c>{x:Static converters:PinTextConverter.Instance}</c> without requiring
    /// resource dictionary registration.
    /// </remarks>
    public static PinTextConverter Instance { get; } = new();

    /// <summary>
    /// Optional logger for diagnostic output. May be null if DI is not configured.
    /// </summary>
    /// <remarks>
    /// The logger is optional to support usage scenarios where DI is not available,
    /// such as design-time XAML preview or unit tests without full DI setup.
    /// </remarks>
    private static ILogger<PinTextConverter>? _logger;

    /// <summary>
    /// Sets the logger instance for diagnostic output.
    /// </summary>
    /// <param name="logger">The logger instance to use, or null to disable logging.</param>
    /// <remarks>
    /// This method should be called during application startup if logging is desired.
    /// It is safe to call multiple times; the last value wins.
    /// </remarks>
    public static void SetLogger(ILogger<PinTextConverter>? logger)
    {
        _logger = logger;
        _logger?.LogDebug("[INIT] PinTextConverter logger configured");
    }

    /// <summary>
    /// Converts a boolean pinned state to context menu display text.
    /// </summary>
    /// <param name="value">The source value to convert (expected: <see cref="bool"/>).</param>
    /// <param name="targetType">The target type (expected: <see cref="string"/>).</param>
    /// <param name="parameter">Optional conversion parameter (not used).</param>
    /// <param name="culture">The culture to use for conversion (not used for this converter).</param>
    /// <returns>
    /// "Unpin" if <paramref name="value"/> is <c>true</c>; otherwise "Pin".
    /// </returns>
    /// <remarks>
    /// <para>
    /// Performance: This method is called frequently during UI updates. The implementation
    /// is optimized for minimal allocations by returning cached string literals.
    /// </para>
    /// <para>
    /// Logging is performed at Debug level to avoid performance impact in production.
    /// </para>
    /// </remarks>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] Convert - Value: {Value}, Type: {Type}",
            value, value?.GetType().Name ?? "null");

        string result;

        if (value is bool isPinned)
        {
            result = isPinned ? "Unpin" : "Pin";
            _logger?.LogDebug("[INFO] Converted boolean {IsPinned} to '{Result}'", isPinned, result);
        }
        else
        {
            // Fallback for null or unexpected types
            result = "Pin";
            _logger?.LogDebug("[INFO] Non-boolean value, using fallback '{Result}'", result);
        }

        _logger?.LogDebug("[EXIT] Convert completed in {ElapsedMs}ms - Result: '{Result}'",
            sw.ElapsedMilliseconds, result);

        return result;
    }

    /// <summary>
    /// Converts a string back to a boolean pinned state.
    /// </summary>
    /// <param name="value">The target value to convert back.</param>
    /// <param name="targetType">The source type to convert to.</param>
    /// <param name="parameter">Optional conversion parameter.</param>
    /// <param name="culture">The culture to use for conversion.</param>
    /// <returns>Never returns; always throws <see cref="NotSupportedException"/>.</returns>
    /// <exception cref="NotSupportedException">
    /// Always thrown. This converter is one-way only; two-way binding is not supported.
    /// </exception>
    /// <remarks>
    /// This converter is designed for one-way binding scenarios (source → target).
    /// The pinned state is modified through commands, not through direct property binding.
    /// </remarks>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        _logger?.LogWarning("[WARN] ConvertBack called - operation not supported");
        throw new NotSupportedException(
            "PinTextConverter does not support ConvertBack. " +
            "Use commands to modify the pinned state.");
    }
}
