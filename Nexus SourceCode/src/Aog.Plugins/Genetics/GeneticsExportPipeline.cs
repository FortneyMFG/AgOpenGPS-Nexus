using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Aog.Core.Paths;

namespace Aog.Plugins.Genetics;

/// <summary>
/// Materializes genetics plan and variety features into CSV, GeoJSON, and ISOXML representations.
/// </summary>
public sealed class GeneticsExportPipeline
{
    /// <summary>
    /// Generates export artifacts for the supplied plan and variety features.
    /// </summary>
    /// <param name="plans">Plan features to export.</param>
    /// <param name="varieties">Variety features to export.</param>
    public GeneticsExportArtifacts Export(
        IEnumerable<GeneticsPlanFeature> plans,
        IEnumerable<GeneticsVarietyFeature> varieties)
    {
        if (plans is null)
        {
            throw new ArgumentNullException(nameof(plans));
        }

        if (varieties is null)
        {
            throw new ArgumentNullException(nameof(varieties));
        }

        var planList = plans.ToList();
        var varietyList = varieties.ToList();

        var csv = BuildCsv(planList, varietyList);
        var geoJson = BuildGeoJson(planList, varietyList);
        var isoXml = BuildIsoXml(planList, varietyList);

        return new GeneticsExportArtifacts(csv, geoJson, isoXml);
    }

    private static string BuildCsv(
        IReadOnlyList<GeneticsPlanFeature> plans,
        IReadOnlyList<GeneticsVarietyFeature> varieties)
    {
        var builder = new StringBuilder();
        builder.AppendLine("recordType,zoneId,featureId,layerId,jobId,sessionId,createdAt,lastModifiedAt,appliedAt,brand,product,traitStack,lot,treatment,source,barcode,notes,areaSquareMeters");

        foreach (var plan in plans.OrderBy(plan => plan.CreatedAt).ThenBy(plan => plan.FeatureId, StringComparer.Ordinal))
        {
            AppendCsvRow(builder, "plan", plan.ZoneId, plan.FeatureId, plan.LayerId, plan.JobId, sessionId: null, plan.CreatedAt, plan.LastModifiedAt, appliedAt: null, plan.Brand, plan.Product, plan.TraitStack, plan.Lot, plan.Treatment, plan.Source, barcode: null, plan.Notes, plan.Geometry.AreaSquareMeters);
        }

        foreach (var variety in varieties.OrderBy(variety => variety.AppliedAt).ThenBy(variety => variety.SessionId, StringComparer.Ordinal).ThenBy(variety => variety.FeatureId, StringComparer.Ordinal))
        {
            AppendCsvRow(builder, "variety", variety.ZoneId, variety.FeatureId, variety.LayerId, variety.JobId, variety.SessionId, variety.CreatedAt, variety.LastModifiedAt, variety.AppliedAt, variety.Brand, variety.Product, variety.TraitStack, variety.Lot, variety.Treatment, variety.Source, variety.Barcode, variety.Notes, variety.Geometry.AreaSquareMeters);
        }

        return builder.ToString();
    }

