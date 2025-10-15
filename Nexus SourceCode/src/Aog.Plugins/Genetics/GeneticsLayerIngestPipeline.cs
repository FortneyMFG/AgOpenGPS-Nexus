using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aog.Core.Paths;

namespace Aog.Plugins.Genetics;

/// <summary>
/// Request describing a genetics plan feature to ingest.
/// </summary>
public sealed class GeneticsPlanIngestRequest
{
    /// <summary>Gets or sets the zone identifier.</summary>
    public string ZoneId { get; init; } = string.Empty;

    /// <summary>Gets or sets the outer boundary polygon.</summary>
    public IReadOnlyList<PlanarPoint> OuterBoundary { get; init; } = Array.Empty<PlanarPoint>();

    /// <summary>Gets or sets optional hole polygons.</summary>
    public IReadOnlyList<IReadOnlyList<PlanarPoint>>? Holes { get; init; }

    /// <summary>Gets or sets the seed brand.</summary>
    public string Brand { get; init; } = string.Empty;

    /// <summary>Gets or sets the product identifier.</summary>
    public string Product { get; init; } = string.Empty;

    /// <summary>Gets or sets the optional trait stack.</summary>
    public string? TraitStack { get; init; }

    /// <summary>Gets or sets the optional lot identifier.</summary>
    public string? Lot { get; init; }

    /// <summary>Gets or sets the optional treatment details.</summary>
    public string? Treatment { get; init; }

    /// <summary>Gets or sets the optional provenance source.</summary>
    public string? Source { get; init; }

    /// <summary>Gets or sets the optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets or sets the optional job association.</summary>
    public string? JobId { get; init; }

    /// <summary>Gets or sets the optional creation timestamp.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>Gets or sets the optional actor recorded for creation.</summary>
    public string? CreatedBy { get; init; }

    /// <summary>Gets or sets the optional last modification timestamp.</summary>
    public DateTimeOffset? LastModifiedAt { get; init; }
}

/// <summary>
/// Request describing an as-applied genetics feature to ingest.
/// </summary>
public sealed class GeneticsVarietyIngestRequest
{
    /// <summary>Gets or sets the zone identifier.</summary>
    public string ZoneId { get; init; } = string.Empty;

    /// <summary>Gets or sets the outer boundary polygon.</summary>
    public IReadOnlyList<PlanarPoint> OuterBoundary { get; init; } = Array.Empty<PlanarPoint>();

    /// <summary>Gets or sets optional hole polygons.</summary>
    public IReadOnlyList<IReadOnlyList<PlanarPoint>>? Holes { get; init; }

    /// <summary>Gets or sets the job identifier.</summary>
    public string JobId { get; init; } = string.Empty;

    /// <summary>Gets or sets the session identifier.</summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>Gets or sets the seed brand.</summary>
    public string Brand { get; init; } = string.Empty;

    /// <summary>Gets or sets the product identifier.</summary>
    public string Product { get; init; } = string.Empty;

    /// <summary>Gets or sets the optional trait stack.</summary>
    public string? TraitStack { get; init; }

    /// <summary>Gets or sets the optional lot identifier.</summary>
    public string? Lot { get; init; }

    /// <summary>Gets or sets the optional treatment description.</summary>
    public string? Treatment { get; init; }

    /// <summary>Gets or sets the optional provenance source.</summary>
    public string? Source { get; init; }

    /// <summary>Gets or sets the optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets or sets the optional barcode payload.</summary>
    public string? Barcode { get; init; }

    /// <summary>Gets or sets the optional change log entries.</summary>
    public IReadOnlyList<GeneticsChangeLogEntry>? ChangeLog { get; init; }

    /// <summary>Gets or sets the optional applied timestamp.</summary>
    public DateTimeOffset? AppliedAt { get; init; }

    /// <summary>Gets or sets the optional creation timestamp.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>Gets or sets the optional actor recorded for creation.</summary>
    public string? CreatedBy { get; init; }

