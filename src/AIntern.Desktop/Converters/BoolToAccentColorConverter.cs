// ============================================================================
// BoolToAccentColorConverter.cs
// AIntern.Desktop - Status Bar Enhancements (v0.2.5g)
// ============================================================================
// Converts a boolean value to accent or muted color for status bar elements.
// Used to indicate model load state in the clickable model name display.
// ============================================================================

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts a boolean to an <see cref="IBrush"/> for status bar color coding.
/// </summary>
/// <remarks>
/// <para>
/// This converter is used in the status bar to visually distinguish between
/// active and inactive states, such as whether a model is loaded.
/// </para>
/// <para>
/// <b>Conversion Logic:</b>
/// <list type="bullet">
///   <item><c>true</c> → Accent color (#00d9ff) - indicates active/loaded state</item>
///   <item><c>false</c> → Muted color (#888888) - indicates inactive/unloaded state</item>
///   <item><c>null</c> or non-boolean → Muted color (default fallback)</item>
/// </list>
/// </para>
/// <para>
/// <b>Color Rationale:</b>
/// <list type="bullet">
///   <item>Accent (#00d9ff): Matches the application's accent color scheme</item>
///   <item>Muted (#888888): Provides clear visual distinction without being distracting</item>
/// </list>
/// </para>
/// <para>
/// <b>XAML Usage:</b>
/// <code>
/// &lt;TextBlock Foreground="{Binding IsModelLoaded, Converter={x:Static converters:BoolToAccentColorConverter.Instance}}" /&gt;
/// </code>
/// </para>
/// <para>Added in v0.2.5g.</para>
/// </remarks>
/// <seealso cref="MainWindowViewModel"/>
public sealed class BoolToAccentColorConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="BoolToAccentColorConverter"/>.
    /// </summary>
    /// <remarks>
    /// Using a singleton pattern avoids unnecessary allocations and is consistent
    /// with other converters in the application (e.g., <see cref="BoolToFontWeightConverter"/>).
    /// </remarks>
    public static BoolToAccentColorConverter Instance { get; } = new();

    /// <summary>
    /// The accent color brush used for active/loaded states.
    /// </summary>
    /// <remarks>
    /// Color #00d9ff matches the application's accent color scheme.
    /// </remarks>
    private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#00d9ff"));

    /// <summary>
    /// The muted color brush used for inactive/unloaded states.
    /// </summary>
    /// <remarks>
    /// Color #888888 provides a subdued appearance for inactive elements.
    /// </remarks>
    private static readonly IBrush MutedBrush = new SolidColorBrush(Color.Parse("#888888"));

    /// <summary>
    /// Converts a boolean value to an <see cref="IBrush"/>.
    /// </summary>
    /// <param name="value">The source value to convert (expected: <see cref="bool"/>).</param>
    /// <param name="targetType">The target type (expected: <see cref="IBrush"/>).</param>
    /// <param name="parameter">Optional conversion parameter (not used).</param>
    /// <param name="culture">The culture to use for conversion (not used).</param>
    /// <returns>
    /// <see cref="AccentBrush"/> if <paramref name="value"/> is <c>true</c>;
    /// otherwise <see cref="MutedBrush"/>.
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? AccentBrush : MutedBrush;
    }

    /// <summary>
    /// Not supported. This converter is one-way only.
    /// </summary>
    /// <param name="value">The target value to convert back.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">Optional conversion parameter.</param>
    /// <param name="culture">The culture to use for conversion.</param>
    /// <returns>Never returns; always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("BoolToAccentColorConverter does not support ConvertBack.");
    }
}
