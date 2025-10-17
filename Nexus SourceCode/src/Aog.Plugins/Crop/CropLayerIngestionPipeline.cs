using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Layers;

namespace Aog.Plugins.Crop;

/// <summary>
/// Maintains the authoritative feature set for crop type layers based on layer edit journal entries.
/// </summary>
public sealed class CropLayerIngestionPipeline
{
    private const double AreaTolerance = 1e-6;

    private readonly string _layerId;
    private readonly string? _expectedStatus;
    private readonly Dictionary<string, CropZoneFeature> _features = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    private string? _jobId;
    private string? _sessionId;
    private LayerEditEventContext? _context;
    private DateTimeOffset _lastUpdated = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="CropLayerIngestionPipeline"/> class.
    /// </summary>
    /// <param name="layerId">Layer identifier this pipeline is bound to.</param>
    public CropLayerIngestionPipeline(string layerId)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        _layerId = layerId.Trim();
        _expectedStatus = DeriveExpectedStatus(_layerId);
    }

    /// <summary>
    /// Applies a journal entry to the pipeline, mutating the tracked feature set.
    /// </summary>
    /// <param name="entry">Journal entry emitted by the zone drawing framework.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask ApplyAsync(LayerEditEventEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(entry.LayerId, _layerId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Pipeline is bound to layer '{_layerId}' but received entry for '{entry.LayerId}'.");
        }

        lock (_sync)
        {
            BindJob(entry.JobId);
            BindSession(entry.SessionId);
            BindContext(entry.Context);

            foreach (var operation in entry.Operations)
            {
                ApplyOperation(entry, operation);
            }

            if (entry.CreatedAt > _lastUpdated)
            {
                _lastUpdated = entry.CreatedAt;
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Attempts to resolve a feature by identifier.
    /// </summary>
    public bool TryGetFeature(string featureId, out CropZoneFeature feature)
    {
        if (string.IsNullOrWhiteSpace(featureId))
        {
            throw new ArgumentException("Feature identifier is required.", nameof(featureId));
        }

        lock (_sync)
        {
            if (_features.TryGetValue(featureId, out var resolved))
            {
                feature = resolved.Clone();
                return true;
            }
        }

        feature = null!;
        return false;
    }

    /// <summary>
    /// Creates an immutable snapshot of the current layer state.
    /// </summary>
    public CropLayerSnapshot CreateSnapshot()
    {
        lock (_sync)
        {
            var features = _features.Values
                .OrderBy(feature => feature.FeatureId, StringComparer.Ordinal)
                .Select(feature => feature.Clone())
                .ToArray();

            return new CropLayerSnapshot(_layerId, _jobId, _sessionId, _context, _lastUpdated, features);
        }
    }

    private void BindJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new InvalidOperationException("Journal entry did not include a job identifier.");
        }

        if (_jobId is null)
        {
            _jobId = jobId;
            return;
        }

        if (!string.Equals(_jobId, jobId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Layer '{_layerId}' already bound to job '{_jobId}' and cannot process entry for job '{jobId}'.");
        }
    }

    private void BindSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        if (_sessionId is null || !string.Equals(_sessionId, sessionId, StringComparison.OrdinalIgnoreCase))
        {
            _sessionId = sessionId;
        }
    }

    private void BindContext(LayerEditEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = new LayerEditEventContext(context.FarmId, context.FieldIds.ToArray(), context.SeasonId);
    }

    private void ApplyOperation(LayerEditEventEntry entry, LayerEditEventOperation operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        var type = operation.Type?.Trim();
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new InvalidOperationException("Layer edit operation type is required.");
        }

        switch (type.ToLowerInvariant())
        {
            case "create":
                ApplyCreate(entry, operation);
                break;
            case "update":
                ApplyUpdate(entry, operation);
                break;
            case "delete":
                ApplyDelete(operation);
                break;
            case "merge":
            case "split":
                ApplyComposite(entry, operation);
                break;
            default:
                throw new NotSupportedException($"Operation type '{operation.Type}' is not supported by the crop ingestion pipeline.");
        }
    }

    private void ApplyComposite(LayerEditEventEntry entry, LayerEditEventOperation operation)
    {
        var removedSources = new Dictionary<string, CropZoneFeature>(StringComparer.Ordinal);

        foreach (var sourceId in operation.SourceFeatureIds)
        {
            if (_features.TryGetValue(sourceId, out var existing))
            {
                removedSources[sourceId] = existing;
            }

            _features.Remove(sourceId);
        }

        try
        {
            var compositeFeature = BuildFeature(entry, operation);
            _features[operation.FeatureId] = compositeFeature;
        }
        catch
        {
            foreach (var pair in removedSources)
            {
                _features[pair.Key] = pair.Value;
            }

            throw;
        }
    }

    private void ApplyCreate(LayerEditEventEntry entry, LayerEditEventOperation operation)
    {
        var feature = BuildFeature(entry, operation);

        _features[operation.FeatureId] = feature;
    }

    private CropZoneFeature BuildFeature(LayerEditEventEntry entry, LayerEditEventOperation operation)
    {
        if (string.IsNullOrWhiteSpace(operation.FeatureId))
        {
            throw new InvalidOperationException("Operations must include a feature identifier.");
        }

        if (_features.ContainsKey(operation.FeatureId))
        {
            throw new InvalidOperationException($"Feature '{operation.FeatureId}' already exists in layer '{_layerId}'.");
        }

    var geometry = CloneGeometry(operation.GeometryAfter, operation.FeatureId, required: true);
    var attributes = ExtractAttributes(operation.AttributesAfter, null, requireAll: true);
        var area = ComputeArea(operation.Summary, null);

        return new CropZoneFeature(
            operation.FeatureId,
            attributes.Crop,
            attributes.Year,
            attributes.Status,
            geometry,
            area,
            attributes.Variety,
            attributes.Source,
            attributes.Notes,
            entry.CreatedAt,
            entry.Actor);
    }

    private void ApplyUpdate(LayerEditEventEntry entry, LayerEditEventOperation operation)
    {
        if (!_features.TryGetValue(operation.FeatureId, out var existing))
        {
            throw new InvalidOperationException($"Cannot update crop feature '{operation.FeatureId}' because it has not been created.");
        }

    var fallback = existing.ToAttributes();
    var attributes = ExtractAttributes(operation.AttributesAfter, fallback, requireAll: false);
    var geometry = CloneGeometry(operation.GeometryAfter, operation.FeatureId, required: false) ?? existing.GeometryNode.DeepClone();
        var area = ComputeArea(operation.Summary, existing.AreaSqMeters);

        var updated = existing.With(
            geometry,
            attributes,
            area,
            entry.CreatedAt,
            entry.Actor);

        _features[operation.FeatureId] = updated;
    }

    private void ApplyDelete(LayerEditEventOperation operation)
    {
        if (!_features.Remove(operation.FeatureId))
        {
            throw new InvalidOperationException($"Cannot delete crop feature '{operation.FeatureId}' because it does not exist.");
        }
    }

    private CropAttributes ExtractAttributes(JsonNode? attributesNode, CropAttributes? fallback, bool requireAll)
    {
        string? crop = fallback?.Crop;
        string? status = fallback?.Status;
        int? year = fallback?.Year;
        string? variety = fallback?.Variety;
        string? source = fallback?.Source;
        string? notes = fallback?.Notes;

        if (attributesNode is null)
        {
            if (requireAll)
            {
                throw new InvalidOperationException("Crop layer operations must include attributes.");
            }

            return ValidateAttributes(crop, year, status, variety, source, notes);
        }

        if (attributesNode is not JsonObject obj)
        {
            throw new InvalidOperationException("Attributes payload must be a JSON object.");
        }

        if (TryReadString(obj, "crop", out var value))
        {
            crop = value;
        }

        if (TryReadString(obj, "status", out value))
        {
            status = value;
        }

        if (TryReadInt(obj, "year", out var intValue))
        {
            year = intValue;
        }

        if (TryReadOptionalString(obj, "variety", out value))
        {
            variety = value;
        }

        if (TryReadOptionalString(obj, "source", out value))
        {
            source = value;
        }

        if (TryReadOptionalString(obj, "notes", out value))
        {
            notes = value;
        }

        return ValidateAttributes(crop, year, status, variety, source, notes);
    }

    private CropAttributes ValidateAttributes(string? crop, int? year, string? status, string? variety, string? source, string? notes)
    {
        if (string.IsNullOrWhiteSpace(crop))
        {
            throw new InvalidOperationException("Crop attribute is required.");
        }

        if (!year.HasValue)
        {
            throw new InvalidOperationException("Year attribute is required.");
        }

        if (year.Value is < 1900 or > 2100)
        {
            throw new InvalidOperationException($"Year '{year.Value}' is outside the supported range (1900-2100).");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new InvalidOperationException("Status attribute is required.");
        }

        if (!IsSupportedStatus(status))
        {
            throw new InvalidOperationException($"Status '{status}' is not recognised.");
        }

        if (_expectedStatus is not null && !status.Equals(_expectedStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Layer '{_layerId}' expects status '{_expectedStatus}' but received '{status}'.");
        }

        var normalizedCrop = crop.Trim();
        var normalizedStatus = status.Trim().ToLowerInvariant();
        var normalizedVariety = string.IsNullOrWhiteSpace(variety) ? null : variety.Trim();
        var normalizedSource = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        return new CropAttributes(normalizedCrop, year.Value, normalizedStatus, normalizedVariety, normalizedSource, normalizedNotes);
    }

    private static JsonNode? CloneGeometry(JsonNode? geometryNode, string featureId, bool required)
    {
        if (geometryNode is null)
        {
            if (required)
            {
                throw new InvalidOperationException($"Feature '{featureId}' must include geometry.");
            }

            return null;
        }

        if (geometryNode.GetValueKind() == JsonValueKind.Null)
        {
            if (required)
            {
                throw new InvalidOperationException($"Feature '{featureId}' must include geometry.");
            }

            return null;
        }

        if (geometryNode is not JsonObject && geometryNode is not JsonArray)
        {
            throw new InvalidOperationException("Geometry payload must be a JSON object or array.");
        }

        return geometryNode.DeepClone();
    }

    private static double? ComputeArea(LayerEditEventOperationSummary? summary, double? existingArea)
    {
        if (summary?.AreaDeltaSqMeters is not double delta)
        {
            return existingArea;
        }

        if (!double.IsFinite(delta))
        {
            throw new InvalidOperationException("Area delta must be a finite number.");
        }

        var baseArea = existingArea ?? 0d;
        var updated = baseArea + delta;

        if (updated < 0 && Math.Abs(updated) < AreaTolerance)
        {
            updated = 0;
        }

        if (updated < 0)
        {
            throw new InvalidOperationException("Operation produced a negative area which is not supported.");
        }

        return updated;
    }

    private static bool TryReadString(JsonObject obj, string propertyName, out string value)
    {
        if (obj.TryGetPropertyValue(propertyName, out var node) && node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            value = raw.Trim();
            return true;
        }

        value = null!;
        return false;
    }

    private static bool TryReadOptionalString(JsonObject obj, string propertyName, out string? value)
    {
        value = null;
        if (!obj.TryGetPropertyValue(propertyName, out var node))
        {
            return false;
        }

        if (node is null || node.GetValueKind() == JsonValueKind.Null)
        {
            return true;
        }

        if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var raw))
        {
            value = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
            return true;
        }

        throw new InvalidOperationException($"Attribute '{propertyName}' must be a string or null.");
    }

    private static bool TryReadInt(JsonObject obj, string propertyName, out int value)
    {
        value = default;
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null || node.GetValueKind() == JsonValueKind.Null)
        {
            return false;
        }

        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<int>(out var intValue))
            {
                value = intValue;
                return true;
            }

            if (jsonValue.TryGetValue<long>(out var longValue))
            {
                value = checked((int)longValue);
                return true;
            }
        }

        throw new InvalidOperationException($"Attribute '{propertyName}' must be an integer.");
    }

    private static bool IsSupportedStatus(string status)
    {
        return status.Equals("planned", StringComparison.OrdinalIgnoreCase)
            || status.Equals("actual", StringComparison.OrdinalIgnoreCase)
            || status.Equals("historical", StringComparison.OrdinalIgnoreCase);
    }

    private static string? DeriveExpectedStatus(string layerId)
    {
        var colonIndex = layerId.IndexOf(':');
        var identifier = colonIndex >= 0 && colonIndex + 1 < layerId.Length
            ? layerId[(colonIndex + 1)..]
            : layerId;

        var parts = identifier.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        if (!parts[0].Equals("cropType", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return parts[1].ToLowerInvariant() switch
        {
            "planned" => "planned",
            "actual" => "actual",
            "history" => "historical",
            _ => null
        };
    }

    internal readonly struct CropAttributes
    {
        public CropAttributes(string crop, int year, string status, string? variety, string? source, string? notes)
        {
            Crop = crop;
            Year = year;
            Status = status;
            Variety = variety;
            Source = source;
            Notes = notes;
        }

        public string Crop { get; }

        public int Year { get; }

        public string Status { get; }

        public string? Variety { get; }

        public string? Source { get; }

        public string? Notes { get; }
    }
}
