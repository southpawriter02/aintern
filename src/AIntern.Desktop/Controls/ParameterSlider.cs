using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;

namespace AIntern.Desktop.Controls;

/// <summary>
/// A custom slider control optimized for inference parameter adjustment with
/// label display, formatted value badge, description text, and keyboard navigation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> ParameterSlider provides a consistent UX for adjusting inference
/// parameters like Temperature, Top-P, Max Tokens, etc. It combines a label, value
/// badge, slider, and optional description into a single cohesive control.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Value Coercion:</b> Automatically clamps values to Min/Max range</description></item>
///   <item><description><b>Formatted Display:</b> Shows value with configurable format and optional unit suffix</description></item>
///   <item><description><b>Keyboard Navigation:</b> Arrow keys (±Step), Shift+Arrow (±Step×10), Home/End (Min/Max)</description></item>
///   <item><description><b>Snap to Tick:</b> Values snap to Step increments for clean values</description></item>
///   <item><description><b>Description Text:</b> Optional contextual help below the slider</description></item>
/// </list>
/// <para>
/// <b>Template Parts:</b>
/// </para>
/// <list type="bullet">
///   <item><description><c>PART_Slider</c>: The inner <see cref="Slider"/> control</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This control is designed for UI thread usage only.
/// All property changes must occur on the dispatcher thread.
/// </para>
/// <para>
/// <b>Visual Layout:</b>
/// </para>
/// <code>
/// ┌────────────────────────────────────────────────────────┐
/// │ Temperature                              [   0.7   ]   │  ← Row 0: Label + Value Badge
/// │ ═══════════════════●══════════════════════════════════ │  ← Row 1: Slider
/// │ Balanced creativity and consistency                    │  ← Row 2: Description
/// └────────────────────────────────────────────────────────┘
/// </code>
/// </remarks>
/// <example>
/// Basic usage for Temperature parameter:
/// <code>
/// &lt;controls:ParameterSlider
///     Label="Temperature"
///     Value="{Binding Temperature}"
///     Minimum="0"
///     Maximum="2"
///     Step="0.1"
///     Description="{Binding TemperatureDescription}"
///     ValueFormat="F1" /&gt;
/// </code>
/// </example>
/// <example>
/// Integer parameter with unit suffix:
/// <code>
/// &lt;controls:ParameterSlider
///     Label="Max Response Tokens"
///     Value="{Binding MaxTokens}"
///     Minimum="64"
///     Maximum="8192"
///     Step="64"
///     Description="{Binding MaxTokensDescription}"
///     IsInteger="True"
///     Unit="tokens" /&gt;
/// </code>
/// </example>
/// <seealso cref="ViewModels.InferenceSettingsViewModel"/>
public class ParameterSlider : TemplatedControl, INotifyPropertyChanged
{
    #region INotifyPropertyChanged

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    /// <remarks>
    /// Used by XAML bindings to update when computed properties like <see cref="FormattedValue"/> change.
    /// This is necessary because computed CLR properties cannot use Avalonia's StyledProperty mechanism.
    /// </remarks>
    public new event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChangedNotify(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Constants

    /// <summary>
    /// The name of the slider template part.
    /// </summary>
    /// <remarks>
    /// Template parts use the PART_ prefix by Avalonia convention to indicate
    /// required controls that the code-behind expects to find in the template.
    /// </remarks>
    private const string PartSlider = "PART_Slider";

    /// <summary>
    /// Epsilon value for floating-point comparisons.
    /// </summary>
    /// <remarks>
    /// Used to determine if values are "close enough" to be considered equal,
    /// avoiding issues with floating-point precision.
    /// </remarks>
    private const double FloatEpsilon = 0.0001;

    /// <summary>
    /// Multiplier for large step increments when Shift is held.
    /// </summary>
    private const double ShiftMultiplier = 10.0;

    #endregion

    #region Static Constructor

    /// <summary>
    /// Static constructor to register property changed callbacks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers class handlers that re-coerce the <see cref="Value"/> property
    /// whenever <see cref="Minimum"/> or <see cref="Maximum"/> changes.
    /// </para>
    /// <para>
    /// This ensures that if the valid range changes, the current value is
    /// automatically clamped to remain within bounds.
    /// </para>
    /// </remarks>
    static ParameterSlider()
    {
        Debug.WriteLine("[STATIC] ParameterSlider - Registering property changed handlers");

        // Re-coerce Value when Min/Max change to ensure it stays in bounds
        MinimumProperty.Changed.AddClassHandler<ParameterSlider>((slider, _) =>
        {
            Debug.WriteLine($"[INFO] ParameterSlider - Minimum changed, re-coercing Value");
            slider.CoerceValue(ValueProperty);
        });

        MaximumProperty.Changed.AddClassHandler<ParameterSlider>((slider, _) =>
        {
            Debug.WriteLine($"[INFO] ParameterSlider - Maximum changed, re-coercing Value");
            slider.CoerceValue(ValueProperty);
        });
    }

    #endregion

    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="Label"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The label text displayed above the slider, identifying what parameter
    /// this slider controls (e.g., "Temperature", "Max Tokens").
    /// </para>
    /// <para>
    /// Default value: "Parameter"
    /// </para>
    /// </remarks>
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ParameterSlider, string>(
            nameof(Label),
            defaultValue: "Parameter");

    /// <summary>
    /// Defines the <see cref="Value"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The current value of the slider. This property:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Uses <see cref="BindingMode.TwoWay"/> by default for seamless ViewModel binding</description></item>
    ///   <item><description>Applies value coercion to clamp values to [Minimum, Maximum]</description></item>
    ///   <item><description>Triggers <see cref="FormattedValue"/> updates when changed</description></item>
    /// </list>
    /// <para>
    /// Default value: 0.0
    /// </para>
    /// </remarks>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ParameterSlider, double>(
            nameof(Value),
            defaultValue: 0.0,
            defaultBindingMode: BindingMode.TwoWay,
            coerce: CoerceValue);