    private static void AppendCsvRow(
        StringBuilder builder,
        string recordType,
        string zoneId,
        string featureId,
        string layerId,
        string? jobId,
        string? sessionId,
        DateTimeOffset createdAt,
        DateTimeOffset? lastModifiedAt,
        DateTimeOffset? appliedAt,
        string brand,
        string product,
        string? traitStack,
        string? lot,
        string? treatment,
        string? source,
        string? barcode,
        string? notes,
        double areaSquareMeters)
    {
        builder.Append(recordType).Append(',');
        builder.Append(Escape(zoneId)).Append(',');
        builder.Append(Escape(featureId)).Append(',');
        builder.Append(Escape(layerId)).Append(',');
        builder.Append(Escape(jobId)).Append(',');
        builder.Append(Escape(sessionId)).Append(',');
        builder.Append(createdAt.ToString("O", CultureInfo.InvariantCulture)).Append(',');
        builder.Append(lastModifiedAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty).Append(',');
        builder.Append(appliedAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty).Append(',');
        builder.Append(Escape(brand)).Append(',');
        builder.Append(Escape(product)).Append(',');
        builder.Append(Escape(traitStack)).Append(',');
        builder.Append(Escape(lot)).Append(',');
        builder.Append(Escape(treatment)).Append(',');
        builder.Append(Escape(source)).Append(',');
        builder.Append(Escape(barcode)).Append(',');
        builder.Append(Escape(notes)).Append(',');
        builder.Append(areaSquareMeters.ToString("0.###", CultureInfo.InvariantCulture)).AppendLine();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
        {
            return value;
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static string BuildGeoJson(
        IReadOnlyList<GeneticsPlanFeature> plans,
        IReadOnlyList<GeneticsVarietyFeature> varieties)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("type", "FeatureCollection");
        writer.WritePropertyName("features");
        writer.WriteStartArray();

        foreach (var plan in plans.OrderBy(plan => plan.CreatedAt).ThenBy(plan => plan.FeatureId, StringComparer.Ordinal))
        {
            WriteFeature(writer, plan, recordType: "plan", sessionId: null, appliedAt: null, barcode: null, changeLog: Array.Empty<GeneticsChangeLogEntry>());
        }

        foreach (var variety in varieties.OrderBy(variety => variety.AppliedAt).ThenBy(variety => variety.SessionId, StringComparer.Ordinal).ThenBy(variety => variety.FeatureId, StringComparer.Ordinal))
        {
            WriteFeature(writer, variety, recordType: "variety", sessionId: variety.SessionId, appliedAt: variety.AppliedAt, barcode: variety.Barcode, changeLog: variety.ChangeLog);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteFeature(
        Utf8JsonWriter writer,
        GeneticsPlanFeature feature,
        string recordType,
        string? sessionId,
        DateTimeOffset? appliedAt,
        string? barcode,
        IReadOnlyList<GeneticsChangeLogEntry>? changeLog)
    {
        var entries = changeLog ?? Array.Empty<GeneticsChangeLogEntry>();

        writer.WriteStartObject();
        writer.WriteString("type", "Feature");
        writer.WriteString("id", feature.FeatureId);

        writer.WritePropertyName("properties");
        writer.WriteStartObject();
        writer.WriteString("recordType", recordType);
        writer.WriteString("zoneId", feature.ZoneId);
        writer.WriteString("layerId", feature.LayerId);
        if (!string.IsNullOrEmpty(feature.JobId))
        {
            writer.WriteString("jobId", feature.JobId);
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            writer.WriteString("sessionId", sessionId);
        }

        writer.WriteString("createdAt", feature.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        if (feature.LastModifiedAt is { } lastModifiedAt)
        {
            writer.WriteString("lastModifiedAt", lastModifiedAt.ToString("O", CultureInfo.InvariantCulture));
        }

        if (appliedAt is { } applied)
        {
            writer.WriteString("appliedAt", applied.ToString("O", CultureInfo.InvariantCulture));
        }

        writer.WriteString("brand", feature.Brand);
        writer.WriteString("product", feature.Product);
        if (!string.IsNullOrEmpty(feature.TraitStack))
        {
            writer.WriteString("traitStack", feature.TraitStack);
        }

        if (!string.IsNullOrEmpty(feature.Lot))
        {
            writer.WriteString("lot", feature.Lot);
        }

        if (!string.IsNullOrEmpty(feature.Treatment))
        {
            writer.WriteString("treatment", feature.Treatment);
        }

        if (!string.IsNullOrEmpty(feature.Source))
        {
            writer.WriteString("source", feature.Source);
        }

        if (!string.IsNullOrEmpty(barcode))
        {
            writer.WriteString("barcode", barcode);
        }

        if (!string.IsNullOrEmpty(feature.Notes))
        {
            writer.WriteString("notes", feature.Notes);
        }

        writer.WriteNumber("areaSquareMeters", feature.Geometry.AreaSquareMeters);

        if (entries.Count > 0)
        {
            writer.WritePropertyName("changeLog");
            writer.WriteStartArray();
            foreach (var entry in entries)
            {
                writer.WriteStartObject();
                writer.WriteString("changedAt", entry.ChangedAt.ToString("O", CultureInfo.InvariantCulture));
                writer.WriteString("actor", entry.Actor);
                writer.WriteString("field", entry.Field);
                if (entry.Previous is not null)
                {
                    writer.WriteString("previous", entry.Previous);
                }
                else
                {
                    writer.WriteNull("previous");
                }

                if (entry.Current is not null)
                {
                    writer.WriteString("current", entry.Current);
                }
                else
                {
                    writer.WriteNull("current");
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();

        writer.WritePropertyName("geometry");
        writer.WriteStartObject();
        writer.WriteString("type", "Polygon");
        writer.WritePropertyName("coordinates");
        writer.WriteStartArray();

        WriteLinearRing(writer, feature.Geometry.OuterBoundary);
        foreach (var hole in feature.Geometry.Holes)
        {
            WriteLinearRing(writer, hole);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteLinearRing(Utf8JsonWriter writer, IReadOnlyList<PlanarPoint> ring)
    {
        writer.WriteStartArray();
        for (var i = 0; i < ring.Count; i++)
        {
            WriteCoordinate(writer, ring[i]);
        }

        if (ring.Count > 0 && (ring[^1].Easting != ring[0].Easting || ring[^1].Northing != ring[0].Northing))
        {
            WriteCoordinate(writer, ring[0]);
        }

        writer.WriteEndArray();
    }

    private static void WriteCoordinate(Utf8JsonWriter writer, PlanarPoint point)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(point.Easting);
        writer.WriteNumberValue(point.Northing);
        writer.WriteEndArray();
    }

    private static string BuildIsoXml(
        IReadOnlyList<GeneticsPlanFeature> plans,
        IReadOnlyList<GeneticsVarietyFeature> varieties)
    {
        var taskData = new XElement("ISO11783_TaskData",
            new XAttribute("version", "4.3"),
            new XElement("GeneticsPlans", plans.OrderBy(plan => plan.CreatedAt).ThenBy(plan => plan.FeatureId, StringComparer.Ordinal).Select(CreatePlanElement)),
            new XElement("GeneticsVarieties", varieties.OrderBy(variety => variety.AppliedAt).ThenBy(variety => variety.SessionId, StringComparer.Ordinal).ThenBy(variety => variety.FeatureId, StringComparer.Ordinal).Select(CreateVarietyElement)));

        var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), taskData);
        using var writer = new Utf8StringWriter();
        document.Save(writer, SaveOptions.None);
        return writer.ToString();
    }

    private static XElement CreatePlanElement(GeneticsPlanFeature feature)
    {
        var element = new XElement("Plan",
            new XAttribute("id", feature.FeatureId),
            new XAttribute("zoneId", feature.ZoneId),
            new XAttribute("layerId", feature.LayerId),
            new XAttribute("brand", feature.Brand),
            new XAttribute("product", feature.Product),
            new XAttribute("areaSquareMeters", feature.Geometry.AreaSquareMeters.ToString("0.###", CultureInfo.InvariantCulture)));

        if (!string.IsNullOrEmpty(feature.JobId))
        {
            element.Add(new XAttribute("jobId", feature.JobId));
        }

        if (!string.IsNullOrEmpty(feature.TraitStack))
        {
            element.Add(new XAttribute("traitStack", feature.TraitStack));
        }

        if (!string.IsNullOrEmpty(feature.Lot))
        {
            element.Add(new XAttribute("lot", feature.Lot));
        }

        if (!string.IsNullOrEmpty(feature.Treatment))
        {
            element.Add(new XAttribute("treatment", feature.Treatment));
        }

        if (!string.IsNullOrEmpty(feature.Source))
        {
            element.Add(new XAttribute("source", feature.Source));
        }

        if (!string.IsNullOrEmpty(feature.Notes))
        {
            element.Add(new XAttribute("notes", feature.Notes));
        }

        element.Add(CreateGeometryElement(feature.Geometry));
        return element;
    }

    private static XElement CreateVarietyElement(GeneticsVarietyFeature feature)
    {
        var element = new XElement("Variety",
            new XAttribute("id", feature.FeatureId),
            new XAttribute("zoneId", feature.ZoneId),
            new XAttribute("layerId", feature.LayerId),
            new XAttribute("jobId", feature.JobId),
            new XAttribute("sessionId", feature.SessionId),
            new XAttribute("brand", feature.Brand),
            new XAttribute("product", feature.Product),
            new XAttribute("appliedAt", feature.AppliedAt.ToString("O", CultureInfo.InvariantCulture)),
            new XAttribute("areaSquareMeters", feature.Geometry.AreaSquareMeters.ToString("0.###", CultureInfo.InvariantCulture)));

        if (!string.IsNullOrEmpty(feature.TraitStack))
        {
            element.Add(new XAttribute("traitStack", feature.TraitStack));
        }

        if (!string.IsNullOrEmpty(feature.Lot))
        {
            element.Add(new XAttribute("lot", feature.Lot));
        }

        if (!string.IsNullOrEmpty(feature.Treatment))
        {
            element.Add(new XAttribute("treatment", feature.Treatment));
        }

        if (!string.IsNullOrEmpty(feature.Source))
        {
            element.Add(new XAttribute("source", feature.Source));
        }

        if (!string.IsNullOrEmpty(feature.Barcode))
        {
            element.Add(new XAttribute("barcode", feature.Barcode));
        }

        if (!string.IsNullOrEmpty(feature.Notes))
        {
            element.Add(new XAttribute("notes", feature.Notes));
        }

        element.Add(CreateGeometryElement(feature.Geometry));

        if (feature.ChangeLog.Count > 0)
        {
            var changeLogElement = new XElement("ChangeLog", feature.ChangeLog.Select(entry =>
                new XElement("Entry",
                    new XAttribute("changedAt", entry.ChangedAt.ToString("O", CultureInfo.InvariantCulture)),
                    new XAttribute("actor", entry.Actor),
                    new XAttribute("field", entry.Field),
                    entry.Previous is null ? null : new XAttribute("previous", entry.Previous),
                    entry.Current is null ? null : new XAttribute("current", entry.Current))));

            element.Add(changeLogElement);
        }

        return element;
    }

    private static XElement CreateGeometryElement(GeneticsZoneGeometry geometry)
    {
        var element = new XElement("Geometry");
        element.Add(CreateRingElement("Boundary", geometry.OuterBoundary));
        foreach (var hole in geometry.Holes)
        {
            element.Add(CreateRingElement("Hole", hole));
        }

        return element;
    }

    private static XElement CreateRingElement(string name, IReadOnlyList<PlanarPoint> ring)
    {
        var element = new XElement(name);
        foreach (var point in ring)
        {
            element.Add(new XElement("Point",
                new XAttribute("e", point.Easting.ToString("0.###", CultureInfo.InvariantCulture)),
                new XAttribute("n", point.Northing.ToString("0.###", CultureInfo.InvariantCulture))));
        }

        if (ring.Count > 0)
        {
            var first = ring[0];
            var last = ring[^1];
            if (last.Easting != first.Easting || last.Northing != first.Northing)
            {
                element.Add(new XElement("Point",
                    new XAttribute("e", first.Easting.ToString("0.###", CultureInfo.InvariantCulture)),
                    new XAttribute("n", first.Northing.ToString("0.###", CultureInfo.InvariantCulture))));
            }
        }

        return element;
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public Utf8StringWriter()
            : base(CultureInfo.InvariantCulture)
        {
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}

/// <summary>
/// Export artifacts emitted by the <see cref="GeneticsExportPipeline"/>.
/// </summary>
/// <param name="Csv">CSV representation of plan and variety features.</param>
/// <param name="GeoJson">GeoJSON feature collection.</param>
/// <param name="IsoXml">ISOXML document capturing plan and variety data.</param>
public sealed record GeneticsExportArtifacts(string Csv, string GeoJson, string IsoXml);
