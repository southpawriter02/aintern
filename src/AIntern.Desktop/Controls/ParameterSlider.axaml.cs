using Avalonia;
using Avalonia.Controls;

namespace AIntern.Desktop.Controls;

/// <summary>
/// A custom slider control for inference parameters with label, value display, and description.
/// </summary>
public partial class ParameterSlider : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ParameterSlider, string>(nameof(Label), "Parameter");

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ParameterSlider, double>(nameof(Value), 0.0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ParameterSlider, double>(nameof(Minimum), 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ParameterSlider, double>(nameof(Maximum), 1.0);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<ParameterSlider, double>(nameof(Step), 0.1);

    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<ParameterSlider, string?>(nameof(Unit));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<ParameterSlider, string?>(nameof(Description));

    /// <summary>
    /// The label displayed above the slider.
    /// </summary>
    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// The current value of the slider.
    /// </summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// The minimum value of the slider.
    /// </summary>
    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    /// The maximum value of the slider.
    /// </summary>
    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>
    /// The step/tick frequency for the slider.
    /// </summary>
    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    /// <summary>
    /// Optional unit suffix to display after the value (e.g., "tokens").
    /// </summary>
    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>
    /// Optional description text displayed below the slider.
    /// </summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public ParameterSlider()
    {
        InitializeComponent();
    }
}
