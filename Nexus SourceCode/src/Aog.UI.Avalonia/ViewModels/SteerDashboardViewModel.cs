using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides historical insight and tuning controls for the AutoSteer plugin.
/// </summary>
public sealed class SteerDashboardViewModel : ObservableObject
{
    private readonly ObservableCollection<SteerTuningEventViewModel> _events = new();
    private readonly ReadOnlyObservableCollection<SteerTuningEventViewModel> _readOnlyEvents;

    private IReadOnlyList<double> _crossTrackErrorHistory = Array.Empty<double>();
    private IReadOnlyList<double> _wheelAngleHistory = Array.Empty<double>();
    private IReadOnlyList<double> _controllerOutputHistory = Array.Empty<double>();
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
    }

    /// <summary>Gets the most recent cross-track error samples (meters).</summary>
    public IReadOnlyList<double> CrossTrackErrorHistory
    {
        get => _crossTrackErrorHistory;
        private set => SetProperty(ref _crossTrackErrorHistory, value);
    }

    /// <summary>Gets the most recent wheel angle samples (degrees).</summary>
    public IReadOnlyList<double> WheelAngleHistory
    {
        get => _wheelAngleHistory;
        private set => SetProperty(ref _wheelAngleHistory, value);
    }

    /// <summary>Gets the most recent controller output samples (unitless).</summary>
    public IReadOnlyList<double> ControllerOutputHistory
    {
        get => _controllerOutputHistory;
        private set => SetProperty(ref _controllerOutputHistory, value);
    }

    /// <summary>Gets or sets the proportional gain used by the AutoSteer controller.</summary>
    public double ProportionalGain
    {
        get => _proportionalGain;
        set
        {
            if (SetProperty(ref _proportionalGain, Math.Clamp(value, 0, 2)))
            {
                OnPropertyChanged(nameof(GainSummary));
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
            }
        }
    }

    /// <summary>Gets a formatted summary of the current tuning values.</summary>
    public string GainSummary => $"P {ProportionalGain:0.00} / I {IntegralGain:0.00} / D {DerivativeGain:0.00}";

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

        CrossTrackErrorHistory = crossTrackErrors.ToArray();
        WheelAngleHistory = wheelAngles.ToArray();
        ControllerOutputHistory = controllerOutputs.ToArray();

        if (CrossTrackErrorHistory.Count > 0)
        {
            var recent = CrossTrackErrorHistory[^1];
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
