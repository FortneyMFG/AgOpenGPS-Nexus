using System;
using Aog.Core.V1;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model representing the status of a single planter row.
/// </summary>
public sealed class PlanterRowViewModel : ObservableObject
{
    private double _targetPopulationPerMeter;
    private double _actualPopulationPerMeter;
    private double _skipRate;
    private double _doubleRate;
    private PlanterRowQuality _quality;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanterRowViewModel"/> class.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index.</param>
    public PlanterRowViewModel(int rowIndex)
    {
        if (rowIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        RowIndex = rowIndex;
        RowLabel = $"Row {rowIndex + 1}";
    }

    /// <summary>
    /// Gets the zero-based row index.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Gets the human readable row label.
    /// </summary>
    public string RowLabel { get; }

    /// <summary>
    /// Gets or sets the target population in seeds per meter.
    /// </summary>
    public double TargetPopulationPerMeter
    {
        get => _targetPopulationPerMeter;
        private set
        {
            if (SetProperty(ref _targetPopulationPerMeter, value))
            {
                OnPropertyChanged(nameof(TargetPopulationDisplay));
                OnPropertyChanged(nameof(PopulationErrorDisplay));
            }
        }
    }

    /// <summary>
    /// Gets or sets the measured population in seeds per meter.
    /// </summary>
    public double ActualPopulationPerMeter
    {
        get => _actualPopulationPerMeter;
        private set
        {
            if (SetProperty(ref _actualPopulationPerMeter, value))
            {
                OnPropertyChanged(nameof(ActualPopulationDisplay));
                OnPropertyChanged(nameof(PopulationErrorDisplay));
            }
        }
    }

    /// <summary>
    /// Gets or sets the fractional skip rate (0-1).
    /// </summary>
    public double SkipRate
    {
        get => _skipRate;
        private set
        {
            if (SetProperty(ref _skipRate, value))
            {
                OnPropertyChanged(nameof(SkipRateDisplay));
            }
        }
    }

    /// <summary>
    /// Gets or sets the fractional double rate (0-1).
    /// </summary>
    public double DoubleRate
    {
        get => _doubleRate;
        private set
        {
            if (SetProperty(ref _doubleRate, value))
            {
                OnPropertyChanged(nameof(DoubleRateDisplay));
            }
        }
    }

    /// <summary>
    /// Gets or sets the evaluated row quality classification.
    /// </summary>
    public PlanterRowQuality Quality
    {
        get => _quality;
        private set
        {
            if (SetProperty(ref _quality, value))
            {
                OnPropertyChanged(nameof(QualityDisplay));
            }
        }
    }

    /// <summary>
    /// Gets formatted text describing the target population.
    /// </summary>
    public string TargetPopulationDisplay => $"Target: {TargetPopulationPerMeter:0.0}/m";

    /// <summary>
    /// Gets formatted text describing the measured population.
    /// </summary>
    public string ActualPopulationDisplay => $"Actual: {ActualPopulationPerMeter:0.0}/m";

    /// <summary>
    /// Gets formatted text describing the population error.
    /// </summary>
    public string PopulationErrorDisplay
    {
        get
        {
            var delta = ActualPopulationPerMeter - TargetPopulationPerMeter;
            return $"Δ {delta:+0.0;-0.0;0.0}/m";
        }
    }

    /// <summary>
    /// Gets formatted text describing the skip rate percentage.
    /// </summary>
    public string SkipRateDisplay => $"Skips: {SkipRate * 100:0.#}%";

    /// <summary>
    /// Gets formatted text describing the double rate percentage.
    /// </summary>
    public string DoubleRateDisplay => $"Doubles: {DoubleRate * 100:0.#}%";

    /// <summary>
    /// Gets formatted text describing the evaluated quality.
    /// </summary>
    public string QualityDisplay => Quality switch
    {
        PlanterRowQuality.Skip => "Quality: Skip",
        PlanterRowQuality.Double => "Quality: Double",
        PlanterRowQuality.Ok => "Quality: OK",
        PlanterRowQuality.Unknown => "Quality: Unknown",
        _ => $"Quality: {Quality}",
    };

    /// <summary>
    /// Applies a <see cref="PlanterRowStatus"/> snapshot to the view-model.
    /// </summary>
    /// <param name="status">Row status.</param>
    public void ApplyStatus(PlanterRowStatus status)
    {
        TargetPopulationPerMeter = status.TargetPopulationPerMeter;
        ActualPopulationPerMeter = status.ActualPopulationPerMeter;
        SkipRate = status.SkipRate;
        DoubleRate = status.DoubleRate;
        Quality = status.Quality;
    }
}
