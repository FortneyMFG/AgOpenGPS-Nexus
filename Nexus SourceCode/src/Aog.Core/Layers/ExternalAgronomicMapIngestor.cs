using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Aog.Core.Paths;

namespace Aog.Core.Layers;

/// <summary>
/// Provides helpers for ingesting external agronomic maps (e.g. prescriptions, yield maps)
/// into the canonical layer representation described by <c>Layer.v1</c>.
/// </summary>
public sealed class ExternalAgronomicMapIngestor
{
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalAgronomicMapIngestor"/> class.
    /// </summary>
    /// <param name="clock">Optional time provider for deterministic testing.</param>
    public ExternalAgronomicMapIngestor(Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Loads a delimited file containing agronomic map values into a layer document.
    /// </summary>
    /// <param name="path">Path to the CSV/TSV file.</param>
    /// <param name="layerId">Layer registry identifier.</param>
    /// <param name="kind">Layer classification (e.g. coverage, rate).</param>
    /// <param name="units">Engineering units for the numeric value.</param>
    /// <param name="cellSizeMeters">Edge length for grid cells.</param>
    /// <param name="source">Source identifier recorded in provenance.</param>
    /// <param name="transform">Transform applied to generate the layer.</param>
    /// <param name="createdBy">Actor producing the import.</param>
    public AgronomicLayerDocument LoadFromDelimitedFile(
        string path,
        string layerId,
        string kind,
        string units,
        double cellSizeMeters,
        string source,
        string transform,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Agronomic map file was not found.", path);
        }

        if (cellSizeMeters <= 0 || double.IsNaN(cellSizeMeters) || double.IsInfinity(cellSizeMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(cellSizeMeters), cellSizeMeters, "Cell size must be a positive finite value.");
        }

        var separator = DetectSeparator(path);
        var cells = new List<AgronomicLayerCell>();

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = trimmed.Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                // Skip header rows or incomplete data.
                if (parts.Any(part => part.Equals("easting", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                throw new InvalidDataException($"Row '{line}' does not contain easting,northing,value.");
            }

            var easting = double.Parse(parts[0], CultureInfo.InvariantCulture);
            var northing = double.Parse(parts[1], CultureInfo.InvariantCulture);
            var value = double.Parse(parts[2], CultureInfo.InvariantCulture);

            cells.Add(new AgronomicLayerCell(new PlanarPoint(easting, northing), cellSizeMeters, value));
        }

        if (cells.Count == 0)
        {
            throw new InvalidDataException("Agronomic map did not contain any samples.");
        }

        var timestamp = _clock();
        var hash = ComputeHash(layerId, cells);
        var provenance = new LayerProvenance(source, transform, hash, timestamp, createdBy);

        return new AgronomicLayerDocument(layerId, kind, units, timestamp, createdBy, cells, provenance);
    }

    private static char DetectSeparator(string path)
    {
        using var reader = new StreamReader(path);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            if (line.Contains('\t'))
            {
                return '\t';
            }

            if (line.Contains(';'))
            {
                return ';';
            }

            if (line.Contains(','))
            {
                return ',';
            }
        }

        return ',';
    }

    private static string ComputeHash(string layerId, IReadOnlyList<AgronomicLayerCell> cells)
    {
        using var sha = SHA256.Create();
        var builder = new StringBuilder();
        builder.Append(layerId);
        builder.Append('|');
        foreach (var cell in cells
            .OrderBy(c => c.Position.Easting)
            .ThenBy(c => c.Position.Northing)
            .ThenBy(c => c.CellSizeMeters))
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:F3},{1:F3},{2:G17},{3:G17};",
                cell.Position.Easting,
                cell.Position.Northing,
                cell.CellSizeMeters,
                cell.Value);
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}

/// <summary>
/// Canonical representation of an agronomic layer.
/// </summary>
public sealed class AgronomicLayerDocument
{
    public AgronomicLayerDocument(
        string layerId,
        string kind,
        string units,
        DateTimeOffset createdAt,
        string createdBy,
        IReadOnlyList<AgronomicLayerCell> cells,
        LayerProvenance provenance)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Layer kind is required.", nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(units))
        {
            throw new ArgumentException("Units are required.", nameof(units));
        }

        LayerId = layerId;
        Kind = kind;
        Units = units;
        CreatedAt = createdAt;
        CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system:nexus" : createdBy;
        Cells = new ReadOnlyCollection<AgronomicLayerCell>(cells ?? Array.Empty<AgronomicLayerCell>());
        Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
    }

    public string LayerId { get; }

    public string Kind { get; }

    public string Units { get; }

    public DateTimeOffset CreatedAt { get; }

    public string CreatedBy { get; }

    public IReadOnlyList<AgronomicLayerCell> Cells { get; }

    public LayerProvenance Provenance { get; }
}

/// <summary>
/// Represents a single agronomic layer cell imported from an external source.
/// </summary>
/// <param name="Position">Cell centre in planar coordinates.</param>
/// <param name="CellSizeMeters">Edge length of the cell in metres.</param>
/// <param name="Value">Numeric value carried by the cell.</param>
public sealed record AgronomicLayerCell(PlanarPoint Position, double CellSizeMeters, double Value);

/// <summary>
/// Describes provenance metadata associated with an agronomic layer.
/// </summary>
public sealed class LayerProvenance
{
    public LayerProvenance(string source, string transform, string hash, DateTimeOffset createdAt, string actor)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source must be provided.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(transform))
        {
            throw new ArgumentException("Transform must be provided.", nameof(transform));
        }

        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Hash must be provided.", nameof(hash));
        }

        Source = source;
        Transform = transform;
        Hash = hash;
        CreatedAt = createdAt;
        Actor = actor;
    }

    public string Source { get; }

    public string Transform { get; }

    public string Hash { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Actor { get; }
}
