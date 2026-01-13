// ============================================================================
// BoolToSelectedConverter.cs
// AIntern.Desktop - Conversation List UI (v0.2.2c)
// ============================================================================
// Converts a boolean IsSelected value to a string "Selected" or empty for
// use with Tag-based style selectors in Avalonia.
// ============================================================================

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts a boolean <c>IsSelected</c> value to "Selected" or empty string.
/// </summary>
/// <remarks>
/// <para>
/// This converter is used to set the Tag property of conversation item borders
/// so that style selectors can target selected items using [Tag=Selected].
/// </para>
/// <para>
/// <b>Conversion Logic:</b>
/// <list type="bullet">
///   <item><c>true</c> → "Selected"</item>
///   <item><c>false</c> → "" (empty string)</item>
///   <item><c>null</c> or non-boolean → "" (empty string)</item>
/// </list>
/// </para>
/// <para>
/// <b>XAML Usage:</b>
/// <code>
/// &lt;Border Tag="{Binding IsSelected, Converter={x:Static converters:BoolToSelectedConverter.Instance}}"&gt;
/// </code>
/// </para>
/// </remarks>
public sealed class BoolToSelectedConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="BoolToSelectedConverter"/>.
    /// </summary>
    public static BoolToSelectedConverter Instance { get; } = new();

    /// <summary>
    /// Converts a boolean selection state to a Tag string.
    /// </summary>
    /// <param name="value">The source value to convert (expected: <see cref="bool"/>).</param>
    /// <param name="targetType">The target type (expected: <see cref="string"/>).</param>
    /// <param name="parameter">Optional conversion parameter (not used).</param>
    /// <param name="culture">The culture to use for conversion (not used).</param>
    /// <returns>"Selected" if <paramref name="value"/> is <c>true</c>; otherwise empty string.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isSelected && isSelected ? "Selected" : string.Empty;
    }

    /// <summary>
    /// Not supported. This converter is one-way only.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("BoolToSelectedConverter does not support ConvertBack.");
    }
}
