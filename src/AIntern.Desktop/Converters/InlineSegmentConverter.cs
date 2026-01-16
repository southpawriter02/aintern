using Avalonia.Data.Converters;
using System.Globalization;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ INLINE SEGMENT CONVERTER (v0.4.2f)                                       │
// │ Converts InlineSegment.IsChanged to boolean for inline styling.          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts InlineSegment.IsChanged to a boolean for inline change class styling.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2f.</para>
/// <para>
/// Used with Classes.inline-changed binding to apply background highlighting
/// to changed segments within a diff line.
/// </para>
/// </remarks>
public sealed class InlineSegmentConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for XAML static resource usage.
    /// </summary>
    public static readonly InlineSegmentConverter Instance = new();

    /// <summary>
    /// Converts IsChanged bool to inline change class application.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isChanged && isChanged;
    }

    /// <summary>
    /// Convert back (not implemented for one-way binding).
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
