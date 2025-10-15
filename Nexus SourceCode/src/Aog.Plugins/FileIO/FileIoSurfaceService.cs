using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Layers;

namespace Aog.Plugins.FileIO;

/// <summary>
/// Provides import/export helpers for agronomic surface layers handled by the File IO plugin.
/// </summary>
public sealed class FileIoSurfaceService
{
    private readonly ExternalAgronomicMapIngestor _ingestor;
    private readonly JsonWriterOptions _writerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileIoSurfaceService"/> class.
    /// </summary>
    /// <param name="ingestor">Optional ingestor used for delimited file imports.</param>
    /// <param name="writerOptions">Optional JSON writer options applied to GeoJSON exports.</param>
    public FileIoSurfaceService(
        ExternalAgronomicMapIngestor? ingestor = null,
        JsonWriterOptions? writerOptions = null)
    {
        _ingestor = ingestor ?? new ExternalAgronomicMapIngestor();
        _writerOptions = writerOptions ?? new JsonWriterOptions
        {
            Indented = true
        };
    }

    /// <summary>
    /// Imports an agronomic surface from a CSV/TSV file using <see cref="ExternalAgronomicMapIngestor"/>.
    /// </summary>
    public AgronomicLayerDocument ImportSurfaceFromDelimitedFile(
        string path,
        string layerId,
        string kind,
        string units,
        double cellSizeMeters,
        string source,
        string transform,
        string createdBy)
    {
        return _ingestor.LoadFromDelimitedFile(
            path,
            layerId,
            kind,
            units,
            cellSizeMeters,
            source,
            transform,
            createdBy);
    }

    /// <summary>
    /// Exports an agronomic surface to a GeoJSON feature collection where each cell becomes a polygon feature.
    /// </summary>
    /// <param name="document">The layer document produced during import or generation.</param>
    /// <param name="path">Destination path for the GeoJSON artifact.</param>
    /// <param name="cancellationToken">Cancellation token honoured while streaming the payload.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the document does not contain any cells.</exception>
    public async Task ExportSurfaceToGeoJsonAsync(
        AgronomicLayerDocument document,
        string path,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Destination path must be provided.", nameof(path));
        }

        if (document.Cells.Count == 0)
        {
            throw new InvalidOperationException("Surface export requires at least one cell.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var boundingBox = ComputeBoundingBox(document.Cells);

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new Utf8JsonWriter(stream, _writerOptions);

        writer.WriteStartObject();
        writer.WriteString("type", "FeatureCollection");
        writer.WriteString("name", document.LayerId);

        writer.WritePropertyName("properties");
        writer.WriteStartObject();
        writer.WriteString("layerId", document.LayerId);
        writer.WriteString("kind", document.Kind);
        writer.WriteString("units", document.Units);
        writer.WriteString("createdAt", document.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        writer.WriteString("createdBy", document.CreatedBy);
        writer.WriteString("provenanceSource", document.Provenance.Source);
        writer.WriteString("provenanceTransform", document.Provenance.Transform);
        writer.WriteString("provenanceHash", document.Provenance.Hash);
        writer.WriteString("provenanceCreatedAt", document.Provenance.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(document.Provenance.Actor))
        {
            writer.WriteString("provenanceActor", document.Provenance.Actor);
        }

        writer.WriteEndObject();

        if (boundingBox is { } bbox)
        {
            writer.WritePropertyName("bbox");
            writer.WriteStartArray();
            writer.WriteNumberValue(bbox.MinX);
            writer.WriteNumberValue(bbox.MinY);
            writer.WriteNumberValue(bbox.MaxX);
            writer.WriteNumberValue(bbox.MaxY);
            writer.WriteEndArray();
        }

        writer.WritePropertyName("features");
        writer.WriteStartArray();

        foreach (var cell in document.Cells)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteStartObject();
            writer.WriteString("type", "Feature");

            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            writer.WriteNumber("value", cell.Value);
            writer.WriteNumber("cellSizeMeters", cell.CellSizeMeters);
            writer.WriteNumber("centerEasting", cell.Position.Easting);
            writer.WriteNumber("centerNorthing", cell.Position.Northing);
            writer.WriteEndObject();

            writer.WritePropertyName("geometry");
            writer.WriteStartObject();
            writer.WriteString("type", "Polygon");
            writer.WritePropertyName("coordinates");
            writer.WriteStartArray();
            writer.WriteStartArray();

            foreach (var (easting, northing) in EnumerateCellPolygon(cell))
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(easting);
                writer.WriteNumberValue(northing);
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static (double MinX, double MinY, double MaxX, double MaxY)? ComputeBoundingBox(
        IReadOnlyList<AgronomicLayerCell> cells)
    {
        if (cells.Count == 0)
        {
            return null;
        }

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        foreach (var cell in cells)
        {
            var half = cell.CellSizeMeters / 2d;
            var minCellX = cell.Position.Easting - half;
            var maxCellX = cell.Position.Easting + half;
            var minCellY = cell.Position.Northing - half;
            var maxCellY = cell.Position.Northing + half;

            if (minCellX < minX)
            {
                minX = minCellX;
            }

            if (minCellY < minY)
            {
                minY = minCellY;
            }

            if (maxCellX > maxX)
            {
                maxX = maxCellX;
            }

            if (maxCellY > maxY)
            {
                maxY = maxCellY;
            }
        }

        return (minX, minY, maxX, maxY);
    }

    private static IEnumerable<(double Easting, double Northing)> EnumerateCellPolygon(AgronomicLayerCell cell)
    {
        var half = cell.CellSizeMeters / 2d;
        var minX = cell.Position.Easting - half;
        var maxX = cell.Position.Easting + half;
        var minY = cell.Position.Northing - half;
        var maxY = cell.Position.Northing + half;

        yield return (minX, minY);
        yield return (maxX, minY);
        yield return (maxX, maxY);
        yield return (minX, maxY);
        yield return (minX, minY);
    }
}
