using System.Globalization;
using Avalonia.Data.Converters;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ REFERENCE EQUALS CONVERTER (v0.4.4f)                                    │
// │ Compares two objects by reference for selection state binding.          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Compares two objects by reference to determine if they are the same instance.
/// </summary>
/// <remarks>
/// <para>
/// Useful for determining selection state in ItemsControl scenarios where
/// the selected item needs to be compared with each item's DataContext.
/// </para>
/// <para>Added in v0.4.4f.</para>
/// </remarks>
public class ReferenceEqualsConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use in XAML.
    /// </summary>
    public static readonly ReferenceEqualsConverter Instance = new();

    /// <summary>
    /// Returns true if value and parameter are the same object reference.
    /// </summary>
    /// <param name="value">The first object to compare.</param>
    /// <param name="targetType">The target type (ignored).</param>
    /// <param name="parameter">The second object to compare.</param>
    /// <param name="culture">Culture info (ignored).</param>
    /// <returns>True if both objects are the same reference, false otherwise.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ReferenceEquals(value, parameter);
    }

    /// <summary>
    /// Not supported - this is a one-way converter.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ReferenceEqualsConverter is one-way only.");
    }
}
