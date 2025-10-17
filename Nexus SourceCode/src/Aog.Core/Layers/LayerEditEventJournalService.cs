using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Layers;

/// <summary>
/// Represents the primary journaling service for <c>LayerEditEvent.v1</c> documents.
/// The service keeps an in-memory, append-only journal per layer, computes deterministic
/// hashes for each entry, and links entries together via <c>previousHash</c>/<c>nextHash</c> pointers.
/// </summary>
public sealed class LayerEditEventJournalService : ILayerEditEventJournalService
{
    private static readonly Regex LayerIdPattern = new("^layer:[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex JobIdPattern = new("^job:[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SessionIdPattern = new("^session:[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex FarmIdPattern = new("^farm:[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex FieldIdPattern = new("^field:[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SeasonIdPattern = new("^season:[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex TileIdPattern = new("^tile:[A-Za-z0-9._:/-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex HashPattern = new("^[A-Fa-f0-9]{16,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex OperationGroupPattern = new("^undoGroup:[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex EventIdSlugTokenizer = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private readonly Dictionary<string, List<LayerEditEventEntry>> _journals = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="LayerEditEventJournalService"/> class.
    /// </summary>
    /// <param name="timeProvider">Time abstraction used for deterministic testing.</param>
    public LayerEditEventJournalService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public ValueTask<LayerEditEventEntry> AppendAsync(LayerEditEventAppendRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        Validate(request);

        var timestamp = request.CreatedAt ?? _timeProvider.GetUtcNow();

        lock (_gate)
        {
            if (!_journals.TryGetValue(request.LayerId, out var journal))
            {
                journal = new List<LayerEditEventEntry>();
                _journals[request.LayerId] = journal;
            }

            var previous = journal.Count > 0 ? journal[^1] : null;
            var ordinal = journal.Count + 1;
            var identifier = CreateEventId(request.LayerId, timestamp, ordinal);

            var entry = new LayerEditEventEntry(
                schemaVersion: "1.0.0",
                id: identifier,
                layerId: request.LayerId,
                jobId: request.JobId,
                sessionId: request.SessionId,
                context: request.Context,
                tool: request.Tool,
                createdAt: timestamp,
                actor: request.Actor ?? string.Empty,
                previousHash: previous?.Hash,
                operationGroupId: request.OperationGroupId,
                tileRefs: request.TileReferences,
                operations: request.Operations,
                metadata: request.Metadata,
                notes: request.Notes);

            var hash = ComputeHash(entry);
            entry.SetHash(hash);

            if (previous is not null)
            {
                previous.SetNextHash(hash);
            }

            journal.Add(entry);
            return ValueTask.FromResult(entry);
        }
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<LayerEditEventEntry>> ListAsync(string layerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_journals.TryGetValue(layerId, out var journal))
            {
                return ValueTask.FromResult<IReadOnlyList<LayerEditEventEntry>>(Array.Empty<LayerEditEventEntry>());
            }

            return ValueTask.FromResult<IReadOnlyList<LayerEditEventEntry>>(journal.ToArray());
        }
    }

    /// <inheritdoc />
    public ValueTask TruncateAsync(string layerId, string? keepHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        if (keepHash is not null && !HashPattern.IsMatch(keepHash))
        {
            throw new ArgumentException("Hash must be a 16-64 character hex string.", nameof(keepHash));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_journals.TryGetValue(layerId, out var journal) || journal.Count == 0)
            {
                return ValueTask.CompletedTask;
            }

            if (keepHash is null)
            {
                _journals.Remove(layerId);
                return ValueTask.CompletedTask;
            }

            var index = journal.FindIndex(entry => string.Equals(entry.Hash, keepHash, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new KeyNotFoundException($"Layer '{layerId}' does not contain journal entry '{keepHash}'.");
            }

            for (var i = journal.Count - 1; i > index; i--)
            {
                journal.RemoveAt(i);
            }

            journal[index].SetNextHash(null);
            return ValueTask.CompletedTask;
        }
    }

    private static string CreateEventId(string layerId, DateTimeOffset createdAt, int ordinal)
    {
        var suffix = layerId;
        var colonIndex = layerId.IndexOf(':');
        if (colonIndex >= 0 && colonIndex + 1 < layerId.Length)
        {
            suffix = layerId[(colonIndex + 1)..];
        }

        suffix = suffix.ToLowerInvariant();
        suffix = EventIdSlugTokenizer.Replace(suffix, "-").Trim('-');
        if (string.IsNullOrWhiteSpace(suffix))
        {
            suffix = "layer";
        }

        return $"layerEdit:{createdAt:yyyyMMdd'T'HHmmssfff}:{suffix}:{ordinal:D4}";
    }

    private static string ComputeHash(LayerEditEventEntry entry)
    {
        var writerBuffer = new ArrayBufferWriter<byte>();
        using (var jsonWriter = new Utf8JsonWriter(writerBuffer, new JsonWriterOptions { Indented = false }))
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("schemaVersion", entry.SchemaVersion);
            jsonWriter.WriteString("id", entry.Id);
            jsonWriter.WriteString("layerId", entry.LayerId);
            jsonWriter.WriteString("jobId", entry.JobId);
            if (entry.SessionId is not null)
            {
                jsonWriter.WriteString("sessionId", entry.SessionId);
            }

            jsonWriter.WritePropertyName("context");
            WriteContext(entry.Context, jsonWriter);

            jsonWriter.WriteString("tool", entry.Tool);
            jsonWriter.WriteString("createdAt", entry.CreatedAt);
            jsonWriter.WriteString("actor", entry.Actor);
            if (entry.PreviousHash is not null)
            {
                jsonWriter.WriteString("previousHash", entry.PreviousHash);
            }

            if (entry.OperationGroupId is not null)
            {
                jsonWriter.WriteString("operationGroupId", entry.OperationGroupId);
            }

            if (entry.TileReferences.Count > 0)
            {
                jsonWriter.WritePropertyName("tileRefs");
                jsonWriter.WriteStartArray();
                foreach (var tile in entry.TileReferences)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WriteString("tileId", tile.TileId);
                    jsonWriter.WriteString("hash", tile.Hash);
                    if (tile.Purpose is not null)
                    {
                        jsonWriter.WriteString("purpose", tile.Purpose);
                    }

                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndArray();
            }

            jsonWriter.WritePropertyName("operations");
            jsonWriter.WriteStartArray();
            foreach (var operation in entry.Operations)
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("type", operation.Type);
                jsonWriter.WriteString("featureId", operation.FeatureId);
                if (operation.SourceFeatureIds.Count > 0)
                {
                    jsonWriter.WritePropertyName("sourceFeatureIds");
                    jsonWriter.WriteStartArray();
                    foreach (var sourceId in operation.SourceFeatureIds)
                    {
                        jsonWriter.WriteStringValue(sourceId);
                    }

                    jsonWriter.WriteEndArray();
                }

                if (operation.SplitStrategy is not null)
                {
                    jsonWriter.WriteString("splitStrategy", operation.SplitStrategy);
                }

                if (operation.GeometryBeforeNode is not null)
                {
                    jsonWriter.WritePropertyName("geometryBefore");
                    operation.GeometryBeforeNode.WriteTo(jsonWriter);
                }

                if (operation.GeometryAfterNode is not null)
                {
                    jsonWriter.WritePropertyName("geometryAfter");
                    operation.GeometryAfterNode.WriteTo(jsonWriter);
                }

                if (operation.AttributesBeforeNode is not null)
                {
                    jsonWriter.WritePropertyName("attributesBefore");
                    operation.AttributesBeforeNode.WriteTo(jsonWriter);
                }

                if (operation.AttributesAfterNode is not null)
                {
                    jsonWriter.WritePropertyName("attributesAfter");
                    operation.AttributesAfterNode.WriteTo(jsonWriter);
                }

                if (operation.VertexEdits.Count > 0)
                {
                    jsonWriter.WritePropertyName("vertexEdits");
                    jsonWriter.WriteStartArray();
                    foreach (var vertex in operation.VertexEdits)
                    {
                        jsonWriter.WriteStartObject();
                        jsonWriter.WriteNumber("index", vertex.Index);
                        jsonWriter.WritePropertyName("before");
                        WriteCoordinate(jsonWriter, vertex.Before);
                        jsonWriter.WritePropertyName("after");
                        WriteCoordinate(jsonWriter, vertex.After);
                        jsonWriter.WriteEndObject();
                    }

                    jsonWriter.WriteEndArray();
                }

                if (operation.AttributePatches.Count > 0)
                {
                    jsonWriter.WritePropertyName("attributePatches");
                    jsonWriter.WriteStartArray();
                    foreach (var patch in operation.AttributePatches)
                    {
                        jsonWriter.WriteStartObject();
                        jsonWriter.WriteString("path", patch.Path);
                        jsonWriter.WriteString("action", patch.Action);
                        if (patch.ValueNode is not null)
                        {
                            jsonWriter.WritePropertyName("value");
                            patch.ValueNode.WriteTo(jsonWriter);
                        }

                        jsonWriter.WriteEndObject();
                    }

                    jsonWriter.WriteEndArray();
                }

                if (operation.Summary is not null)
                {
                    jsonWriter.WritePropertyName("summary");
                    jsonWriter.WriteStartObject();
                    if (operation.Summary.AreaDeltaSqMeters.HasValue)
                    {
                        jsonWriter.WriteNumber("areaDeltaSqMeters", operation.Summary.AreaDeltaSqMeters.Value);
                    }

                    if (operation.Summary.PerimeterDeltaMeters.HasValue)
                    {
                        jsonWriter.WriteNumber("perimeterDeltaMeters", operation.Summary.PerimeterDeltaMeters.Value);
                    }

                    if (operation.Summary.ChangedAttributes.Count > 0)
                    {
                        jsonWriter.WritePropertyName("changedAttributes");
                        jsonWriter.WriteStartArray();
                        foreach (var attribute in operation.Summary.ChangedAttributes)
                        {
                            jsonWriter.WriteStringValue(attribute);
                        }

                        jsonWriter.WriteEndArray();
                    }

                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndArray();

            if (entry.MetadataNode is not null)
            {
                jsonWriter.WritePropertyName("metadata");
                entry.MetadataNode.WriteTo(jsonWriter);
            }

            if (!string.IsNullOrEmpty(entry.Notes))
            {
                jsonWriter.WriteString("notes", entry.Notes);
            }

            jsonWriter.WriteEndObject();
        }

        var hash = SHA256.HashData(writerBuffer.WrittenSpan);
        return Convert.ToHexString(hash);
    }

    private static void WriteContext(LayerEditEventContext context, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("farmId", context.FarmId);
        writer.WritePropertyName("fieldIds");
        writer.WriteStartArray();
        foreach (var fieldId in context.FieldIds)
        {
            writer.WriteStringValue(fieldId);
        }

        writer.WriteEndArray();
        if (context.SeasonId is not null)
        {
            writer.WriteString("seasonId", context.SeasonId);
        }

        writer.WriteEndObject();
    }

    private static void WriteCoordinate(Utf8JsonWriter writer, IReadOnlyList<double> coordinate)
    {
        writer.WriteStartArray();
        foreach (var value in coordinate)
        {
            writer.WriteNumberValue(value);
        }

        writer.WriteEndArray();
    }

    private static void Validate(LayerEditEventAppendRequest request)
    {
        if (!LayerIdPattern.IsMatch(request.LayerId))
        {
            throw new ArgumentException("Layer identifier must match 'layer:*' format.", nameof(request.LayerId));
        }

        if (!JobIdPattern.IsMatch(request.JobId))
        {
            throw new ArgumentException("Job identifier must match 'job:*' format.", nameof(request.JobId));
        }

        if (request.SessionId is not null && !SessionIdPattern.IsMatch(request.SessionId))
        {
            throw new ArgumentException("Session identifier must match 'session:*' format.", nameof(request.SessionId));
        }

        ValidateContext(request.Context);
        ValidateTool(request.Tool);

        if (request.Actor is null)
        {
            throw new ArgumentException("Actor must be provided.", nameof(request.Actor));
        }

        if (request.OperationGroupId is not null && !OperationGroupPattern.IsMatch(request.OperationGroupId))
        {
            throw new ArgumentException("Operation group identifier is invalid.", nameof(request.OperationGroupId));
        }

        if (request.Operations.Count == 0)
        {
            throw new ArgumentException("At least one operation is required.", nameof(request.Operations));
        }

        foreach (var operation in request.Operations)
        {
            ValidateOperation(operation);
        }

        foreach (var tileRef in request.TileReferences)
        {
            ValidateTileReference(tileRef);
        }
    }

    private static void ValidateContext(LayerEditEventContext context)
    {
        if (!FarmIdPattern.IsMatch(context.FarmId))
        {
            throw new ArgumentException("Farm identifier must match 'farm:*' format.", nameof(context));
        }

        if (context.FieldIds.Count == 0)
        {
            throw new ArgumentException("At least one field identifier is required.", nameof(context));
        }

        foreach (var fieldId in context.FieldIds)
        {
            if (!FieldIdPattern.IsMatch(fieldId))
            {
                throw new ArgumentException("Field identifier must match 'field:*' format.", nameof(context));
            }
        }

        if (context.SeasonId is not null && !SeasonIdPattern.IsMatch(context.SeasonId))
        {
            throw new ArgumentException("Season identifier must match 'season:*' format.", nameof(context));
        }
    }

    private static void ValidateTool(string tool)
    {
        if (!LayerEditTools.Allowed.Contains(tool))
        {
            throw new ArgumentException($"Tool '{tool}' is not a supported editing affordance.", nameof(tool));
        }
    }

    private static void ValidateOperation(LayerEditEventOperation operation)
    {
        if (!LayerEditOperationTypes.Allowed.Contains(operation.Type))
        {
            throw new ArgumentException($"Operation type '{operation.Type}' is not supported.", nameof(operation));
        }

        if (!LayerEditOperationTypes.FeaturePattern.IsMatch(operation.FeatureId))
        {
            throw new ArgumentException("Feature identifier must match 'feature:*' format.", nameof(operation));
        }

        if (operation.Type.Equals("merge", StringComparison.OrdinalIgnoreCase) && operation.SourceFeatureIds.Count == 0)
        {
            throw new ArgumentException("Merge operations must include at least one source feature identifier.", nameof(operation));
        }

        if (operation.Type.Equals("split", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(operation.SplitStrategy))
        {
            throw new ArgumentException("Split operations must include a split strategy.", nameof(operation));
        }

        foreach (var sourceId in operation.SourceFeatureIds)
        {
            if (!LayerEditOperationTypes.FeaturePattern.IsMatch(sourceId))
            {
                throw new ArgumentException("Source feature identifier must match 'feature:*' format.", nameof(operation));
            }
        }

        foreach (var vertex in operation.VertexEdits)
        {
            if (vertex.Index < 0)
            {
                throw new ArgumentException("Vertex index must be non-negative.", nameof(operation));
            }

            if (vertex.Before.Count < 2 || vertex.Before.Count > 3)
            {
                throw new ArgumentException("Vertex coordinates must contain 2 or 3 components.", nameof(operation));
            }

            if (vertex.After.Count < 2 || vertex.After.Count > 3)
            {
                throw new ArgumentException("Vertex coordinates must contain 2 or 3 components.", nameof(operation));
            }
        }

        foreach (var patch in operation.AttributePatches)
        {
            if (!LayerEditAttributePatchActions.Allowed.Contains(patch.Action))
            {
                throw new ArgumentException($"Attribute patch action '{patch.Action}' is invalid.", nameof(operation));
            }

            if (patch.Action.Equals("remove", StringComparison.OrdinalIgnoreCase) && patch.ValueNode is not null)
            {
                throw new ArgumentException("Remove patches must not include a value payload.", nameof(operation));
            }

            if (!patch.Action.Equals("remove", StringComparison.OrdinalIgnoreCase) && patch.ValueNode is null)
            {
                throw new ArgumentException("Add/replace patches must include a value payload.", nameof(operation));
            }
        }
    }

    private static void ValidateTileReference(LayerEditEventTileReference tileReference)
    {
        if (!TileIdPattern.IsMatch(tileReference.TileId))
        {
            throw new ArgumentException("Tile identifier must match 'tile:*' format.", nameof(tileReference));
        }

        if (!HashPattern.IsMatch(tileReference.Hash))
        {
            throw new ArgumentException("Tile hash must be a 16-64 character hex string.", nameof(tileReference));
        }
    }
}

/// <summary>
/// Contract for persisting <c>LayerEditEvent</c> journal entries.
/// </summary>
public interface ILayerEditEventJournalService
{
    /// <summary>
    /// Appends a new journal entry and returns the persisted document.
    /// </summary>
    ValueTask<LayerEditEventEntry> AppendAsync(LayerEditEventAppendRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the ordered journal for the specified layer.
    /// </summary>
    ValueTask<IReadOnlyList<LayerEditEventEntry>> ListAsync(string layerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Truncates journal entries after the specified hash. When <paramref name="keepHash"/> is <c>null</c>,
    /// the entire journal is cleared.
    /// </summary>
    ValueTask TruncateAsync(string layerId, string? keepHash, CancellationToken cancellationToken = default);
}

/// <summary>
/// Creation request used when appending a new journal entry.
/// </summary>
public sealed class LayerEditEventAppendRequest
{
    public LayerEditEventAppendRequest(
        string layerId,
        string jobId,
        LayerEditEventContext context,
        IReadOnlyList<LayerEditEventOperation> operations,
        string actor,
        string tool)
    {
        LayerId = layerId ?? throw new ArgumentNullException(nameof(layerId));
        JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Operations = operations is null ? throw new ArgumentNullException(nameof(operations)) : new ReadOnlyCollection<LayerEditEventOperation>(operations.ToArray());
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        Tool = tool ?? throw new ArgumentNullException(nameof(tool));
        TileReferences = Array.Empty<LayerEditEventTileReference>();
    }

    public string LayerId { get; }

    public string JobId { get; }

    public string? SessionId { get; init; }

    public LayerEditEventContext Context { get; }

    public IReadOnlyList<LayerEditEventOperation> Operations { get; }

    public string Actor { get; }

    public string Tool { get; }

    public string? OperationGroupId { get; init; }

    public IReadOnlyList<LayerEditEventTileReference> TileReferences { get; init; } = Array.Empty<LayerEditEventTileReference>();

    public JsonNode? Metadata { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>
/// Canonical representation of a persisted journal entry.
/// </summary>
public sealed class LayerEditEventEntry
{
    internal LayerEditEventEntry(
        string schemaVersion,
        string id,
        string layerId,
        string jobId,
        string? sessionId,
        LayerEditEventContext context,
        string tool,
        DateTimeOffset createdAt,
        string actor,
        string? previousHash,
        string? operationGroupId,
        IReadOnlyList<LayerEditEventTileReference> tileRefs,
        IReadOnlyList<LayerEditEventOperation> operations,
        JsonNode? metadata,
        string? notes)
    {
        SchemaVersion = schemaVersion;
        Id = id;
        LayerId = layerId;
        JobId = jobId;
        SessionId = sessionId;
        Context = context;
        Tool = tool;
        CreatedAt = createdAt;
        Actor = actor;
        PreviousHash = previousHash;
        OperationGroupId = operationGroupId;
        TileReferences = tileRefs ?? Array.Empty<LayerEditEventTileReference>();
        Operations = operations ?? Array.Empty<LayerEditEventOperation>();
        MetadataNode = metadata?.DeepClone();
        Notes = notes;
    }

    public string SchemaVersion { get; }

    public string Id { get; }

    public string LayerId { get; }

    public string JobId { get; }

    public string? SessionId { get; }

    public LayerEditEventContext Context { get; }

    public string Tool { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Actor { get; }

    public string? Hash { get; private set; }

    public string? PreviousHash { get; }

    public string? NextHash { get; private set; }

    public string? OperationGroupId { get; }

    public IReadOnlyList<LayerEditEventTileReference> TileReferences { get; }

    public IReadOnlyList<LayerEditEventOperation> Operations { get; }

    public JsonNode? Metadata => MetadataNode?.DeepClone();

    internal JsonNode? MetadataNode { get; }

    public string? Notes { get; }

    internal void SetHash(string hash)
    {
        Hash = hash;
    }

    internal void SetNextHash(string? nextHash)
    {
        NextHash = nextHash;
    }
}

/// <summary>
/// Describes the farm/field envelope associated with an edit session.
/// </summary>
public sealed class LayerEditEventContext
{
    public LayerEditEventContext(string farmId, IReadOnlyList<string> fieldIds, string? seasonId)
    {
        FarmId = farmId ?? throw new ArgumentNullException(nameof(farmId));
        FieldIds = fieldIds is null
            ? throw new ArgumentNullException(nameof(fieldIds))
            : new ReadOnlyCollection<string>(fieldIds.ToArray());
        SeasonId = seasonId;
    }

    public string FarmId { get; }

    public IReadOnlyList<string> FieldIds { get; }

    public string? SeasonId { get; }
}

/// <summary>
/// Represents a tile touched by an edit for provenance replay.
/// </summary>
public sealed class LayerEditEventTileReference
{
    public LayerEditEventTileReference(string tileId, string hash, string? purpose)
    {
        TileId = tileId ?? throw new ArgumentNullException(nameof(tileId));
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        Purpose = string.IsNullOrWhiteSpace(purpose) ? null : purpose;
    }

    public string TileId { get; }

    public string Hash { get; }

    public string? Purpose { get; }
}

/// <summary>
/// Operation recorded in the journal entry.
/// </summary>
public sealed class LayerEditEventOperation
{
    public LayerEditEventOperation(
        string type,
        string featureId,
        IReadOnlyList<string>? sourceFeatureIds = null,
        string? splitStrategy = null,
        JsonNode? geometryBefore = null,
        JsonNode? geometryAfter = null,
        JsonNode? attributesBefore = null,
        JsonNode? attributesAfter = null,
        IReadOnlyList<LayerEditEventVertexEdit>? vertexEdits = null,
        IReadOnlyList<LayerEditEventAttributePatch>? attributePatches = null,
        LayerEditEventOperationSummary? summary = null)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        FeatureId = featureId ?? throw new ArgumentNullException(nameof(featureId));
        SourceFeatureIds = new ReadOnlyCollection<string>((sourceFeatureIds ?? Array.Empty<string>()).ToArray());
        SplitStrategy = string.IsNullOrWhiteSpace(splitStrategy) ? null : splitStrategy;
        GeometryBeforeNode = geometryBefore?.DeepClone();
        GeometryAfterNode = geometryAfter?.DeepClone();
        AttributesBeforeNode = attributesBefore?.DeepClone();
        AttributesAfterNode = attributesAfter?.DeepClone();
        VertexEdits = new ReadOnlyCollection<LayerEditEventVertexEdit>((vertexEdits ?? Array.Empty<LayerEditEventVertexEdit>()).ToArray());
        AttributePatches = new ReadOnlyCollection<LayerEditEventAttributePatch>((attributePatches ?? Array.Empty<LayerEditEventAttributePatch>()).ToArray());
        Summary = summary;
    }

    public string Type { get; }

    public string FeatureId { get; }

    public IReadOnlyList<string> SourceFeatureIds { get; }

    public string? SplitStrategy { get; }

    public JsonNode? GeometryBefore => GeometryBeforeNode?.DeepClone();

    public JsonNode? GeometryAfter => GeometryAfterNode?.DeepClone();

    public JsonNode? AttributesBefore => AttributesBeforeNode?.DeepClone();

    public JsonNode? AttributesAfter => AttributesAfterNode?.DeepClone();

    public IReadOnlyList<LayerEditEventVertexEdit> VertexEdits { get; }

    public IReadOnlyList<LayerEditEventAttributePatch> AttributePatches { get; }

    public LayerEditEventOperationSummary? Summary { get; }

    internal JsonNode? GeometryBeforeNode { get; }

    internal JsonNode? GeometryAfterNode { get; }

    internal JsonNode? AttributesBeforeNode { get; }

    internal JsonNode? AttributesAfterNode { get; }
}

/// <summary>
/// Vertex mutation captured for analytics and replay.
/// </summary>
public sealed class LayerEditEventVertexEdit
{
    public LayerEditEventVertexEdit(int index, IReadOnlyList<double> before, IReadOnlyList<double> after)
    {
        Index = index;
        Before = new ReadOnlyCollection<double>((before ?? throw new ArgumentNullException(nameof(before))).ToArray());
        After = new ReadOnlyCollection<double>((after ?? throw new ArgumentNullException(nameof(after))).ToArray());
    }

    public int Index { get; }

    public IReadOnlyList<double> Before { get; }

    public IReadOnlyList<double> After { get; }
}

/// <summary>
/// JSON Patch style attribute mutation captured in the journal.
/// </summary>
public sealed class LayerEditEventAttributePatch
{
    public LayerEditEventAttributePatch(string path, string action, JsonNode? value)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Action = action ?? throw new ArgumentNullException(nameof(action));
        ValueNode = value?.DeepClone();
    }

    public string Path { get; }

    public string Action { get; }

    public JsonNode? Value => ValueNode?.DeepClone();

    internal JsonNode? ValueNode { get; }
}

/// <summary>
/// Quantitative summary describing the impact of an operation.
/// </summary>
public sealed class LayerEditEventOperationSummary
{
    public LayerEditEventOperationSummary(double? areaDeltaSqMeters, double? perimeterDeltaMeters, IReadOnlyList<string>? changedAttributes)
    {
        AreaDeltaSqMeters = areaDeltaSqMeters;
        PerimeterDeltaMeters = perimeterDeltaMeters;
        ChangedAttributes = new ReadOnlyCollection<string>((changedAttributes ?? Array.Empty<string>()).ToArray());
    }

    public double? AreaDeltaSqMeters { get; }

    public double? PerimeterDeltaMeters { get; }

    public IReadOnlyList<string> ChangedAttributes { get; }
}

internal static class LayerEditTools
{
    public static readonly ISet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "polygon",
        "rectangle",
        "brush",
        "eraser",
        "attribute",
        "merge",
        "split",
    };
}

internal static class LayerEditOperationTypes
{
    public static readonly ISet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "create",
        "update",
        "delete",
        "merge",
        "split",
    };

    public static readonly Regex FeaturePattern = new("^feature:[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
}

internal static class LayerEditAttributePatchActions
{
    public static readonly ISet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "add",
        "replace",
        "remove",
    };
}
