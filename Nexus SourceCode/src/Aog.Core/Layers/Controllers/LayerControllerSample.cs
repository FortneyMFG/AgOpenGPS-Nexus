using System;
using Aog.Core.Paths;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Represents a single sensor sample ingested by a layer controller.
/// </summary>
public readonly record struct LayerControllerSample
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayerControllerSample"/> struct.
    /// </summary>
    /// <param name="timestamp">Timestamp when the sample was captured.</param>
    /// <param name="position">Position of the section/row in planar coordinates.</param>
    /// <param name="engineeringValue">Engineering value reported by the sensor.</param>
    /// <param name="normalizedValue">Normalized value (0–1) derived from the engineering sample.</param>
    /// <param name="quality">Quality metric (0–1) describing confidence in the sample.</param>
    /// <param name="rateUnavailable">Indicates that the controller should surface a rate-unavailable state.</param>
    /// <param name="areaSquareMeters">Effective area represented by the sample in square meters.</param>
    /// <param name="engineeringContribution">Optional precomputed engineering contribution used for sum aggregations.</param>
    /// <param name="numeratorContribution">Optional numerator increment for ratio-based diagnostics.</param>
    /// <param name="denominatorContribution">Optional denominator increment for ratio-based diagnostics.</param>
    public LayerControllerSample(
        DateTimeOffset timestamp,
        PlanarPoint position,
        double engineeringValue,
        double normalizedValue,
        double quality,
        bool rateUnavailable,
        double areaSquareMeters,
        double? engineeringContribution = null,
        double? numeratorContribution = null,
        double? denominatorContribution = null)
    {
        if (!double.IsFinite(engineeringValue))
        {
            throw new ArgumentOutOfRangeException(nameof(engineeringValue), engineeringValue, "Engineering value must be finite.");
        }

        if (!double.IsFinite(normalizedValue))
        {
            throw new ArgumentOutOfRangeException(nameof(normalizedValue), normalizedValue, "Normalized value must be finite.");
        }

        if (!double.IsFinite(quality))
        {
            throw new ArgumentOutOfRangeException(nameof(quality), quality, "Quality must be finite.");
        }

        if (areaSquareMeters < 0 || double.IsNaN(areaSquareMeters) || double.IsInfinity(areaSquareMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(areaSquareMeters), areaSquareMeters, "Area must be a finite non-negative value.");
        }

        if (engineeringContribution.HasValue && !double.IsFinite(engineeringContribution.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(engineeringContribution), engineeringContribution, "Engineering contribution must be finite when specified.");
        }

        if (numeratorContribution.HasValue && !double.IsFinite(numeratorContribution.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(numeratorContribution), numeratorContribution, "Numerator contribution must be finite when specified.");
        }

        if (denominatorContribution.HasValue && !double.IsFinite(denominatorContribution.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(denominatorContribution), denominatorContribution, "Denominator contribution must be finite when specified.");
        }

        Timestamp = timestamp;
        Position = position;
        EngineeringValue = engineeringValue;
        NormalizedValue = Math.Clamp(normalizedValue, 0d, 1d);
        Quality = Math.Clamp(quality, 0d, 1d);
        RateUnavailable = rateUnavailable;
        AreaSquareMeters = areaSquareMeters;
        EngineeringContribution = engineeringContribution;
        NumeratorContribution = numeratorContribution;
        DenominatorContribution = denominatorContribution;
    }

    /// <summary>
    /// Gets the timestamp when the sample was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the section/row position for the sample.
    /// </summary>
    public PlanarPoint Position { get; }

    /// <summary>
    /// Gets the engineering value reported by the sensor.
    /// </summary>
    public double EngineeringValue { get; }

    /// <summary>
    /// Gets the normalized value (0–1) derived from the engineering value.
    /// </summary>
    public double NormalizedValue { get; }

    /// <summary>
    /// Gets the quality metric (0–1) associated with the sample.
    /// </summary>
    public double Quality { get; }

    /// <summary>
    /// Gets a value indicating whether the controller should surface a rate-unavailable state.
    /// </summary>
    public bool RateUnavailable { get; }

    /// <summary>
    /// Gets the effective area represented by the sample in square meters.
    /// </summary>
    public double AreaSquareMeters { get; }

    /// <summary>
    /// Gets the precomputed engineering contribution used for <see cref="LayerAggregationStrategy.Sum"/> when specified.
    /// </summary>
    public double? EngineeringContribution { get; }

    /// <summary>
    /// Gets the optional numerator increment for ratio-based diagnostics.
    /// </summary>
    public double? NumeratorContribution { get; }

    /// <summary>
    /// Gets the optional denominator increment for ratio-based diagnostics.
    /// </summary>
    public double? DenominatorContribution { get; }

    /// <summary>
    /// Computes the effective area represented by an ingestion slice.
    /// </summary>
    /// <param name="groundSpeedMetersPerSecond">Ground speed in metres per second.</param>
    /// <param name="duration">Duration of the sample.</param>
    /// <param name="sectionWidthMeters">Section or row width in metres.</param>
    /// <param name="coverageFactor">Coverage factor [0,1] indicating overlap/skip weighting.</param>
    /// <returns>The computed area in square metres.</returns>
    public static double ComputeAreaSlice(
        double groundSpeedMetersPerSecond,
        TimeSpan duration,
        double sectionWidthMeters,
        double coverageFactor)
    {
        if (duration <= TimeSpan.Zero || double.IsNaN(duration.TotalSeconds) || double.IsInfinity(duration.TotalSeconds))
        {
            return 0d;
        }

        var speed = double.IsNaN(groundSpeedMetersPerSecond) || double.IsInfinity(groundSpeedMetersPerSecond)
            ? 0d
            : Math.Max(0d, groundSpeedMetersPerSecond);
        var width = double.IsNaN(sectionWidthMeters) || double.IsInfinity(sectionWidthMeters)
            ? 0d
            : Math.Max(0d, sectionWidthMeters);
        var factor = double.IsNaN(coverageFactor) || double.IsInfinity(coverageFactor)
            ? 0d
            : Math.Clamp(coverageFactor, 0d, 1d);

        return speed * duration.TotalSeconds * width * factor;
    }
}
