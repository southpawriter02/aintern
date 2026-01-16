using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AIntern.Core.Models;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ VALIDATION SEVERITY TO BRUSH CONVERTER (v0.4.4e)                        │
// │ Converts ValidationSeverity to an appropriate color brush.              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts ValidationSeverity to an appropriate color brush.
/// </summary>
/// <remarks>
/// <para>
/// Mapping:
/// <list type="bullet">
/// <item>Error → ErrorForeground</item>
/// <item>Warning → WarningForeground</item>
/// <item>Info → TextMuted</item>
/// </list>
/// </para>
/// <para>Added in v0.4.4e.</para>
/// </remarks>
public class ValidationSeverityToBrushConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use in XAML.
    /// </summary>
    public static readonly ValidationSeverityToBrushConverter Instance = new();

    /// <summary>
    /// Converts ValidationSeverity to a brush.
    /// </summary>
    /// <param name="value">The ValidationSeverity value.</param>
    /// <param name="targetType">The target type (ignored).</param>
    /// <param name="parameter">Optional parameter (ignored).</param>
    /// <param name="culture">Culture info (ignored).</param>
    /// <returns>The corresponding IBrush.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ValidationSeverity severity)
        {
            return null;
        }

        var resourceKey = severity switch
        {
            ValidationSeverity.Error => "ErrorForeground",
            ValidationSeverity.Warning => "WarningForeground",
            ValidationSeverity.Info => "TextMuted",
            _ => "TextPrimary"
        };

        if (Application.Current?.TryGetResource(resourceKey, null, out var resource) == true)
        {
            return resource as IBrush;
        }

        return null;
    }

    /// <summary>
    /// Not supported - this is a one-way converter.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ValidationSeverityToBrushConverter is one-way only.");
    }
}
