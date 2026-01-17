using Avalonia.Data.Converters;
using Avalonia.Media;
using AIntern.Core.Models;
using System.Globalization;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts FileChangeType to a color brush (v0.4.5h).
/// </summary>
public class ChangeTypeColorConverter : IValueConverter
{
    public static readonly ChangeTypeColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not FileChangeType changeType)
            return Brushes.Gray;

        return changeType switch
        {
            FileChangeType.Created => Brushes.Green,
            FileChangeType.Modified => Brushes.Orange,
            FileChangeType.Deleted => Brushes.Red,
            FileChangeType.Renamed => Brushes.Blue,
            _ => Brushes.Gray
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
