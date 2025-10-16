using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Aog.Core.Layers;

namespace Aog.Plugins.VariableMapping;

/// <summary>
/// Builds ISOXML TaskData payloads and manifest metadata for planned prescription layers.
/// </summary>
public sealed class PrescriptionExportPipeline
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrescriptionExportPipeline"/> class.
    /// </summary>
    /// <param name="timeProvider">Optional time provider used for deterministic timestamps.</param>
    public PrescriptionExportPipeline(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Generates export artifacts for the supplied planned prescription layer.
    /// </summary>
    /// <param name="layer">Layer that should be exported.</param>
    /// <param name="jobId">Identifier of the job associated with the export.</param>
    /// <param name="sessionId">Session identifier captured for provenance.</param>
    /// <param name="recipeId">Recipe identifier recorded in the export manifest.</param>
    /// <param name="operatorId">Optional actor recorded in the manifest.</param>
    /// <returns>Structured export artifacts.</returns>
    public PrescriptionExportArtifacts Export(
        AgronomicLayerDocument layer,
        string jobId,
        string sessionId,
        string recipeId,
        string? operatorId = null)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier is required.", nameof(jobId));
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session identifier is required.", nameof(sessionId));
        }

        if (string.IsNullOrWhiteSpace(recipeId))
        {
            throw new ArgumentException("Recipe identifier is required.", nameof(recipeId));
        }

        if (layer.Cells.Count == 0)
        {
            throw new InvalidOperationException("Prescription export requires at least one cell.");
        }

        var timestamp = _timeProvider.GetUtcNow();
        var isoXml = BuildIsoXml(layer, jobId, sessionId, recipeId, timestamp);
        var manifest = BuildManifest(layer, jobId, sessionId, recipeId, operatorId, timestamp);

        return new PrescriptionExportArtifacts(isoXml, manifest);
    }

    private static string BuildIsoXml(
        AgronomicLayerDocument layer,
        string jobId,
        string sessionId,
        string recipeId,
        DateTimeOffset timestamp)
    {
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(
                "ISO11783_TaskData",
                new XAttribute("version", "4.3"),
                new XAttribute("managementSoftware", "AgOpenGPS Nexus"),
                new XAttribute("exportedAt", timestamp.ToString("O", CultureInfo.InvariantCulture)),
                new XElement(
                    "Task",
                    new XAttribute("id", jobId),
                    new XAttribute("sessionId", sessionId),
                    new XAttribute("recipeId", recipeId),
                    new XAttribute("layerId", layer.LayerId),
                    new XAttribute("units", layer.Units),
                    new XAttribute("cellCount", layer.Cells.Count)),
                new XElement(
                    "PrescriptionLayer",
                    new XAttribute("id", layer.LayerId),
                    new XAttribute("kind", layer.Kind),
                    new XAttribute("units", layer.Units),
                    new XAttribute("createdAt", layer.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
                    new XAttribute("createdBy", layer.CreatedBy),
                    new XAttribute("source", layer.Provenance.Source),
                    new XAttribute("transform", layer.Provenance.Transform),
                    new XAttribute("hash", layer.Provenance.Hash),
                    BuildCellsElement(layer))));

        using var writer = new Utf8StringWriter();
        document.Save(writer, SaveOptions.DisableFormatting);
        return writer.ToString();
    }

    private static XElement BuildCellsElement(AgronomicLayerDocument layer)
    {
        var cellsElement = new XElement("Cells");
        for (var index = 0; index < layer.Cells.Count; index++)
        {
            var cell = layer.Cells[index];
            cellsElement.Add(new XElement(
                "Cell",
                new XAttribute("index", index),
                new XAttribute("centerEasting", cell.Position.Easting.ToString("0.###", CultureInfo.InvariantCulture)),
                new XAttribute("centerNorthing", cell.Position.Northing.ToString("0.###", CultureInfo.InvariantCulture)),
                new XAttribute("cellSizeMeters", cell.CellSizeMeters.ToString("0.###", CultureInfo.InvariantCulture)),
                new XAttribute("value", cell.Value.ToString("0.##########", CultureInfo.InvariantCulture))));
        }

        return cellsElement;
    }

    private static string BuildManifest(
        AgronomicLayerDocument layer,
        string jobId,
        string sessionId,
        string recipeId,
        string? operatorId,
        DateTimeOffset timestamp)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("jobId", jobId);
            writer.WriteString("sessionId", sessionId);
            writer.WriteString("recipeId", recipeId);
            writer.WriteString("layerId", layer.LayerId);
            writer.WriteString("layerKind", layer.Kind);
            writer.WriteString("units", layer.Units);
            writer.WriteNumber("cellCount", layer.Cells.Count);
            writer.WriteString("provenanceHash", layer.Provenance.Hash);
            writer.WriteString("exportedAt", timestamp.ToString("O", CultureInfo.InvariantCulture));
            writer.WriteString("source", layer.Provenance.Source);
            writer.WriteString("transform", layer.Provenance.Transform);
            if (!string.IsNullOrWhiteSpace(operatorId))
            {
                writer.WriteString("operatorId", operatorId);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}

/// <summary>
/// Export artifacts emitted by the <see cref="PrescriptionExportPipeline"/>.
/// </summary>
/// <param name="IsoXml">ISOXML TaskData document containing the prescription payload.</param>
/// <param name="ManifestJson">Structured manifest capturing audit metadata.</param>
public sealed record PrescriptionExportArtifacts(string IsoXml, string ManifestJson);
