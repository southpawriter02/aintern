namespace AIntern.Desktop.Converters;

using System.Globalization;
using Avalonia.Data.Converters;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CODE BLOCK CONVERTERS (v0.4.1h)                                          │
// │ Value converters for code block UI rendering.                            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Compares an enum value to a parameter string for conditional visibility/styling.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.1h for code block status badge styling.</para>
/// <para>
/// Usage:
/// <code>
/// Classes.applied="{Binding Status, Converter={StaticResource EnumEqualsConverter}, ConverterParameter=Applied}"
/// </code>
/// </para>
/// </remarks>
public class EnumEqualsConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for XAML resource usage.
    /// </summary>
    public static readonly EnumEqualsConverter Instance = new();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        var paramString = parameter.ToString();
        var valueString = value.ToString();

        return string.Equals(paramString, valueString, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("EnumEqualsConverter does not support ConvertBack.");
    }
}

/// <summary>
/// Returns true when an integer value is greater than 1.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.1h for "Apply All" button visibility.</para>
/// <para>
/// The button should only appear when there are multiple applicable blocks.
/// </para>
/// </remarks>
public class GreaterThanOneConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for XAML resource usage.
    /// </summary>
    public static readonly GreaterThanOneConverter Instance = new();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int intValue && intValue > 1;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("GreaterThanOneConverter does not support ConvertBack.");
    }
}

/// <summary>
/// Determines if a CodeBlockType represents applicable code.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.1h.</para>
/// <para>
/// Returns true for CompleteFile and Snippet types which can be applied to files.
/// Returns false for Example, Command, Output, and Config types.
/// </para>
/// </remarks>
public class IsApplicableBlockTypeConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for XAML resource usage.
    /// </summary>
    public static readonly IsApplicableBlockTypeConverter Instance = new();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CodeBlockType blockType)
        {
            return blockType is CodeBlockType.CompleteFile or CodeBlockType.Snippet;
        }
        return false;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("IsApplicableBlockTypeConverter does not support ConvertBack.");
    }
}

/// <summary>
/// Inverts a boolean value for mutually exclusive visibility.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.1h.</para>
/// </remarks>
public class InverseBoolConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for XAML resource usage.
    /// </summary>
    public static readonly InverseBoolConverter Instance = new();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }
}

/// <summary>
/// Returns true when value is not null for conditional visibility.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.1h.</para>
/// </remarks>
public class NotNullConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for XAML resource usage.
    /// </summary>
    public static readonly NotNullConverter Instance = new();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("NotNullConverter does not support ConvertBack.");
    }
}

/// <summary>
/// Returns true when an integer value is greater than zero.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.1h for HasCodeBlocks-style visibility.</para>
/// </remarks>
public class GreaterThanZeroConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for XAML resource usage.
    /// </summary>
    public static readonly GreaterThanZeroConverter Instance = new();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int intValue && intValue > 0;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("GreaterThanZeroConverter does not support ConvertBack.");
    }
}