    /// <summary>Gets or sets the optional last modification timestamp.</summary>
    public DateTimeOffset? LastModifiedAt { get; init; }
}

/// <summary>
/// Maintains in-memory plan and as-applied genetics features, enforcing schema contracts and provenance metadata.
/// </summary>
public sealed class GeneticsLayerIngestPipeline
{
    private readonly GeneticsLayerIngestOptions _options;
    private readonly Func<DateTimeOffset> _clock;
    private readonly object _sync = new();
    private readonly Dictionary<string, GeneticsPlanFeature> _plans = new(StringComparer.Ordinal);
    private readonly Dictionary<(string ZoneKey, string SessionKey), GeneticsVarietyFeature> _varieties = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsLayerIngestPipeline"/> class.
    /// </summary>
    /// <param name="options">Pipeline options. When <c>null</c>, defaults are used.</param>
    /// <param name="clock">Clock function used for deterministic testing.</param>
    public GeneticsLayerIngestPipeline(GeneticsLayerIngestOptions? options = null, Func<DateTimeOffset>? clock = null)
    {
        _options = options ?? new GeneticsLayerIngestOptions();
        _options.Validate();
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Ingests or updates a genetics plan feature.
    /// </summary>
    public GeneticsPlanFeature UpsertPlan(GeneticsPlanIngestRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var zoneDisplayId = NormalizeRequired(request.ZoneId, nameof(request.ZoneId));
        var zoneKey = CreateStableKey(zoneDisplayId);
        var geometry = new GeneticsZoneGeometry(request.OuterBoundary ?? Array.Empty<PlanarPoint>(), request.Holes);
        var brand = NormalizeRequired(request.Brand, nameof(request.Brand));
        var product = NormalizeRequired(request.Product, nameof(request.Product));
        var traitStack = NormalizeOptional(request.TraitStack);
        var lot = NormalizeOptional(request.Lot);
        var treatment = NormalizeOptional(request.Treatment);
        var source = NormalizeOptional(request.Source);
        var notes = NormalizeOptional(request.Notes);
        var jobId = NormalizeOptional(request.JobId);

        lock (_sync)
        {
            _plans.TryGetValue(zoneKey, out var existing);

            var createdAt = request.CreatedAt ?? existing?.CreatedAt ?? _clock();
            var createdBy = ResolveActor(request.CreatedBy, existing?.CreatedBy);
            var lastModifiedAt = existing is null ? request.LastModifiedAt : request.LastModifiedAt ?? _clock();
            var featureId = existing?.FeatureId ?? CreatePlanFeatureId(zoneKey);
            var effectiveSource = source ?? existing?.Source ?? _options.DefaultSource;
            var effectiveNotes = notes ?? existing?.Notes;
            var effectiveTraitStack = traitStack ?? existing?.TraitStack;
            var effectiveLot = lot ?? existing?.Lot;
            var effectiveTreatment = treatment ?? existing?.Treatment;
            var effectiveJobId = jobId ?? existing?.JobId;

            var feature = new GeneticsPlanFeature(
                zoneDisplayId,
                featureId,
                _options.PlanLayerId,
                effectiveJobId,
                createdAt,
                createdBy,
                lastModifiedAt,
                brand,
                product,
                effectiveTraitStack,
                effectiveLot,
                effectiveTreatment,
                effectiveSource,
                effectiveNotes,
                geometry);

            _plans[zoneKey] = feature;
            return feature;
        }
    }

    /// <summary>
    /// Ingests or updates an as-applied genetics variety feature.
    /// </summary>
    public GeneticsVarietyFeature UpsertVariety(GeneticsVarietyIngestRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var zoneDisplayId = NormalizeRequired(request.ZoneId, nameof(request.ZoneId));
        var zoneKey = CreateStableKey(zoneDisplayId);
        var sessionId = NormalizeRequired(request.SessionId, nameof(request.SessionId));
        var sessionKey = CreateStableKey(sessionId);
        var dictionaryKey = (zoneKey, sessionKey);
        var geometry = new GeneticsZoneGeometry(request.OuterBoundary ?? Array.Empty<PlanarPoint>(), request.Holes);
        var brand = NormalizeRequired(request.Brand, nameof(request.Brand));
        var product = NormalizeRequired(request.Product, nameof(request.Product));
        var traitStack = NormalizeOptional(request.TraitStack);
        var lot = NormalizeOptional(request.Lot);
        var treatment = NormalizeOptional(request.Treatment);
        var source = NormalizeOptional(request.Source);
        var notes = NormalizeOptional(request.Notes);
        var barcode = NormalizeOptional(request.Barcode);
        var jobId = NormalizeRequired(request.JobId, nameof(request.JobId));
        var changeLog = NormalizeChangeLog(request.ChangeLog);

        lock (_sync)
        {
            _varieties.TryGetValue(dictionaryKey, out var existing);

            var createdAt = request.CreatedAt ?? existing?.CreatedAt ?? _clock();
            var createdBy = ResolveActor(request.CreatedBy, existing?.CreatedBy);
            var appliedAt = request.AppliedAt ?? existing?.AppliedAt ?? _clock();
            var lastModifiedAt = existing is null ? request.LastModifiedAt : request.LastModifiedAt ?? _clock();
            var featureId = existing?.FeatureId ?? CreateVarietyFeatureId(sessionKey, zoneKey);
            var effectiveSource = source ?? existing?.Source ?? _options.DefaultSource;
            var effectiveNotes = notes ?? existing?.Notes;
            var effectiveTraitStack = traitStack ?? existing?.TraitStack;
            var effectiveLot = lot ?? existing?.Lot;
            var effectiveTreatment = treatment ?? existing?.Treatment;
            var effectiveBarcode = barcode ?? existing?.Barcode;
            var effectiveChangeLog = changeLog ?? existing?.ChangeLog ?? Array.Empty<GeneticsChangeLogEntry>();

            var feature = new GeneticsVarietyFeature(
                zoneDisplayId,
                featureId,
                _options.VarietyLayerId,
                jobId,
                sessionId,
                createdAt,
                createdBy,
                lastModifiedAt,
                appliedAt,
                brand,
                product,
                effectiveTraitStack,
                effectiveLot,
                effectiveTreatment,
                effectiveSource,
                effectiveBarcode,
                effectiveChangeLog,
                effectiveNotes,
                geometry);

            _varieties[dictionaryKey] = feature;
            return feature;
        }
    }

    /// <summary>
    /// Attempts to retrieve a plan feature by zone identifier.
    /// </summary>
    public bool TryGetPlan(string zoneId, out GeneticsPlanFeature feature)
    {
        var zoneKey = CreateStableKey(NormalizeRequired(zoneId, nameof(zoneId)));
        lock (_sync)
        {
            if (_plans.TryGetValue(zoneKey, out var resolved))
            {
                feature = resolved;
                return true;
            }
        }

        feature = null!;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve an as-applied feature by zone and session identifier.
    /// </summary>
    public bool TryGetVariety(string zoneId, string sessionId, out GeneticsVarietyFeature feature)
    {
        var zoneKey = CreateStableKey(NormalizeRequired(zoneId, nameof(zoneId)));
        var sessionKey = CreateStableKey(NormalizeRequired(sessionId, nameof(sessionId)));
        lock (_sync)
        {
            if (_varieties.TryGetValue((zoneKey, sessionKey), out var resolved))
            {
                feature = resolved;
                return true;
            }
        }

        feature = null!;
        return false;
    }

    /// <summary>
    /// Removes a plan feature by zone identifier.
    /// </summary>
    public bool RemovePlan(string zoneId)
    {
        var zoneKey = CreateStableKey(NormalizeRequired(zoneId, nameof(zoneId)));
        lock (_sync)
        {
            return _plans.Remove(zoneKey);
        }
    }

    /// <summary>
    /// Removes an as-applied feature by zone and session identifier.
    /// </summary>
    public bool RemoveVariety(string zoneId, string sessionId)
    {
        var zoneKey = CreateStableKey(NormalizeRequired(zoneId, nameof(zoneId)));
        var sessionKey = CreateStableKey(NormalizeRequired(sessionId, nameof(sessionId)));
        lock (_sync)
        {
            return _varieties.Remove((zoneKey, sessionKey));
        }
    }

    /// <summary>
    /// Returns a snapshot of the current plan features ordered by creation timestamp.
    /// </summary>
    public IReadOnlyList<GeneticsPlanFeature> GetPlanFeatures()
    {
        lock (_sync)
        {
            var ordered = new List<GeneticsPlanFeature>(_plans.Values);
            ordered.Sort((left, right) =>
            {
                var comparison = left.CreatedAt.CompareTo(right.CreatedAt);
                return comparison != 0
                    ? comparison
                    : string.Compare(left.FeatureId, right.FeatureId, StringComparison.Ordinal);
            });
            return new ReadOnlyCollection<GeneticsPlanFeature>(ordered);
        }
    }

    /// <summary>
    /// Returns a snapshot of the current variety features ordered by applied timestamp.
    /// </summary>
    public IReadOnlyList<GeneticsVarietyFeature> GetVarietyFeatures()
    {
        lock (_sync)
        {
            var ordered = new List<GeneticsVarietyFeature>(_varieties.Values);
            ordered.Sort((left, right) =>
            {
                var comparison = left.AppliedAt.CompareTo(right.AppliedAt);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = string.Compare(left.SessionId, right.SessionId, StringComparison.Ordinal);
                if (comparison != 0)
                {
                    return comparison;
                }

                return string.Compare(left.FeatureId, right.FeatureId, StringComparison.Ordinal);
            });
            return new ReadOnlyCollection<GeneticsVarietyFeature>(ordered);
        }
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private string ResolveActor(string? candidate, string? existing)
    {
        return NormalizeOptional(candidate)
            ?? existing
            ?? _options.DefaultActor;
    }

    private static IReadOnlyList<GeneticsChangeLogEntry>? NormalizeChangeLog(IReadOnlyList<GeneticsChangeLogEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return null;
        }

        var ordered = new List<GeneticsChangeLogEntry>(entries);
        ordered.Sort((left, right) =>
        {
            var comparison = left.ChangedAt.CompareTo(right.ChangedAt);
            return comparison != 0
                ? comparison
                : string.Compare(left.Field, right.Field, StringComparison.Ordinal);
        });

        return new ReadOnlyCollection<GeneticsChangeLogEntry>(ordered);
    }

    private static string CreatePlanFeatureId(string zoneKey) => $"geneticsPlan:{zoneKey}";

    private static string CreateVarietyFeatureId(string sessionKey, string zoneKey) => $"geneticsVariety:{sessionKey}:{zoneKey}";

    private static string CreateStableKey(string value)
    {
        var trimmed = value.Trim();
        var builder = new StringBuilder(trimmed.Length);
        char previous = '\0';

        foreach (var ch in trimmed)
        {
            char mapped;
            if (char.IsLetterOrDigit(ch))
            {
                mapped = char.ToLowerInvariant(ch);
            }
            else if (ch is '.' or '_' or '-' or ':')
            {
                mapped = char.ToLowerInvariant(ch);
            }
            else if (char.IsWhiteSpace(ch))
            {
                mapped = '-';
            }
            else
            {
                mapped = '-';
            }

            if (mapped == '-' && previous == '-')
            {
                continue;
            }

            builder.Append(mapped);
            previous = mapped;
        }

        var sanitized = builder.ToString().Trim('-');
        if (sanitized.Length == 0)
        {
            sanitized = ComputeHashToken(trimmed);
        }

        return sanitized;
    }

    private static string ComputeHashToken(string value)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha.ComputeHash(bytes);
        var hex = Convert.ToHexString(hash).ToLowerInvariant(CultureInfo.InvariantCulture);
        return hex[..12];
    }
}
