using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Describes a telemetry series rendered inside a metadata-driven dashboard widget.
/// </summary>
public sealed class DashboardSeriesViewModel : ObservableObject
{
    private IReadOnlyList<double> _values = Array.Empty<double>();

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardSeriesViewModel"/> class.
    /// </summary>
    /// <param name="id">Stable identifier for the series.</param>
    /// <param name="title">Human readable title displayed in the dashboard.</param>
    /// <param name="units">Units used when describing the series.</param>
    /// <param name="stroke">Primary stroke brush for the sparkline.</param>
    /// <param name="fill">Optional fill brush rendered below the line.</param>
    /// <param name="strokeThickness">Stroke thickness for the sparkline.</param>
    public DashboardSeriesViewModel(
        string id,
        string title,
        string units,
        IBrush stroke,
        IBrush? fill = null,
        double strokeThickness = 2.5)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Units = units ?? throw new ArgumentNullException(nameof(units));
        Stroke = stroke ?? throw new ArgumentNullException(nameof(stroke));
        Fill = fill;
        StrokeThickness = strokeThickness;
    }

    /// <summary>Gets the stable identifier for the series.</summary>
    public string Id { get; }

    /// <summary>Gets the human readable title displayed in the dashboard.</summary>
    public string Title { get; }

    /// <summary>Gets the units used when describing the series.</summary>
    public string Units { get; }

    /// <summary>Gets the stroke brush used when rendering the series.</summary>
    public IBrush Stroke { get; }

    /// <summary>Gets the optional fill brush rendered below the sparkline.</summary>
    public IBrush? Fill { get; }

    /// <summary>Gets the stroke thickness used by the sparkline.</summary>
    public double StrokeThickness { get; }

    /// <summary>Gets the values rendered inside the sparkline.</summary>
    public IReadOnlyList<double> Values
    {
        get => _values;
        private set => SetProperty(ref _values, value);
    }

    /// <summary>
    /// Replaces the current series with the provided historical values.
    /// </summary>
    /// <param name="samples">Samples displayed by the sparkline.</param>
    public void SetValues(IEnumerable<double> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        Values = samples.ToArray();
    }
}
