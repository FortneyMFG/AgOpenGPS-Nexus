using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aog.Core.Paths;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Persists controller snapshots into TileStore oriented records, wiring diagnostics surfaced by
/// <see cref="LayerControllerDiagnosticsFeed"/>. Implements NX-219.
/// </summary>
public sealed class LayerControllerTileStoreWriter
{
    private readonly ILayerControllerTileSink _sink;
    private readonly LayerControllerDiagnosticsFeed _diagnosticsFeed;
    private readonly LayerControllerTileStoreWriterOptions _options;
    private readonly Dictionary<string, int> _sequenceByKey = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="LayerControllerTileStoreWriter"/> class.
    /// </summary>
    /// <param name="sink">Sink that accepts generated tiles.</param>
    /// <param name="diagnosticsFeed">Optional diagnostics feed used when computing health state.</param>
    /// <param name="options">Optional configuration overriding tile identifier rules.</param>
    public LayerControllerTileStoreWriter(
        ILayerControllerTileSink sink,
        LayerControllerDiagnosticsFeed? diagnosticsFeed = null,
        LayerControllerTileStoreWriterOptions? options = null)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _diagnosticsFeed = diagnosticsFeed ?? new LayerControllerDiagnosticsFeed();
        _options = options ?? LayerControllerTileStoreWriterOptions.Default;
    }

    /// <summary>
    /// Materialises a <see cref="LayerControllerTile"/> from a snapshot and forwards it to the sink.
    /// </summary>
    /// <param name="snapshot">Snapshot produced by the controller runtime.</param>
    /// <param name="clock">Optional clock used for diagnostics staleness evaluation.</param>
    /// <returns>The generated tile record.</returns>
    public LayerControllerTile WriteSnapshot(LayerControllerSnapshot snapshot, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var diagnostic = _diagnosticsFeed.CreateDiagnostic(snapshot, clock);
        var tileId = CreateTileId(snapshot);

        var positions = snapshot.Positions.Count == 0
            ? Array.Empty<PlanarPoint>()
            : snapshot.Positions.ToArray();

        var metrics = diagnostic.Metrics.Count == 0
            ? diagnostic.Metrics
            : new Dictionary<string, double>(diagnostic.Metrics, StringComparer.Ordinal);

        var tile = new LayerControllerTile(
            tileId,
            snapshot.ControllerId,
            snapshot.LayerId,
            snapshot.SnapshotTimestamp,
            diagnostic.Status,
            diagnostic.StatusReason,
            diagnostic.ContainsFreshData,
            diagnostic.RateUnavailable,
            diagnostic.NormalizedValue,
            diagnostic.EngineeringValue,
            diagnostic.Quality,
            diagnostic.AreaSquareMeters,
            diagnostic.MinimumValue,
            diagnostic.MaximumValue,
            diagnostic.SampleCount,
            diagnostic.Numerator,
            diagnostic.Denominator,
            diagnostic.Ratio,
            diagnostic.TimeSinceLastSample,
            positions,
            metrics);

        _sink.Append(tile);
        return tile;
    }

    private string CreateTileId(LayerControllerSnapshot snapshot)
    {
        var sanitizedController = Sanitize(snapshot.ControllerId);
        var timestamp = snapshot.SnapshotTimestamp.ToUniversalTime()
            .ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
        var key = sanitizedController + "|" + timestamp;
        var sequence = _sequenceByKey.TryGetValue(key, out var current)
            ? ++current
            : 0;
        _sequenceByKey[key] = sequence;
        return FormattableString.Invariant(
            $"{_options.TilePrefix}:{sanitizedController}:{timestamp}-{sequence:D2}");
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "controller";
        }

        Span<char> buffer = stackalloc char[value.Length];
        var index = 0;
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch) || ch is '.' or '_' or ':' or '/' or '-')
            {
                buffer[index++] = ch;
            }
            else
            {
                buffer[index++] = '-';
            }
        }

        return new string(buffer[..index]);
    }
}

