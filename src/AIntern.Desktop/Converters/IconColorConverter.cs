namespace AIntern.Desktop.Converters;

using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

/// <summary>
/// Converts icon key and item type to appropriate icon color.
/// Provides language-specific branding colors for file icons.
/// </summary>
/// <remarks>Added in v0.3.2e.</remarks>
public class IconColorConverter : IMultiValueConverter
{
    /// <summary>
    /// Singleton instance for use in XAML.
    /// </summary>
    public static IconColorConverter Instance { get; } = new();

    /// <summary>
    /// Folder icon color - warm gold.
    /// </summary>
    private static readonly Color FolderColor = Color.Parse("#E8A838");

    /// <summary>
    /// Default file icon color - neutral gray.
    /// </summary>
    private static readonly Color DefaultColor = Color.Parse("#8B8B8B");

    /// <summary>
    /// Language-specific icon colors based on official branding.
    /// </summary>
    private static readonly Dictionary<string, Color> IconColors = new()
    {
        // .NET / C#
        ["file-csharp"] = Color.Parse("#68217A"),      // Visual Studio Purple
        
        // JavaScript / TypeScript
        ["file-javascript"] = Color.Parse("#F7DF1E"),  // JS Yellow
        ["file-typescript"] = Color.Parse("#3178C6"),  // TS Blue
        
        // Python
        ["file-python"] = Color.Parse("#3776AB"),      // Python Blue
        
        // Web
        ["file-html"] = Color.Parse("#E34F26"),        // HTML5 Orange
        ["file-css"] = Color.Parse("#1572B6"),         // CSS3 Blue
        
        // Data / Config
        ["file-json"] = Color.Parse("#CBB078"),        // JSON Gold
        ["file-xml"] = Color.Parse("#E34C26"),         // XML Orange
        ["file-yaml"] = Color.Parse("#CB171E"),        // YAML Red
        
        // Documentation
        ["file-markdown"] = Color.Parse("#083FA1"),    // Markdown Blue
        ["file-text"] = Color.Parse("#8B8B8B"),        // Plain text Gray
        
        // Version Control
        ["file-git"] = Color.Parse("#F05032"),         // Git Red
        
        // Systems
        ["file-rust"] = Color.Parse("#DEA584"),        // Rust Orange
        ["file-go"] = Color.Parse("#00ADD8"),          // Go Cyan
        ["file-java"] = Color.Parse("#B07219"),        // Java Brown
        ["file-shell"] = Color.Parse("#89E051"),       // Shell Green
        
        // Database
        ["file-database"] = Color.Parse("#336791"),    // PostgreSQL Blue
        
        // Images
        ["file-image"] = Color.Parse("#8BC34A"),       // Light Green
        
        // Config
        ["file-config"] = Color.Parse("#6D8086"),      // Config Gray-Blue
    };

    /// <summary>
    /// Converts icon key and directory flag to a color brush.
    /// </summary>
    /// <param name="values">
    /// values[0]: IconKey (string)
    /// values[1]: IsDirectory (bool)
    /// </param>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // Validate input
        if (values.Count < 2)
            return new SolidColorBrush(DefaultColor);

        var iconKey = values[0] as string ?? "file";
        var isDirectory = values[1] is bool b && b;

        // Folders always get the folder color
        if (isDirectory)
            return new SolidColorBrush(FolderColor);

        // Look up language-specific color
        if (IconColors.TryGetValue(iconKey, out var color))
            return new SolidColorBrush(color);

        // Fallback to default gray
        return new SolidColorBrush(DefaultColor);
    }

    /// <summary>
    /// Gets the color for a specific icon key (for programmatic use).
    /// </summary>
    public static Color GetColorForIconKey(string iconKey, bool isDirectory = false)
    {
        if (isDirectory)
            return FolderColor;

        return IconColors.TryGetValue(iconKey, out var color) 
            ? color 
            : DefaultColor;
    }

    /// <summary>
    /// Gets all supported icon keys with colors.
    /// </summary>
    public static IReadOnlyDictionary<string, Color> GetAllColors() 
        => IconColors;

    /// <summary>
    /// Gets the folder color constant.
    /// </summary>
    public static Color GetFolderColor() => FolderColor;

    /// <summary>
    /// Gets the default color constant.
    /// </summary>
    public static Color GetDefaultColor() => DefaultColor;
}
