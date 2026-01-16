using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AIntern.Core.Models;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PHASE TO BRUSH CONVERTER (v0.4.4g)                                      │
// │ Maps BatchApplyPhase to foreground brush colors.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts a <see cref="BatchApplyPhase"/> to a foreground brush color.
/// </summary>
/// <remarks>
/// <para>
/// Provides visual feedback:
/// <list type="bullet">
/// <item>Completed → Success (green)</item>
/// <item>RollingBack → Error (red)</item>
/// <item>Other phases → Accent (blue)</item>
/// </list>
/// </para>
/// <para>Added in v0.4.4g.</para>
/// </remarks>
public class PhaseToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush AccentBrush = new(Color.Parse("#007ACC"));
    private static readonly SolidColorBrush SuccessBrush = new(Color.Parse("#89D185"));
    private static readonly SolidColorBrush ErrorBrush = new(Color.Parse("#F48771"));

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BatchApplyPhase phase)
        {
            return AccentBrush;
        }

        return phase switch
        {
            BatchApplyPhase.Completed => SuccessBrush,
            BatchApplyPhase.RollingBack => ErrorBrush,
            _ => AccentBrush
        };
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("PhaseToBrushConverter is one-way only.");
    }
}
