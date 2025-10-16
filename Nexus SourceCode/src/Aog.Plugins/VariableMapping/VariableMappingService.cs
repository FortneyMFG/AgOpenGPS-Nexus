using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Aog.Core.Layers;
using Aog.Core.Layers.Controllers;
using Aog.Core.Paths;
using Aog.Plugins.Sections;

namespace Aog.Plugins.VariableMapping;

/// <summary>
/// Provides ingestion and sampling services for variable rate prescriptions.
/// </summary>
public sealed class VariableMappingService
{
    private const string PlannedLayerPrefix = "vr.planned.";
    private const string LayerKind = "variable-rate";
    private const string Source = "plugin:variable-mapping";

    private readonly ExternalAgronomicMapIngestor _ingestor;
    private readonly PrescriptionExportPipeline _exportPipeline;
    private readonly Dictionary<string, AgronomicLayerDocument> _plannedLayers = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableMappingService"/> class.
    /// </summary>
    /// <param name="ingestor">Agronomic map ingestor used when importing grid files.</param>
    public VariableMappingService(
        ExternalAgronomicMapIngestor ingestor,
        PrescriptionExportPipeline? exportPipeline = null)
    {
        _ingestor = ingestor ?? throw new ArgumentNullException(nameof(ingestor));
        _exportPipeline = exportPipeline ?? new PrescriptionExportPipeline();
    }