    /// <summary>
    /// Defines the <see cref="Minimum"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum allowed value for this slider. When this property changes,
    /// the current <see cref="Value"/> is automatically re-coerced to ensure
    /// it remains within the valid range.
    /// </para>
    /// <para>
    /// Default value: 0.0
    /// </para>
    /// </remarks>
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ParameterSlider, double>(
            nameof(Minimum),
            defaultValue: 0.0);

    /// <summary>
    /// Defines the <see cref="Maximum"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The maximum allowed value for this slider. When this property changes,
    /// the current <see cref="Value"/> is automatically re-coerced to ensure
    /// it remains within the valid range.
    /// </para>
    /// <para>
    /// Default value: 1.0
    /// </para>
    /// </remarks>
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ParameterSlider, double>(
            nameof(Maximum),
            defaultValue: 1.0);

    /// <summary>
    /// Defines the <see cref="Step"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The increment/decrement amount used for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Keyboard navigation (Arrow keys)</description></item>
    ///   <item><description>Tick snapping when <c>IsSnapToTickEnabled</c> is true</description></item>
    ///   <item><description>SmallChange and LargeChange on the inner slider</description></item>
    /// </list>
    /// <para>
    /// For float parameters, use values like 0.05 or 0.1. For integers, use whole numbers.
    /// </para>
    /// <para>
    /// Default value: 0.1
    /// </para>
    /// </remarks>
    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<ParameterSlider, double>(
            nameof(Step),
            defaultValue: 0.1);

    /// <summary>
    /// Defines the <see cref="Description"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contextual help text displayed below the slider. Typically bound to a
    /// computed property from the ViewModel that changes based on the current value.
    /// </para>
    /// <para>
    /// Examples:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"Very focused and deterministic" (low temperature)</description></item>
    ///   <item><description>"Balanced creativity and consistency" (medium temperature)</description></item>
    ///   <item><description>"Highly random and experimental" (high temperature)</description></item>
    /// </list>
    /// <para>
    /// Default value: "" (empty string)
    /// </para>
    /// </remarks>
    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<ParameterSlider, string>(
            nameof(Description),
            defaultValue: string.Empty);

