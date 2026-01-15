namespace AIntern.Desktop.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

/// <summary>
/// Converts token count integers to formatted strings with thousands separators.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4d.</para>
/// </remarks>
public class TokenCountConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use in bindings.
    /// </summary>
    public static TokenCountConverter Instance { get; } = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int count => count.ToString("N0", culture),
            long longCount => longCount.ToString("N0", culture),
            _ => value?.ToString() ?? "0"
        };
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && int.TryParse(str.Replace(",", "").Replace(" ", ""), out var result))
        {
            return result;
        }
        return 0;
    }
}