    /// <summary>
    /// Imports a prescription into the planned layer catalogue.
    /// </summary>
    /// <param name="recipeId">Identifier of the prescription recipe.</param>
    /// <param name="filePath">Path to the agronomic grid file.</param>
    /// <param name="cellSizeMeters">Grid cell edge length in metres.</param>
    /// <param name="units">Engineering units for the prescription values.</param>
    /// <param name="createdBy">Actor responsible for the import.</param>
    /// <param name="regionOfInterest">Optional mask layer restricting the planned output.</param>
    public AgronomicLayerDocument ImportPrescription(
        string recipeId,
        string filePath,
        double cellSizeMeters,
        string units,
        string? createdBy = null,
        AgronomicLayerDocument? regionOfInterest = null)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            throw new ArgumentException("Recipe identifier is required.", nameof(recipeId));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(units))
        {
            throw new ArgumentException("Units are required.", nameof(units));
        }

        if (cellSizeMeters <= 0 || double.IsNaN(cellSizeMeters) || double.IsInfinity(cellSizeMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(cellSizeMeters));
        }

        var layerId = PlannedLayerPrefix + recipeId.Trim();
        var transform = BuildTransform(recipeId, regionOfInterest?.LayerId);
        var actor = string.IsNullOrWhiteSpace(createdBy) ? Source : createdBy.Trim();

        var plannedLayer = _ingestor.LoadFromDelimitedFile(
            filePath,
            layerId,
            LayerKind,
            units,
            cellSizeMeters,
            Source,
            transform,
            actor);

        if (regionOfInterest is not null)
        {
            plannedLayer = ClipToRegionOfInterest(plannedLayer, regionOfInterest);
        }

        lock (_sync)
        {
            _plannedLayers[layerId] = plannedLayer;
        }

        return plannedLayer;
    }

    /// <summary>
    /// Attempts to retrieve a previously imported planned layer.
    /// </summary>
    /// <param name="layerId">Layer registry identifier.</param>
    /// <param name="layer">Resolved layer when found.</param>
    public bool TryGetPlannedLayer(string layerId, out AgronomicLayerDocument layer)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        lock (_sync)
        {
            if (_plannedLayers.TryGetValue(layerId, out var resolved))
            {
                layer = resolved;
                return true;
            }
        }

        layer = null!;
        return false;
    }

    /// <summary>
    /// Computes section setpoints using a planned layer.
    /// </summary>
    /// <param name="layerId">Layer identifier that contains planned rates.</param>
    /// <param name="controller">Controller used to translate layer values into setpoints.</param>
    /// <param name="sections">Section placements consuming the layer.</param>
    public IReadOnlyList<double> ComputeSetpoints(
        string layerId,
        VariableRateController controller,
        IReadOnlyList<SectionPlacement> sections,
        VariableRateTransportGuard? transportGuard = null,
        DateTimeOffset? timestampUtc = null)
    {
        if (controller is null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (sections is null)
        {
            throw new ArgumentNullException(nameof(sections));
        }

        if (!TryGetPlannedLayer(layerId, out var layer))
        {
            throw new InvalidOperationException($"Layer '{layerId}' has not been imported.");
        }

        return controller.ComputeRates(sections, layer, transportGuard: transportGuard, timestampUtc: timestampUtc);
    }

    /// <summary>
    /// Exports a previously imported prescription to ISOXML and manifest artifacts.
    /// </summary>
    /// <param name="layerId">Identifier of the planned layer to export.</param>
    /// <param name="jobId">Job identifier recorded in the manifest.</param>
    /// <param name="sessionId">Session identifier recorded in the manifest.</param>
    /// <param name="recipeId">Prescription recipe identifier recorded in the manifest.</param>
    /// <param name="operatorId">Optional operator identifier recorded in the manifest.</param>
    public PrescriptionExportArtifacts ExportPrescription(
        string layerId,
        string jobId,
        string sessionId,
        string recipeId,
        string? operatorId = null)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        if (!TryGetPlannedLayer(layerId, out var layer))
        {
            throw new InvalidOperationException($"Layer '{layerId}' has not been imported.");
        }

        return _exportPipeline.Export(layer, jobId, sessionId, recipeId, operatorId);
    }

    private static string BuildTransform(string recipeId, string? regionLayerId)
    {
        var roiToken = string.IsNullOrWhiteSpace(regionLayerId) ? "none" : regionLayerId;
        return $"recipe:{recipeId.Trim()};roi:{roiToken}";
    }

    private static AgronomicLayerDocument ClipToRegionOfInterest(
        AgronomicLayerDocument plannedLayer,
        AgronomicLayerDocument regionOfInterest)
    {
        if (regionOfInterest.Cells.Count == 0)
        {
            return plannedLayer;
        }

        var filtered = new List<AgronomicLayerCell>();
        foreach (var cell in plannedLayer.Cells)
        {
            if (IsWithinRegion(cell.Position, regionOfInterest.Cells))
            {
                filtered.Add(cell);
            }
        }

        if (filtered.Count == plannedLayer.Cells.Count)
        {
            return plannedLayer;
        }

        var provenance = new LayerProvenance(
            plannedLayer.Provenance.Source,
            plannedLayer.Provenance.Transform,
            ComputeLayerHash(plannedLayer.LayerId, filtered),
            plannedLayer.Provenance.CreatedAt,
            plannedLayer.Provenance.Actor);

        return new AgronomicLayerDocument(
            plannedLayer.LayerId,
            plannedLayer.Kind,
            plannedLayer.Units,
            plannedLayer.CreatedAt,
            plannedLayer.CreatedBy,
            filtered,
            provenance);
    }

    private static bool IsWithinRegion(PlanarPoint position, IReadOnlyList<AgronomicLayerCell> regionCells)
    {
        foreach (var regionCell in regionCells)
        {
            if (regionCell.Value <= 0)
            {
                continue;
            }

            var half = regionCell.CellSizeMeters / 2.0;
            var minX = regionCell.Position.Easting - half;
            var maxX = regionCell.Position.Easting + half;
            var minY = regionCell.Position.Northing - half;
            var maxY = regionCell.Position.Northing + half;

            if (position.Easting >= minX && position.Easting <= maxX
                && position.Northing >= minY && position.Northing <= maxY)
            {
                return true;
            }
        }

        return false;
    }

    private static string ComputeLayerHash(string layerId, IReadOnlyList<AgronomicLayerCell> cells)
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
