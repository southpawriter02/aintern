namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ MultiplyConverter (v0.5.2f)                                                  │
// │ Multiplies a numeric value by a factor for dynamic height calculations.     │
// └─────────────────────────────────────────────────────────────────────────────┘

using System;
using System.Globalization;
using Avalonia.Data.Converters;

#region Type Documentation

/// <summary>
/// Multiplies a numeric value by a factor specified in the converter parameter.
/// Used for calculating dynamic max heights based on window dimensions.
/// </summary>
/// <remarks>
/// <para>
/// This converter enables responsive height constraints that scale with
/// the window size, such as limiting a panel to 70% of the window height.
/// </para>
/// <para>
/// Usage in XAML:
/// <code>
/// MaxHeight="{Binding Bounds.Height,
///             RelativeSource={RelativeSource AncestorType=Window},
///             Converter={StaticResource MultiplyConverter},
///             ConverterParameter=0.7}"
/// </code>
/// </para>
/// <para>Added in v0.5.2f.</para>
/// </remarks>
/// <example>
/// <code>
/// // Input: value=800.0, parameter="0.7"
/// // Output: 560.0
/// 
/// // Input: value=1000.0, parameter="0.5"
/// // Output: 500.0
/// </code>
/// </example>

#endregion

public class MultiplyConverter : IValueConverter
{
    #region Singleton Instance

    /// <summary>
    /// Singleton instance for use in XAML resources.
    /// </summary>
    /// <remarks>
    /// Using a static instance avoids creating multiple converter instances
    /// and allows direct XAML binding via {x:Static converters:MultiplyConverter.Instance}.
    /// </remarks>
    public static readonly MultiplyConverter Instance = new();

    #endregion

    #region IValueConverter Implementation

    /// <summary>
    /// Converts a numeric value by multiplying it with the parameter.
    /// </summary>
    /// <param name="value">The numeric value to multiply (typically a double).</param>
    /// <param name="targetType">The target type (not used).</param>
    /// <param name="parameter">The multiplier as a string (e.g., "0.7").</param>
    /// <param name="culture">The culture info (not used, uses InvariantCulture).</param>
    /// <returns>
    /// The multiplied value if both value and parameter are valid;
    /// otherwise, the original value unchanged.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The parameter is parsed using InvariantCulture to ensure consistent
    /// behavior regardless of the user's locale settings (e.g., "0.7" works
    /// in both US and German locales).
    /// </para>
    /// <para>
    /// If either the value is not a double or the parameter cannot be parsed,
    /// the original value is returned without modification. This provides
    /// graceful degradation rather than throwing exceptions during binding.
    /// </para>
    /// </remarks>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Validate: value must be double, parameter must be parseable string
        if (value is double d && 
            parameter is string s && 
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
        {
            // Perform multiplication and return result
            return d * factor;
        }

        // Fallback: return original value if conversion fails
        return value;
    }

    /// <summary>
    /// Not implemented - this is a one-way converter.
    /// </summary>
    /// <param name="value">The value (unused).</param>
    /// <param name="targetType">The target type (unused).</param>
    /// <param name="parameter">The parameter (unused).</param>
    /// <param name="culture">The culture (unused).</param>
    /// <returns>Never returns - always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    /// <remarks>
    /// ConvertBack is not supported because reversing a multiplication
    /// would require dividing, and there's no practical use case for
    /// binding back from a calculated max height to the original window size.
    /// </remarks>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("MultiplyConverter is a one-way converter.");
    }

    #endregion
}