    /// <summary>
    /// Defines the <see cref="ValueFormat"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The .NET format string used to display the value in the value badge.
    /// Common formats:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"F1" - One decimal place (0.7)</description></item>
    ///   <item><description>"F2" - Two decimal places (0.95)</description></item>
    ///   <item><description>"F0" - No decimal places (2048)</description></item>
    /// </list>
    /// <para>
    /// Note: When <see cref="IsInteger"/> is true, this property is ignored
    /// and "F0" is used automatically.
    /// </para>
    /// <para>
    /// Default value: "F1"
    /// </para>
    /// </remarks>
    public static readonly StyledProperty<string> ValueFormatProperty =
        AvaloniaProperty.Register<ParameterSlider, string>(
            nameof(ValueFormat),
            defaultValue: "F1");

    /// <summary>
    /// Defines the <see cref="Unit"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optional unit suffix displayed after the formatted value in the badge.
    /// When set, the <see cref="FormattedValue"/> becomes "{value} {unit}".
    /// </para>
    /// <para>
    /// Examples:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"tokens" → "2048 tokens"</description></item>
    ///   <item><description>"words" → "1536 words"</description></item>
    ///   <item><description>null → "0.7" (no suffix)</description></item>
    /// </list>
    /// <para>
    /// Default value: null (no unit)
    /// </para>
    /// </remarks>
    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<ParameterSlider, string?>(
            nameof(Unit),
            defaultValue: null);

    /// <summary>
    /// Defines the <see cref="ShowDescription"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls visibility of the description text row. Set to false for a
    /// more compact layout when descriptions are not needed.
    /// </para>
    /// <para>
    /// Default value: true
    /// </para>
    /// </remarks>
    public static readonly StyledProperty<bool> ShowDescriptionProperty =
        AvaloniaProperty.Register<ParameterSlider, bool>(
            nameof(ShowDescription),
            defaultValue: true);

