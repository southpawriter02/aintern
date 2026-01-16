using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AIntern.Core.Models;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PHASE TO ICON CONVERTER (v0.4.4g)                                       │
// │ Maps BatchApplyPhase to StreamGeometry icons for the progress overlay.  │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts a <see cref="BatchApplyPhase"/> to the corresponding icon geometry.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4g.</para>
/// </remarks>
public class PhaseToIconConverter : IValueConverter
{
    /// <summary>
    /// Phase-to-icon mappings using lazy initialization.
    /// </summary>
    private static readonly Lazy<Dictionary<BatchApplyPhase, StreamGeometry>> PhaseIcons = new(() =>
    {
        var icons = new Dictionary<BatchApplyPhase, StreamGeometry>();

        // Checklist icon for Validating
        icons[BatchApplyPhase.Validating] = StreamGeometry.Parse(
            "M4 4h16v16H4V4zm2 2v12h12V6H6zm2 2h4v2H8V8zm0 4h8v2H8v-2zm0 4h6v2H8v-2z");

        // Backup/Save icon for CreatingBackups
        icons[BatchApplyPhase.CreatingBackups] = StreamGeometry.Parse(
            "M17 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm2 16H5V5h11.17L19 7.83V19zm-7-7c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3zM6 6h9v4H6V6z");

        // Folder plus icon for CreatingDirectories
        icons[BatchApplyPhase.CreatingDirectories] = StreamGeometry.Parse(
            "M10 4H2v16h20V6H12l-2-2zm1 9h3v3h2v-3h3v-2h-3V8h-2v3h-3v2z");

        // File edit icon for WritingFiles
        icons[BatchApplyPhase.WritingFiles] = StreamGeometry.Parse(
            "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm-1 9h-2v2H9v-2H7v-2h2V7h2v2h2v2zm0-4V3.5L17.5 9H13z");

        // Check circle icon for Finalizing and Completed
        var checkCircle = StreamGeometry.Parse(
            "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z");
        icons[BatchApplyPhase.Finalizing] = checkCircle;
        icons[BatchApplyPhase.Completed] = checkCircle;

        // Undo/Rollback icon
        icons[BatchApplyPhase.RollingBack] = StreamGeometry.Parse(
            "M12.5 8c-2.65 0-5.05.99-6.9 2.6L2 7v9h9l-3.62-3.62c1.39-1.16 3.16-1.88 5.12-1.88 3.54 0 6.55 2.31 7.6 5.5l2.37-.78C21.08 11.03 17.15 8 12.5 8z");

        return icons;
    });

    /// <summary>
    /// Default geometry for unknown phases.
    /// </summary>
    private static readonly Lazy<StreamGeometry> DefaultIcon = new(() =>
        StreamGeometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z"));

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BatchApplyPhase phase)
        {
            return DefaultIcon.Value;
        }

        return PhaseIcons.Value.TryGetValue(phase, out var icon)
            ? icon
            : DefaultIcon.Value;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("PhaseToIconConverter is one-way only.");
    }
}