/// <summary>
/// Tile record representing controller output persisted to the TileStore.
/// </summary>
public sealed class LayerControllerTile
{
    /// <summary>Initializes a new instance of the <see cref="LayerControllerTile"/> class.</summary>
    public LayerControllerTile(
        string tileId,
        string controllerId,
        string layerId,
        DateTimeOffset capturedAt,
        ControllerDiagnosticStatus status,
        string? statusReason,
        bool containsFreshData,
        bool rateUnavailable,
        double normalizedValue,
        double engineeringValue,
        double quality,
        double areaSquareMeters,
        double minimumValue,
        double maximumValue,
        int sampleCount,
        double numerator,
        double denominator,
        double ratio,
        TimeSpan timeSinceLastSample,
        IReadOnlyList<PlanarPoint> positions,
        IReadOnlyDictionary<string, double> metrics)
    {
        TileId = tileId ?? throw new ArgumentNullException(nameof(tileId));
        ControllerId = controllerId ?? throw new ArgumentNullException(nameof(controllerId));
        LayerId = layerId ?? throw new ArgumentNullException(nameof(layerId));
        CapturedAt = capturedAt;
        Status = status;
        StatusReason = statusReason;
        ContainsFreshData = containsFreshData;
        RateUnavailable = rateUnavailable;
        NormalizedValue = normalizedValue;
        EngineeringValue = engineeringValue;
        Quality = quality;
        AreaSquareMeters = areaSquareMeters;
        MinimumValue = minimumValue;
        MaximumValue = maximumValue;
        SampleCount = sampleCount;
        Numerator = numerator;
        Denominator = denominator;
        Ratio = ratio;
        TimeSinceLastSample = timeSinceLastSample;
        Positions = positions ?? throw new ArgumentNullException(nameof(positions));
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <summary>Gets the tile identifier following the <c>tile:*</c> convention.</summary>
    public string TileId { get; }

    /// <summary>Gets the controller identifier that produced the tile.</summary>
    public string ControllerId { get; }

    /// <summary>Gets the layer identifier associated with the tile.</summary>
    public string LayerId { get; }

    /// <summary>Gets the timestamp when the tile was captured.</summary>
    public DateTimeOffset CapturedAt { get; }

    /// <summary>Gets the diagnostic status derived from the controller snapshot.</summary>
    public ControllerDiagnosticStatus Status { get; }

    /// <summary>Gets a human-readable reason describing the diagnostic status when present.</summary>
    public string? StatusReason { get; }

    /// <summary>Gets a value indicating whether fresh data was observed during the emission window.</summary>
    public bool ContainsFreshData { get; }

    /// <summary>Gets a value indicating whether the controller reported a rate unavailable state.</summary>
    public bool RateUnavailable { get; }

    /// <summary>Gets the normalized controller value.</summary>
    public double NormalizedValue { get; }

    /// <summary>Gets the engineering controller value.</summary>
    public double EngineeringValue { get; }

    /// <summary>Gets the aggregated quality metric.</summary>
    public double Quality { get; }

    /// <summary>Gets the area covered by the tile in square metres.</summary>
    public double AreaSquareMeters { get; }

    /// <summary>Gets the minimum engineering value observed.</summary>
    public double MinimumValue { get; }

    /// <summary>Gets the maximum engineering value observed.</summary>
    public double MaximumValue { get; }

    /// <summary>Gets the number of samples included in the tile.</summary>
    public int SampleCount { get; }

    /// <summary>Gets the accumulated numerator used for derived ratios.</summary>
    public double Numerator { get; }

    /// <summary>Gets the accumulated denominator used for derived ratios.</summary>
    public double Denominator { get; }

    /// <summary>Gets the computed ratio using the numerator/denominator pair when valid.</summary>
    public double Ratio { get; }

    /// <summary>Gets the elapsed time since the last sample was ingested.</summary>
    public TimeSpan TimeSinceLastSample { get; }

    /// <summary>Gets the captured positions contributing to the tile.</summary>
    public IReadOnlyList<PlanarPoint> Positions { get; }

    /// <summary>Gets additional metrics exposed to diagnostic dashboards.</summary>
    public IReadOnlyDictionary<string, double> Metrics { get; }
}

/// <summary>
/// Sink contract for TileStore writers.
/// </summary>
public interface ILayerControllerTileSink
{
    /// <summary>Appends the supplied tile to the persistence boundary.</summary>
    /// <param name="tile">Tile to append.</param>
    void Append(LayerControllerTile tile);
}

/// <summary>
/// Options controlling <see cref="LayerControllerTileStoreWriter"/> behaviour.
/// </summary>
/// <param name="TilePrefix">Prefix used when generating tile identifiers.</param>
public sealed record LayerControllerTileStoreWriterOptions(string TilePrefix)
{
    /// <summary>Default options that follow the <c>tile:</c> naming convention.</summary>
    public static LayerControllerTileStoreWriterOptions Default { get; } = new("tile");
}
