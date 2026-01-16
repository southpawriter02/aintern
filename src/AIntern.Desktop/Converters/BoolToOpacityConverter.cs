namespace AIntern.Desktop.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

/// <summary>
/// Converts boolean to opacity (1.0 for true, 0.4 for false).
/// </summary>
/// <remarks>Added in v0.3.5d.</remarks>
public class BoolToOpacityConverter : IValueConverter
{
    /// <summary>
    /// Opacity when true.
    /// </summary>
    public double TrueValue { get; set; } = 1.0;

    /// <summary>
    /// Opacity when false.
    /// </summary>
    public double FalseValue { get; set; } = 0.4;

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueValue : FalseValue;
        }
        return FalseValue;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
