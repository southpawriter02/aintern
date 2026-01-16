using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ ICON NAME CONVERTER (v0.4.4e)                                           │
// │ Converts icon name strings to StreamGeometry for PathIcon.Data binding. │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts icon name strings to StreamGeometry resources for PathIcon.Data binding.
/// </summary>
/// <remarks>
/// <para>
/// Looks up the icon name as a resource key and returns the corresponding StreamGeometry.
/// Falls back to FileIcon if the requested icon is not found.
/// </para>
/// <para>Added in v0.4.4e.</para>
/// </remarks>
public class IconNameConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use in XAML.
    /// </summary>
    public static readonly IconNameConverter Instance = new();

    /// <summary>
    /// Converts an icon name to a StreamGeometry.
    /// </summary>
    /// <param name="value">The icon name string.</param>
    /// <param name="targetType">The target type (ignored).</param>
    /// <param name="parameter">Optional parameter (ignored).</param>
    /// <param name="culture">Culture info (ignored).</param>
    /// <returns>The StreamGeometry for the icon, or null if not found.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string iconName || string.IsNullOrEmpty(iconName))
        {
            return null;
        }

        // Build the resource key - append "Icon" suffix if not present
        var resourceKey = iconName.EndsWith("Icon", StringComparison.OrdinalIgnoreCase)
            ? iconName
            : $"{iconName}Icon";

        // Try to find the icon resource
        if (Application.Current?.TryGetResource(resourceKey, null, out var resource) == true)
        {
            return resource as StreamGeometry;
        }

        // Try without the suffix
        if (Application.Current?.TryGetResource(iconName, null, out resource) == true)
        {
            return resource as StreamGeometry;
        }

        // Fall back to default file icon
        if (Application.Current?.TryGetResource("FileIcon", null, out var defaultIcon) == true)
        {
            return defaultIcon as StreamGeometry;
        }

        return null;
    }

    /// <summary>
    /// Not supported - this is a one-way converter.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("IconNameConverter is one-way only.");
    }
}
