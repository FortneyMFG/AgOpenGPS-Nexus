using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a tunable parameter driven by dashboard metadata.
/// </summary>
public sealed class DashboardTuningParameterViewModel : ObservableObject
{
    private readonly Action<double>? _onValueChanged;
    private bool _suppressCallback;
    private double _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardTuningParameterViewModel"/> class.
    /// </summary>
    /// <param name="id">Stable identifier for the parameter.</param>
    /// <param name="label">Short label displayed beside the control.</param>
    /// <param name="minimum">Minimum valid value.</param>
    /// <param name="maximum">Maximum valid value.</param>
    /// <param name="step">Increment applied by keyboard nudges.</param>
    /// <param name="displayFormat">Composite string used when displaying the value.</param>
    /// <param name="valueChanged">Callback invoked when the value changes via the UI.</param>
    public DashboardTuningParameterViewModel(
        string id,
        string label,
        double minimum,
        double maximum,
        double step,
        string displayFormat,
        Action<double>? valueChanged = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Minimum = minimum;
        Maximum = maximum;
        Step = step;
        DisplayFormat = displayFormat ?? "{0:0.00}";
        _onValueChanged = valueChanged;
    }

    /// <summary>Gets the stable identifier for the parameter.</summary>
    public string Id { get; }

    /// <summary>Gets the label displayed beside the control.</summary>
    public string Label { get; }

    /// <summary>Gets the minimum valid value.</summary>
    public double Minimum { get; }

    /// <summary>Gets the maximum valid value.</summary>
    public double Maximum { get; }

    /// <summary>Gets the recommended step used for keyboard nudges.</summary>
    public double Step { get; }

    /// <summary>Gets the display format used when presenting the value.</summary>
    public string DisplayFormat { get; }

    /// <summary>Gets or sets the current parameter value.</summary>
    public double Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, Minimum, Maximum);
            if (SetProperty(ref _value, clamped))
            {
                OnPropertyChanged(nameof(DisplayValue));
                if (!_suppressCallback)
                {
                    _onValueChanged?.Invoke(clamped);
                }
            }
        }
    }

    /// <summary>Gets the formatted representation used in the UI.</summary>
    public string DisplayValue => string.Format(DisplayFormat, Value);

    /// <summary>
    /// Updates the parameter value without invoking the UI callback.
    /// </summary>
    /// <param name="value">Value supplied by the owning view-model.</param>
    public void SetValueFromOwner(double value)
    {
        _suppressCallback = true;
        Value = value;
        _suppressCallback = false;
    }
}
