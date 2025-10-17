using System;
using System.Collections.Generic;

namespace Aog.Plugins.Sections;

/// <summary>
/// Computes bit masks for section control based on coverage demand, vehicle speed,
/// and configurable look-ahead heuristics.
/// </summary>
public sealed class SectionMaskCalculator
{
    /// <summary>
    /// Maximum number of supported boom sections.
    /// </summary>
    public const int MaxSectionCount = 8;

    private readonly int _sectionCount;
    private readonly double _minimumSpeedMps;
    private readonly double _lookAheadSeconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionMaskCalculator"/> class.
    /// </summary>
    /// <param name="sectionCount">Number of controlled sections (1-8).</param>
    /// <param name="minimumSpeedMps">Speed gate threshold in meters per second.</param>
    /// <param name="lookAheadSeconds">Look-ahead horizon in seconds.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when configuration values are outside valid ranges.</exception>
    public SectionMaskCalculator(int sectionCount, double minimumSpeedMps, double lookAheadSeconds)
    {
        if (sectionCount is < 1 or > MaxSectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(sectionCount), sectionCount, $"Section count must be between 1 and {MaxSectionCount} inclusive.");
        }

        if (double.IsNaN(minimumSpeedMps) || double.IsInfinity(minimumSpeedMps) || minimumSpeedMps < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSpeedMps), minimumSpeedMps, "Minimum speed must be a finite, non-negative value.");
        }

        if (double.IsNaN(lookAheadSeconds) || double.IsInfinity(lookAheadSeconds) || lookAheadSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lookAheadSeconds), lookAheadSeconds, "Look-ahead must be a finite, non-negative value.");
        }

        _sectionCount = sectionCount;
        _minimumSpeedMps = minimumSpeedMps;
        _lookAheadSeconds = lookAheadSeconds;
    }

    /// <summary>
    /// Computes the section activation mask for the supplied state snapshot.
    /// </summary>
    /// <param name="speedMps">Current vehicle speed (m/s).</param>
    /// <param name="sections">Per-section coverage observations.</param>
    /// <returns>A packed bit mask where bit N corresponds to section N (0-based).</returns>
    public uint ComputeMask(double speedMps, IReadOnlyList<SectionObservation> sections)
    {
        if (sections is null)
        {
            throw new ArgumentNullException(nameof(sections));
        }

        if (double.IsNaN(speedMps) || double.IsInfinity(speedMps) || speedMps < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(speedMps), speedMps, "Speed must be a finite, non-negative value.");
        }

        if (sections.Count != _sectionCount)
        {
            throw new ArgumentException($"Exactly {_sectionCount} sections are required.", nameof(sections));
        }

        if (speedMps < _minimumSpeedMps)
        {
            return 0;
        }

        var lookAheadDistance = speedMps * _lookAheadSeconds;
        var mask = 0u;

        for (var index = 0; index < sections.Count; index++)
        {
            var section = sections[index];
            if (section.IsSuppressed)
            {
                continue;
            }

            var shouldEnable = section.HasCoverage;

            if (!shouldEnable && section.DistanceToCoverageStartMeters.HasValue)
            {
                var distance = section.DistanceToCoverageStartMeters.Value;
                if (distance < 0)
                {
                    distance = 0;
                }

                if (distance <= lookAheadDistance)
                {
                    shouldEnable = true;
                }
            }

            if (shouldEnable)
            {
                mask |= 1u << index;
            }
        }

        return mask;
    }
}

/// <summary>
/// Represents a single section's coverage observation used to compute a mask.
/// </summary>
/// <remarks>
/// <para><see cref="HasCoverage"/> indicates whether coverage is currently required under the boom.</para>
/// <para><see cref="DistanceToCoverageStartMeters"/> is the distance to the next coverage boundary ahead of the vehicle. Null when unknown.</para>
/// <para><see cref="IsSuppressed"/> prevents the section from activating (e.g. manual override, boundary conditions).</para>
/// </remarks>
public readonly record struct SectionObservation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SectionObservation"/> struct.
    /// </summary>
    /// <param name="hasCoverage">True when coverage is currently required under the boom.</param>
    /// <param name="distanceToCoverageStartMeters">Distance to the next coverage boundary ahead of the vehicle. Null when unknown.</param>
    /// <param name="isSuppressed">True to prevent the section from activating (e.g. manual override, boundary conditions).</param>
    public SectionObservation(bool hasCoverage, double? distanceToCoverageStartMeters, bool isSuppressed = false)
    {
        if (distanceToCoverageStartMeters.HasValue)
        {
            var value = distanceToCoverageStartMeters.Value;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(distanceToCoverageStartMeters), value, "Distance must be a finite value when provided.");
            }
        }

        HasCoverage = hasCoverage;
        DistanceToCoverageStartMeters = distanceToCoverageStartMeters;
        IsSuppressed = isSuppressed;
    }

    /// <summary>
    /// Gets a value indicating whether coverage is currently required.
    /// </summary>
    public bool HasCoverage { get; }

    /// <summary>
    /// Gets the distance to the next coverage region start measured along the vehicle path.
    /// Null when no forward prediction is available.
    /// </summary>
    public double? DistanceToCoverageStartMeters { get; }

    /// <summary>
    /// Gets a value indicating whether the section is suppressed by higher level logic.
    /// </summary>
    public bool IsSuppressed { get; }
}
