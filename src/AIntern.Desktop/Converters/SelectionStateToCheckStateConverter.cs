using System.Globalization;
using Avalonia.Data.Converters;
using AIntern.Core.Models;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SELECTION STATE TO CHECK STATE CONVERTER (v0.4.4e)                      │
// │ Converts SelectionState enum to nullable bool for tri-state checkbox.   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts SelectionState enum to nullable bool for tri-state checkbox binding.
/// </summary>
/// <remarks>
/// <para>
/// Mapping:
/// <list type="bullet">
/// <item>None → false (unchecked)</item>
/// <item>Some → null (indeterminate)</item>
/// <item>All → true (checked)</item>
/// </list>
/// </para>
/// <para>Added in v0.4.4e.</para>
/// </remarks>
public class SelectionStateToCheckStateConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use in XAML.
    /// </summary>
    public static readonly SelectionStateToCheckStateConverter Instance = new();

    /// <summary>
    /// Converts SelectionState to nullable bool.
    /// </summary>
    /// <param name="value">The SelectionState value.</param>
    /// <param name="targetType">The target type (ignored).</param>
    /// <param name="parameter">Optional parameter (ignored).</param>
    /// <param name="culture">Culture info (ignored).</param>
    /// <returns>Nullable bool: false, null, or true.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SelectionState state)
        {
            return false;
        }

        return state switch
        {
            SelectionState.None => false,
            SelectionState.Some => null, // Indeterminate
            SelectionState.All => true,
            _ => false
        };
    }

    /// <summary>
    /// Converts nullable bool back to SelectionState.
    /// </summary>
    /// <param name="value">The nullable bool value.</param>
    /// <param name="targetType">The target type (ignored).</param>
    /// <param name="parameter">Optional parameter (ignored).</param>
    /// <param name="culture">Culture info (ignored).</param>
    /// <returns>The corresponding SelectionState.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            true => SelectionState.All,
            false => SelectionState.None,
            null => SelectionState.Some,
            _ => SelectionState.None
        };
    }
}
