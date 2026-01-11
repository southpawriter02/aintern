using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts between enum values and bool for RadioButton bindings.
/// Usage: IsChecked="{Binding EnumProp, Converter={StaticResource EnumBoolConverter}, ConverterParameter=EnumValue}"
/// </summary>
public sealed class EnumBoolConverter : IValueConverter
{
    public static readonly EnumBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        var enumValue = value.ToString();
        var targetValue = parameter.ToString();

        return string.Equals(enumValue, targetValue, StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isChecked || !isChecked || parameter is null)
            return BindingOperations.DoNothing;

        return Enum.Parse(targetType, parameter.ToString()!);
    }
}
