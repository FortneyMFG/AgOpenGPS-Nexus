using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides historical insight and tuning controls for the AutoSteer plugin.
/// </summary>
public sealed class SteerDashboardViewModel : ObservableObject
{
    private readonly ObservableCollection<SteerTuningEventViewModel> _events = new();
    private readonly ReadOnlyObservableCollection<SteerTuningEventViewModel> _readOnlyEvents;
    private readonly ObservableCollection<DashboardSeriesViewModel> _series = new();
    private readonly ReadOnlyObservableCollection<DashboardSeriesViewModel> _readOnlySeries;
    private readonly IReadOnlyList<DashboardTuningParameterViewModel> _tuningParameters;

    private readonly DashboardSeriesViewModel _crossTrackSeries;
    private readonly DashboardSeriesViewModel _wheelAngleSeries;
    private readonly DashboardSeriesViewModel _controllerOutputSeries;
    private readonly DashboardTuningParameterViewModel _proportionalParameter;
    private readonly DashboardTuningParameterViewModel _integralParameter;
    private readonly DashboardTuningParameterViewModel _derivativeParameter;
    private double _proportionalGain = 0.28;
    private double _integralGain = 0.02;
    private double _derivativeGain = 0.12;
    private string _status = "Awaiting telemetry";

    /// <summary>
    /// Initializes a new instance of the <see cref="SteerDashboardViewModel"/> class.
    /// </summary>
    public SteerDashboardViewModel()
    {
        _readOnlyEvents = new ReadOnlyObservableCollection<SteerTuningEventViewModel>(_events);
        _crossTrackSeries = new DashboardSeriesViewModel(
            id: "autosteer.crossTrack",
            title: "Cross-track error",
            units: "meters",
            stroke: Brushes.LimeGreen);
        _wheelAngleSeries = new DashboardSeriesViewModel(
            id: "autosteer.wheelAngle",
            title: "Wheel angle",
            units: "degrees",
            stroke: new SolidColorBrush(Color.FromArgb(0xFF, 0x35, 0xA1, 0xFF)),
            fill: new SolidColorBrush(Color.FromArgb(0x20, 0x35, 0xA1, 0xFF)));
        _controllerOutputSeries = new DashboardSeriesViewModel(
            id: "autosteer.controllerOutput",
            title: "Controller output",
            units: "fraction",
            stroke: new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00)),
            fill: new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xD7, 0x00)));

        _series.Add(_crossTrackSeries);
        _series.Add(_wheelAngleSeries);
        _series.Add(_controllerOutputSeries);
        _readOnlySeries = new ReadOnlyObservableCollection<DashboardSeriesViewModel>(_series);

        _proportionalParameter = new DashboardTuningParameterViewModel(
            id: "controller.p",
            label: "P",
            minimum: 0,
            maximum: 2,
            step: 0.01,
            displayFormat: "{0:0.00}",
            valueChanged: value => ProportionalGain = value);
        _integralParameter = new DashboardTuningParameterViewModel(
            id: "controller.i",
            label: "I",
            minimum: 0,
            maximum: 0.5,
            step: 0.005,
            displayFormat: "{0:0.00}",
            valueChanged: value => IntegralGain = value);
        _derivativeParameter = new DashboardTuningParameterViewModel(
            id: "controller.d",
            label: "D",
            minimum: 0,
            maximum: 1,
            step: 0.01,
            displayFormat: "{0:0.00}",
            valueChanged: value => DerivativeGain = value);

        _tuningParameters = new[]
        {
            _proportionalParameter,
            _integralParameter,
            _derivativeParameter,
        };

        _proportionalParameter.SetValueFromOwner(_proportionalGain);
        _integralParameter.SetValueFromOwner(_integralGain);
        _derivativeParameter.SetValueFromOwner(_derivativeGain);
    }

    /// <summary>Gets the dashboard series rendered in the metadata-driven layout.</summary>
    public IReadOnlyList<DashboardSeriesViewModel> Series => _readOnlySeries;

    /// <summary>Gets or sets the proportional gain used by the AutoSteer controller.</summary>
    public double ProportionalGain
    {
        get => _proportionalGain;
        set
        {
            if (SetProperty(ref _proportionalGain, Math.Clamp(value, 0, 2)))
            {
                OnPropertyChanged(nameof(GainSummary));
                _proportionalParameter.SetValueFromOwner(_proportionalGain);
            }
        }
    }

    /// <summary>Gets or sets the integral gain used by the AutoSteer controller.</summary>
    public double IntegralGain
    {
        get => _integralGain;
        set
        {
            if (SetProperty(ref _integralGain, Math.Clamp(value, 0, 0.5)))
            {
                OnPropertyChanged(nameof(GainSummary));
                _integralParameter.SetValueFromOwner(_integralGain);
            }
        }
    }

    /// <summary>Gets or sets the derivative gain used by the AutoSteer controller.</summary>
    public double DerivativeGain
    {
        get => _derivativeGain;
        set
        {
            if (SetProperty(ref _derivativeGain, Math.Clamp(value, 0, 1)))
            {
                OnPropertyChanged(nameof(GainSummary));
                _derivativeParameter.SetValueFromOwner(_derivativeGain);
            }
        }
    }

    /// <summary>Gets a formatted summary of the current tuning values.</summary>
    public string GainSummary => $"P {ProportionalGain:0.00} / I {IntegralGain:0.00} / D {DerivativeGain:0.00}";

    /// <summary>Gets the metadata describing each exposed tuning parameter.</summary>
    public IReadOnlyList<DashboardTuningParameterViewModel> TuningParameters => _tuningParameters;

    /// <summary>Gets a short status message describing the steering health.</summary>
    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    /// <summary>Gets the recorded tuning events shown in the dashboard.</summary>
    public ReadOnlyObservableCollection<SteerTuningEventViewModel> TuningEvents => _readOnlyEvents;

    /// <summary>Applies a batch of historical samples to seed the charts.</summary>
    public void ApplyHistoricalSamples(IEnumerable<double> crossTrackErrors, IEnumerable<double> wheelAngles, IEnumerable<double> controllerOutputs)
    {
        ArgumentNullException.ThrowIfNull(crossTrackErrors);
        ArgumentNullException.ThrowIfNull(wheelAngles);
        ArgumentNullException.ThrowIfNull(controllerOutputs);

        var crossTrack = crossTrackErrors.ToArray();
        var wheels = wheelAngles.ToArray();
        var outputs = controllerOutputs.ToArray();

        _crossTrackSeries.SetValues(crossTrack);
        _wheelAngleSeries.SetValues(wheels);
        _controllerOutputSeries.SetValues(outputs);

        if (crossTrack.Length > 0)
        {
            var recent = crossTrack[^1];
            Status = $"Cross-track error {recent:0.00} m";
        }
    }

    /// <summary>Records a tuning event.</summary>
    /// <param name="timestamp">When the change occurred.</param>
    /// <param name="description">Human readable description.</param>
    public void RecordTuningEvent(DateTimeOffset timestamp, string description)
    {
        ArgumentNullException.ThrowIfNull(description);

        _events.Insert(0, new SteerTuningEventViewModel(timestamp, description));
        while (_events.Count > 6)
        {
            _events.RemoveAt(_events.Count - 1);
        }
    }

    /// <summary>Updates the status message based on the latest commanded wheel angle.</summary>
    /// <param name="targetWheelAngle">Current commanded wheel angle in degrees.</param>
    public void UpdateCommandedAngle(double targetWheelAngle)
    {
        Status = $"Commanded angle {targetWheelAngle:0.0}°";
    }
}
