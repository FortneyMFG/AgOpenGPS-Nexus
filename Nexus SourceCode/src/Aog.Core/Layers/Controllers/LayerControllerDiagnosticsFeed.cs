using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Generates high level controller diagnostics from <see cref="LayerControllerSnapshot"/> instances.
/// This feed surfaces health, quality, and ratio metrics that plugins and operator dashboards consume
/// while evaluating controller behaviour. Implements NX-218.
/// </summary>
public sealed class LayerControllerDiagnosticsFeed
{
    private static readonly IReadOnlyDictionary<string, double> EmptyMetrics =
        new ReadOnlyDictionary<string, double>(new Dictionary<string, double>());

    private readonly LayerControllerDiagnosticsFeedOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayerControllerDiagnosticsFeed"/> class.
    /// </summary>
    /// <param name="options">Optional configuration overriding default quality thresholds.</param>
    public LayerControllerDiagnosticsFeed(LayerControllerDiagnosticsFeedOptions? options = null)
    {
        _options = options ?? LayerControllerDiagnosticsFeedOptions.Default;
    }

    /// <summary>
    /// Produces a diagnostic record describing the supplied snapshot.
    /// </summary>
    /// <param name="snapshot">Snapshot produced by the controller runtime.</param>
    /// <param name="clock">Optional clock used to evaluate staleness. When omitted the snapshot timestamp is used.</param>
    /// <returns>A diagnostic record containing health and summary metrics.</returns>
    public LayerControllerDiagnostic CreateDiagnostic(
        LayerControllerSnapshot snapshot,
        TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var diagnosticClock = clock ?? TimeProvider.System;
        var now = diagnosticClock.GetUtcNow();
        var lastSampleTimestamp = snapshot.LastSampleTimestamp ?? snapshot.SnapshotTimestamp;
        var timeSinceLastSample = now - lastSampleTimestamp;
        if (timeSinceLastSample < TimeSpan.Zero)
        {
            timeSinceLastSample = TimeSpan.Zero;
        }

        var metrics = BuildMetrics(snapshot);

        var status = ControllerDiagnosticStatus.Nominal;
        string? reason = null;

        if (snapshot.RateUnavailable)
        {
            status = ControllerDiagnosticStatus.Fault;
            reason = "Rate unavailable";
        }
        else if (!snapshot.ContainsFreshData)
        {
            if (timeSinceLastSample >= _options.StaleAfter)
            {
                status = ControllerDiagnosticStatus.Warning;
                reason = "No fresh samples";
            }
            else
            {
                status = ControllerDiagnosticStatus.Warning;
                reason = "Hold frame";
            }
        }

        if (snapshot.Quality <= _options.FaultQualityThreshold)
        {
            status = ControllerDiagnosticStatus.Fault;
            reason = "Quality below fault threshold";
        }
        else if (status != ControllerDiagnosticStatus.Fault && snapshot.Quality < _options.WarningQualityThreshold)
        {
            status = ControllerDiagnosticStatus.Warning;
            reason = "Quality below warning threshold";
        }

        var ratio = double.IsFinite(snapshot.Denominator) && snapshot.Denominator != 0d
            ? snapshot.Numerator / snapshot.Denominator
            : double.NaN;

        return new LayerControllerDiagnostic(
            snapshot.ControllerId,
            snapshot.LayerId,
            snapshot.SnapshotTimestamp,
            snapshot.ContainsFreshData,
            snapshot.RateUnavailable,
            snapshot.NormalizedValue,
            snapshot.EngineeringValue,
            snapshot.Quality,
            snapshot.AreaSquareMeters,
            snapshot.MinimumValue,
            snapshot.MaximumValue,
            snapshot.SampleCount,
            snapshot.Numerator,
            snapshot.Denominator,
            ratio,
            timeSinceLastSample,
            status,
            reason,
            metrics);
    }

    private static IReadOnlyDictionary<string, double> BuildMetrics(LayerControllerSnapshot snapshot)
    {
        if (!snapshot.ContainsFreshData && snapshot.SampleCount == 0 && snapshot.AreaSquareMeters == 0d)
        {
            return EmptyMetrics;
        }

        var metrics = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["normalized"] = snapshot.NormalizedValue,
            ["engineering"] = snapshot.EngineeringValue,
            ["quality"] = snapshot.Quality,
            ["areaSqM"] = snapshot.AreaSquareMeters,
            ["minimum"] = snapshot.MinimumValue,
            ["maximum"] = snapshot.MaximumValue,
            ["sampleCount"] = snapshot.SampleCount,
            ["numerator"] = snapshot.Numerator,
            ["denominator"] = snapshot.Denominator,
        };

        return new ReadOnlyDictionary<string, double>(metrics);
    }
}

/// <summary>
/// Immutable diagnostic summary produced by <see cref="LayerControllerDiagnosticsFeed"/>.
/// </summary>
public sealed record LayerControllerDiagnostic(
    string ControllerId,
    string LayerId,
    DateTimeOffset CapturedAt,
    bool ContainsFreshData,
    bool RateUnavailable,
    double NormalizedValue,
    double EngineeringValue,
    double Quality,
    double AreaSquareMeters,
    double MinimumValue,
    double MaximumValue,
    int SampleCount,
    double Numerator,
    double Denominator,
    double Ratio,
    TimeSpan TimeSinceLastSample,
    ControllerDiagnosticStatus Status,
    string? StatusReason,
    IReadOnlyDictionary<string, double> Metrics);

/// <summary>
/// Represents coarse controller health states surfaced to operator dashboards.
/// </summary>
public enum ControllerDiagnosticStatus
{
    /// <summary>Controller is operating normally.</summary>
    Nominal = 0,

    /// <summary>Controller is producing results but requires operator attention.</summary>
    Warning = 1,

    /// <summary>Controller output is invalid or unavailable.</summary>
    Fault = 2,
}

/// <summary>
/// Configuration for <see cref="LayerControllerDiagnosticsFeed"/>.
/// </summary>
/// <param name="WarningQualityThreshold">Quality values below this threshold surface a warning.</param>
/// <param name="FaultQualityThreshold">Quality values below this threshold mark the controller as faulted.</param>
/// <param name="StaleAfter">Duration without fresh samples before the controller is marked stale.</param>
public sealed record LayerControllerDiagnosticsFeedOptions(
    double WarningQualityThreshold,
    double FaultQualityThreshold,
    TimeSpan StaleAfter)
{
    /// <summary>Default diagnostic configuration used when no options are supplied.</summary>
    public static LayerControllerDiagnosticsFeedOptions Default { get; } = new(
        WarningQualityThreshold: 0.65,
        FaultQualityThreshold: 0.35,
        StaleAfter: TimeSpan.FromSeconds(2));
}