    /// <summary>
    /// Defines the <see cref="IsInteger"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, the <see cref="ValueFormat"/> property is ignored and "F0"
    /// is used instead, displaying values without decimal places.
    /// </para>
    /// <para>
    /// Use for integer parameters like MaxTokens, TopK, Seed, ContextSize.
    /// </para>
    /// <para>
    /// Default value: false
    /// </para>
    /// </remarks>
    public static readonly StyledProperty<bool> IsIntegerProperty =
        AvaloniaProperty.Register<ParameterSlider, bool>(
            nameof(IsInteger),
            defaultValue: false);

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the label text displayed above the slider.
    /// </summary>
    /// <value>A descriptive label like "Temperature" or "Max Response Tokens".</value>
    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Gets or sets the current slider value.
    /// </summary>
    /// <value>
    /// The current value, automatically clamped to [<see cref="Minimum"/>, <see cref="Maximum"/>].
    /// </value>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum allowed value.
    /// </summary>
    /// <value>The lower bound of the valid range.</value>
    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum allowed value.
    /// </summary>
    /// <value>The upper bound of the valid range.</value>
    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>
    /// Gets or sets the step increment for keyboard navigation and tick snapping.
    /// </summary>
    /// <value>The increment amount (e.g., 0.1 for floats, 64 for tokens).</value>
    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    /// <summary>
    /// Gets or sets the description text displayed below the slider.
    /// </summary>
    /// <value>Contextual help text that may change based on current value.</value>
    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets the format string for value display.
    /// </summary>
    /// <value>A .NET format string like "F1" or "F2".</value>
    public string ValueFormat
    {
        get => GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    /// <summary>
    /// Gets or sets the optional unit suffix displayed after the value.
    /// </summary>
    /// <value>Unit text like "tokens" or null for no suffix.</value>
    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the description text is visible.
    /// </summary>
    /// <value>True to show description; false for compact layout.</value>
    public bool ShowDescription
    {
        get => GetValue(ShowDescriptionProperty);
        set => SetValue(ShowDescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets whether this slider represents an integer parameter.
    /// </summary>
    /// <value>True to use "F0" format (no decimals); false to use <see cref="ValueFormat"/>.</value>
    public bool IsInteger
    {
        get => GetValue(IsIntegerProperty);
        set => SetValue(IsIntegerProperty, value);
    }

    /// <summary>
    /// Gets the formatted value for display in the value badge.
    /// </summary>
    /// <value>
    /// Formatted string like "0.7" or "2048 tokens" depending on
    /// <see cref="ValueFormat"/>, <see cref="IsInteger"/>, and <see cref="Unit"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This computed property is updated via <see cref="RaisePropertyChanged"/>
    /// whenever <see cref="Value"/>, <see cref="ValueFormat"/>, <see cref="Unit"/>,
    /// or <see cref="IsInteger"/> changes.
    /// </para>
    /// <para>
    /// The template binds to this property using a RelativeSource binding:
    /// <c>Text="{Binding FormattedValue, RelativeSource={RelativeSource TemplatedParent}}"</c>
    /// </para>
    /// </remarks>
    public string FormattedValue
    {
        get
        {
            // Use F0 for integers, otherwise use the configured format
            var format = IsInteger ? "F0" : ValueFormat;
            var formattedNumber = Value.ToString(format);

            // Append unit suffix if specified
            return Unit != null ? $"{formattedNumber} {Unit}" : formattedNumber;
        }
    }

    #endregion

    #region Template Parts

    /// <summary>
    /// Reference to the inner Slider control from the template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field holds a reference to the PART_Slider named element found in
    /// the control template. It is used to hook keyboard events for custom
    /// navigation behavior.
    /// </para>
    /// <para>
    /// The reference is set during <see cref="OnApplyTemplate"/> and may be
    /// null if the template doesn't contain a Slider with the expected name.
    /// </para>
    /// </remarks>
    private Slider? _slider;

    /// <summary>
    /// Called when the control's template is applied.
    /// </summary>
    /// <param name="e">The template applied event arguments containing the name scope.</param>
    /// <remarks>
    /// <para>
    /// This method:
    /// </para>
    /// <list type="number">
    ///   <item><description>Calls the base implementation to complete template application</description></item>
    ///   <item><description>Unhooks any previous slider's KeyDown event (for re-templating scenarios)</description></item>
    ///   <item><description>Finds the PART_Slider element in the template's name scope</description></item>
    ///   <item><description>Hooks the new slider's KeyDown event for custom keyboard navigation</description></item>
    /// </list>
    /// <para>
    /// If PART_Slider is not found, a warning is logged but the control remains functional
    /// (just without custom keyboard navigation).
    /// </para>
    /// </remarks>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        Debug.WriteLine($"[ENTER] ParameterSlider.OnApplyTemplate - Label: {Label}");

        base.OnApplyTemplate(e);

        // Unhook previous slider if re-templating
        if (_slider != null)
        {
            Debug.WriteLine("[INFO] ParameterSlider.OnApplyTemplate - Unhooking previous slider KeyDown event");
            _slider.KeyDown -= OnSliderKeyDown;
        }

        // Find and hook the new slider
        _slider = e.NameScope.Find<Slider>(PartSlider);

        if (_slider != null)
        {
            Debug.WriteLine("[INFO] ParameterSlider.OnApplyTemplate - PART_Slider found, hooking KeyDown event");
            _slider.KeyDown += OnSliderKeyDown;
        }
        else
        {
            Debug.WriteLine("[WARNING] ParameterSlider.OnApplyTemplate - PART_Slider not found in template");
        }

        Debug.WriteLine($"[EXIT] ParameterSlider.OnApplyTemplate - Slider hooked: {_slider != null}");
    }

    #endregion

    #region Value Coercion

    /// <summary>
    /// Coerces the value to the valid range defined by Minimum and Maximum.
    /// </summary>
    /// <param name="sender">The <see cref="ParameterSlider"/> instance.</param>
    /// <param name="value">The value to coerce.</param>
    /// <returns>The value clamped to [<see cref="Minimum"/>, <see cref="Maximum"/>].</returns>
    /// <remarks>
    /// <para>
    /// This method is called automatically by the Avalonia property system when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Value"/> is set directly</description></item>
    ///   <item><description><see cref="Minimum"/> changes (via class handler)</description></item>
    ///   <item><description><see cref="Maximum"/> changes (via class handler)</description></item>
    /// </list>
    /// <para>
    /// Coercion ensures the control never enters an invalid state where
    /// Value is outside the [Minimum, Maximum] range.
    /// </para>
    /// </remarks>
    private static double CoerceValue(AvaloniaObject sender, double value)
    {
        var slider = (ParameterSlider)sender;
        var clamped = Math.Clamp(value, slider.Minimum, slider.Maximum);

        // Log if clamping actually changed the value
        if (Math.Abs(clamped - value) > FloatEpsilon)
        {
            Debug.WriteLine(
                $"[INFO] ParameterSlider.CoerceValue - Clamped {value:F4} to {clamped:F4} " +
                $"(Min: {slider.Minimum:F4}, Max: {slider.Maximum:F4})");
        }

        return clamped;
    }

    #endregion

    #region Property Changed

    /// <summary>
    /// Called when an Avalonia property changes on this control.
    /// </summary>
    /// <param name="change">The property change event arguments.</param>
    /// <remarks>
    /// <para>
    /// This override raises <see cref="RaisePropertyChanged"/> for <see cref="FormattedValue"/>
    /// when any of its dependent properties change:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Value"/> - The numeric value being formatted</description></item>
    ///   <item><description><see cref="ValueFormat"/> - The format string (e.g., "F1")</description></item>
    ///   <item><description><see cref="Unit"/> - The optional unit suffix</description></item>
    ///   <item><description><see cref="IsInteger"/> - Whether to force F0 format</description></item>
    /// </list>
    /// <para>
    /// This ensures the template's binding to FormattedValue is notified and updates the UI.
    /// </para>
    /// </remarks>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Raise FormattedValue change when any dependent property changes
        if (change.Property == ValueProperty ||
            change.Property == ValueFormatProperty ||
            change.Property == UnitProperty ||
            change.Property == IsIntegerProperty)
        {
            Debug.WriteLine(
                $"[INFO] ParameterSlider.OnPropertyChanged - Raising FormattedValue " +
                $"(cause: {change.Property.Name}, new FormattedValue: {FormattedValue})");

            OnPropertyChangedNotify(nameof(FormattedValue));
        }
    }

    #endregion

    #region Keyboard Navigation

    /// <summary>
    /// Handles keyboard input on the inner slider for custom navigation behavior.
    /// </summary>
    /// <param name="sender">The slider that received the key event.</param>
    /// <param name="e">The key event arguments.</param>
    /// <remarks>
    /// <para>
    /// Supports the following keyboard shortcuts:
    /// </para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Key</term>
    ///     <description>Action</description>
    ///   </listheader>
    ///   <item>
    ///     <term>Left / Down</term>
    ///     <description>Decrease value by <see cref="Step"/></description>
    ///   </item>
    ///   <item>
    ///     <term>Right / Up</term>
    ///     <description>Increase value by <see cref="Step"/></description>
    ///   </item>
    ///   <item>
    ///     <term>Shift + Arrow</term>
    ///     <description>Increase/decrease by Step × 10</description>
    ///   </item>
    ///   <item>
    ///     <term>Home</term>
    ///     <description>Jump to <see cref="Minimum"/></description>
    ///   </item>
    ///   <item>
    ///     <term>End</term>
    ///     <description>Jump to <see cref="Maximum"/></description>
    ///   </item>
    /// </list>
    /// <para>
    /// The Shift modifier provides a "large step" behavior for quickly moving
    /// through large value ranges (e.g., MaxTokens 64-8192).
    /// </para>
    /// </remarks>
    private void OnSliderKeyDown(object? sender, KeyEventArgs e)
    {
        // Calculate increment: 10x when Shift is held
        var hasShift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var increment = hasShift ? Step * ShiftMultiplier : Step;

        var oldValue = Value;
        var handled = true;

        switch (e.Key)
        {
            case Key.Left:
            case Key.Down:
                // Decrease, clamping to Minimum
                Value = Math.Max(Minimum, Value - increment);
                break;

            case Key.Right:
            case Key.Up:
                // Increase, clamping to Maximum
                Value = Math.Min(Maximum, Value + increment);
                break;

            case Key.Home:
                // Jump to minimum
                Value = Minimum;
                break;

            case Key.End:
                // Jump to maximum
                Value = Maximum;
                break;

            default:
                handled = false;
                break;
        }

        if (handled)
        {
            Debug.WriteLine(
                $"[INFO] ParameterSlider.OnSliderKeyDown - Key: {e.Key}, " +
                $"Shift: {hasShift}, OldValue: {oldValue:F4}, NewValue: {Value:F4}");

            e.Handled = true;
        }
    }

    #endregion
}
