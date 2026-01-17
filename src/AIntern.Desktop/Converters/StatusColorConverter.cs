using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AIntern.Core.Models;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ STATUS COLOR CONVERTER (v0.4.5i)                                        │
// │ Converts StatusColor enum to Avalonia brushes.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts StatusColor enum to Avalonia Brush.
/// </summary>
public class StatusColorConverter : IValueConverter
{
    // Color palette matching design spec
    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#4CAF50"));
    private static readonly IBrush WarningBrush = new SolidColorBrush(Color.Parse("#FFC107"));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#F44336"));
    private static readonly IBrush InfoBrush = new SolidColorBrush(Color.Parse("#2196F3"));
    private static readonly IBrush MutedBrush = new SolidColorBrush(Color.Parse("#9E9E9E"));
    private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#B0BEC5"));

    /// <summary>Singleton instance for binding.</summary>
    public static readonly StatusColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value switch
        {
            StatusColor sc => sc,
            string s when Enum.TryParse<StatusColor>(s, out var parsed) => parsed,
            _ => StatusColor.Default
        };

        return color switch
        {
            StatusColor.Success => SuccessBrush,
            StatusColor.Warning => WarningBrush,
            StatusColor.Error => ErrorBrush,
            StatusColor.Info => InfoBrush,
            StatusColor.Muted => MutedBrush,
            _ => DefaultBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
