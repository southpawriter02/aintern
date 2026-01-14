// ============================================================================
// SaveStatusConverter.cs
// AIntern.Desktop - Status Bar Enhancements (v0.2.5g)
// ============================================================================
// Contains converters for displaying save status in the status bar.
// Converts SaveStateChangedEventArgs to display text and color.
// ============================================================================

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AIntern.Core.Events;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts <see cref="SaveStateChangedEventArgs"/> to display text for the status bar.
/// </summary>
/// <remarks>
/// <para>
/// This converter transforms save state information into user-friendly status text.
/// </para>
/// <para>
/// <b>Conversion Logic (priority order):</b>
/// <list type="bullet">
///   <item><c>IsSaving = true</c> → "Saving..."</item>
///   <item><c>HasUnsavedChanges = true</c> → "Unsaved"</item>
///   <item>Otherwise → "Saved"</item>
///   <item><c>null</c> or non-SaveStateChangedEventArgs → "Saved" (default)</item>
/// </list>
/// </para>
/// <para>
/// <b>XAML Usage:</b>
/// <code>
/// &lt;TextBlock Text="{Binding SaveState, Converter={x:Static converters:SaveStatusTextConverter.Instance}}" /&gt;
/// </code>
/// </para>
/// <para>Added in v0.2.5g.</para>
/// </remarks>
/// <seealso cref="SaveStateChangedEventArgs"/>
/// <seealso cref="SaveStatusColorConverter"/>
public sealed class SaveStatusTextConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="SaveStatusTextConverter"/>.
    /// </summary>
    /// <remarks>
    /// Using a singleton pattern avoids unnecessary allocations and is consistent
    /// with other converters in the application.
    /// </remarks>
    public static SaveStatusTextConverter Instance { get; } = new();

    /// <summary>
    /// Display text for when a save operation is in progress.
    /// </summary>
    public const string SavingText = "Saving...";

    /// <summary>
    /// Display text for when there are unsaved changes.
    /// </summary>
    public const string UnsavedText = "Unsaved";

    /// <summary>
    /// Display text for when all changes are saved.
    /// </summary>
    public const string SavedText = "Saved";

    /// <summary>
    /// Converts <see cref="SaveStateChangedEventArgs"/> to display text.
    /// </summary>
    /// <param name="value">The source value to convert (expected: <see cref="SaveStateChangedEventArgs"/>).</param>
    /// <param name="targetType">The target type (expected: <see cref="string"/>).</param>
    /// <param name="parameter">Optional conversion parameter (not used).</param>
    /// <param name="culture">The culture to use for conversion (not used).</param>
    /// <returns>
    /// Status text based on the save state: "Saving...", "Unsaved", or "Saved".
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SaveStateChangedEventArgs saveState)
        {
            if (saveState.IsSaving)
            {
                return SavingText;
            }

            if (saveState.HasUnsavedChanges)
            {
                return UnsavedText;
            }

            return SavedText;
        }

        // Default to "Saved" for null or invalid input
        return SavedText;
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
        throw new NotSupportedException("SaveStatusTextConverter does not support ConvertBack.");
    }
}

/// <summary>
/// Converts <see cref="SaveStateChangedEventArgs"/> to a color brush for the status bar.
/// </summary>
/// <remarks>
/// <para>
/// This converter transforms save state information into color-coded visual feedback.
/// </para>
/// <para>
/// <b>Conversion Logic (priority order):</b>
/// <list type="bullet">
///   <item><c>IsSaving = true</c> → Accent color (#00d9ff) - blue, indicates activity</item>
///   <item><c>HasUnsavedChanges = true</c> → Yellow (#ffc107) - warning, needs attention</item>
///   <item>Otherwise → Green (#4caf50) - success, all saved</item>
///   <item><c>null</c> or non-SaveStateChangedEventArgs → Green (default)</item>
/// </list>
/// </para>
/// <para>
/// <b>Color Rationale:</b>
/// <list type="bullet">
///   <item>Accent (#00d9ff): Matches app theme, indicates ongoing activity</item>
///   <item>Yellow (#ffc107): Standard warning color, draws attention without alarm</item>
///   <item>Green (#4caf50): Standard success color, indicates everything is saved</item>
/// </list>
/// </para>
/// <para>
/// <b>XAML Usage:</b>
/// <code>
/// &lt;TextBlock Foreground="{Binding SaveState, Converter={x:Static converters:SaveStatusColorConverter.Instance}}" /&gt;
/// </code>
/// </para>
/// <para>Added in v0.2.5g.</para>
/// </remarks>
/// <seealso cref="SaveStateChangedEventArgs"/>
/// <seealso cref="SaveStatusTextConverter"/>
public sealed class SaveStatusColorConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="SaveStatusColorConverter"/>.
    /// </summary>
    /// <remarks>
    /// Using a singleton pattern avoids unnecessary allocations and is consistent
    /// with other converters in the application.
    /// </remarks>
    public static SaveStatusColorConverter Instance { get; } = new();

    /// <summary>
    /// The accent color brush used when saving is in progress.
    /// </summary>
    /// <remarks>
    /// Color #00d9ff matches the application's accent color scheme.
    /// </remarks>
    private static readonly IBrush SavingBrush = new SolidColorBrush(Color.Parse("#00d9ff"));

    /// <summary>
    /// The yellow color brush used when there are unsaved changes.
    /// </summary>
    /// <remarks>
    /// Color #ffc107 is a standard warning yellow that draws attention.
    /// </remarks>
    private static readonly IBrush UnsavedBrush = new SolidColorBrush(Color.Parse("#ffc107"));

    /// <summary>
    /// The green color brush used when all changes are saved.
    /// </summary>
    /// <remarks>
    /// Color #4caf50 is a standard success green.
    /// </remarks>
    private static readonly IBrush SavedBrush = new SolidColorBrush(Color.Parse("#4caf50"));

    /// <summary>
    /// Converts <see cref="SaveStateChangedEventArgs"/> to an <see cref="IBrush"/>.
    /// </summary>
    /// <param name="value">The source value to convert (expected: <see cref="SaveStateChangedEventArgs"/>).</param>
    /// <param name="targetType">The target type (expected: <see cref="IBrush"/>).</param>
    /// <param name="parameter">Optional conversion parameter (not used).</param>
    /// <param name="culture">The culture to use for conversion (not used).</param>
    /// <returns>
    /// A colored brush based on the save state: accent (saving), yellow (unsaved), or green (saved).
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SaveStateChangedEventArgs saveState)
        {
            if (saveState.IsSaving)
            {
                return SavingBrush;
            }

            if (saveState.HasUnsavedChanges)
            {
                return UnsavedBrush;
            }

            return SavedBrush;
        }

        // Default to green (saved) for null or invalid input
        return SavedBrush;
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
        throw new NotSupportedException("SaveStatusColorConverter does not support ConvertBack.");
    }
}
