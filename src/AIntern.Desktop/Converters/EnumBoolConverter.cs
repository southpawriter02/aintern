// -----------------------------------------------------------------------
// <copyright file="EnumBoolConverter.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Converts between enum values and boolean for RadioButton bindings.
//     Added in v0.2.5f.
// </summary>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts between enum values and boolean for RadioButton bindings.
/// </summary>
/// <remarks>
/// <para>
/// This converter enables RadioButton binding to enum properties by comparing
/// the bound enum value to a string parameter. When the enum matches the parameter,
/// Convert returns true (checked). When a RadioButton is checked, ConvertBack
/// parses the parameter string back to the enum type.
/// </para>
/// <para>
/// <b>Convert Logic:</b>
/// <list type="bullet">
///   <item><description>Compares enum value.ToString() to parameter string (case-insensitive)</description></item>
///   <item><description>Returns true if they match, false otherwise</description></item>
///   <item><description>Returns false for null value or null parameter</description></item>
/// </list>
/// </para>
/// <para>
/// <b>ConvertBack Logic:</b>
/// <list type="bullet">
///   <item><description>When IsChecked is true, parses parameter string to target enum type</description></item>
///   <item><description>Returns <see cref="BindingOperations.DoNothing"/> when IsChecked is false</description></item>
///   <item><description>Returns <see cref="BindingOperations.DoNothing"/> for null parameter</description></item>
/// </list>
/// </para>
/// <para>
/// <b>XAML Usage:</b>
/// <code>
/// &lt;Window.Resources&gt;
///     &lt;converters:EnumBoolConverter x:Key="EnumBoolConverter" /&gt;
/// &lt;/Window.Resources&gt;
///
/// &lt;RadioButton Content="Markdown (.md)"
///              IsChecked="{Binding SelectedFormat,
///                         Converter={StaticResource EnumBoolConverter},
///                         ConverterParameter=Markdown}" /&gt;
/// </code>
/// </para>
/// <para>Added in v0.2.5f.</para>
/// </remarks>
/// <example>
/// Binding radio buttons to an ExportFormat enum:
/// <code>
/// // ViewModel property
/// [ObservableProperty]
/// private ExportFormat _selectedFormat = ExportFormat.Markdown;
///
/// // XAML
/// &lt;RadioButton Content="Markdown"
///              IsChecked="{Binding SelectedFormat,
///                         Converter={StaticResource EnumBoolConverter},
///                         ConverterParameter=Markdown}" /&gt;
/// &lt;RadioButton Content="JSON"
///              IsChecked="{Binding SelectedFormat,
///                         Converter={StaticResource EnumBoolConverter},
///                         ConverterParameter=Json}" /&gt;
/// </code>
/// </example>
public sealed class EnumBoolConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="EnumBoolConverter"/>.
    /// </summary>
    /// <remarks>
    /// Using a singleton pattern avoids unnecessary allocations and is consistent
    /// with other converters in the application (e.g., <see cref="BoolToFontWeightConverter"/>).
    /// </remarks>
    public static EnumBoolConverter Instance { get; } = new();

    /// <summary>
    /// Converts an enum value to a boolean indicating whether it matches the parameter.
    /// </summary>
    /// <param name="value">The source enum value.</param>
    /// <param name="targetType">The target type (expected: <see cref="bool"/>).</param>
    /// <param name="parameter">The enum value name to compare against (as string).</param>
    /// <param name="culture">The culture to use for conversion (not used).</param>
    /// <returns>
    /// <c>true</c> if the enum value matches the parameter (case-insensitive);
    /// <c>false</c> if they don't match or if either value or parameter is null.
    /// </returns>
    /// <example>
    /// <code>
    /// var converter = EnumBoolConverter.Instance;
    ///
    /// // Returns true - Markdown matches "Markdown"
    /// converter.Convert(ExportFormat.Markdown, typeof(bool), "Markdown", null);
    ///
    /// // Returns false - Json doesn't match "Markdown"
    /// converter.Convert(ExportFormat.Json, typeof(bool), "Markdown", null);
    /// </code>
    /// </example>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        var enumValue = value.ToString();
        var targetValue = parameter.ToString();

        return string.Equals(enumValue, targetValue, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts a boolean back to the enum value specified by the parameter.
    /// </summary>
    /// <param name="value">The boolean value (IsChecked state).</param>
    /// <param name="targetType">The target enum type.</param>
    /// <param name="parameter">The enum value name to parse (as string).</param>
    /// <param name="culture">The culture to use for conversion (not used).</param>
    /// <returns>
    /// The parsed enum value if <paramref name="value"/> is <c>true</c>;
    /// <see cref="BindingOperations.DoNothing"/> if <paramref name="value"/> is <c>false</c>,
    /// not a boolean, or if <paramref name="parameter"/> is null.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returning <see cref="BindingOperations.DoNothing"/> for unchecked radio buttons
    /// prevents the binding from updating the source, which is the expected behavior
    /// for radio button groups.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var converter = EnumBoolConverter.Instance;
    ///
    /// // Returns ExportFormat.Json when radio button is checked
    /// converter.ConvertBack(true, typeof(ExportFormat), "Json", null);
    ///
    /// // Returns DoNothing when radio button is unchecked
    /// converter.ConvertBack(false, typeof(ExportFormat), "Json", null);
    /// </code>
    /// </example>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isChecked || !isChecked || parameter is null)
        {
            return BindingOperations.DoNothing;
        }

        return Enum.Parse(targetType, parameter.ToString()!);
    }
}
