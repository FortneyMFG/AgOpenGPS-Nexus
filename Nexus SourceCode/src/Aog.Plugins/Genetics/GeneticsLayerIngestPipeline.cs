using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using PlanarPoint = Aog.Core.Paths.PlanarPoint;

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
    private readonly Dictionary<Guid, GeneticsAnalyticsCallback> _analyticsCallbacks = new();
    private GeneticsAnalyticsSnapshot _latestAnalytics;

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
        _latestAnalytics = CreateAnalyticsSnapshotUnsafe();
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

        GeneticsPlanFeature feature;
        GeneticsAnalyticsSnapshot previousSnapshot;
        GeneticsAnalyticsSnapshot currentSnapshot;

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

            feature = new GeneticsPlanFeature(
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
            previousSnapshot = _latestAnalytics;
            currentSnapshot = CreateAnalyticsSnapshotUnsafe();
            _latestAnalytics = currentSnapshot;
        }

        NotifyAnalyticsCallbacks(previousSnapshot, currentSnapshot);
        return feature;
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

        GeneticsVarietyFeature feature;
        GeneticsAnalyticsSnapshot previousSnapshot;
        GeneticsAnalyticsSnapshot currentSnapshot;

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
            var effectiveChangeLog = ResolveChangeLog(changeLog, existing, createdBy, lastModifiedAt, brand, product, effectiveTraitStack, effectiveLot, effectiveTreatment, effectiveSource, effectiveBarcode, effectiveNotes);

            feature = new GeneticsVarietyFeature(
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
            previousSnapshot = _latestAnalytics;
            currentSnapshot = CreateAnalyticsSnapshotUnsafe();
            _latestAnalytics = currentSnapshot;
        }

        NotifyAnalyticsCallbacks(previousSnapshot, currentSnapshot);
        return feature;
    }

    /// <summary>
    /// Attempts to retrieve a plan feature by zone identifier.
    /// </summary>
    public bool TryGetPlan(string zoneId, [NotNullWhen(true)] out GeneticsPlanFeature? feature)
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

        feature = null;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve an as-applied feature by zone and session identifier.
    /// </summary>
    public bool TryGetVariety(string zoneId, string sessionId, [NotNullWhen(true)] out GeneticsVarietyFeature? feature)
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

        feature = null;
        return false;
    }

    /// <summary>
    /// Removes a plan feature by zone identifier.
    /// </summary>
    public bool RemovePlan(string zoneId)
    {
        var zoneKey = CreateStableKey(NormalizeRequired(zoneId, nameof(zoneId)));
        GeneticsAnalyticsSnapshot previousSnapshot;
        GeneticsAnalyticsSnapshot currentSnapshot;
        bool removed;

        lock (_sync)
        {
            removed = _plans.Remove(zoneKey);
            if (!removed)
            {
                return false;
            }

            previousSnapshot = _latestAnalytics;
            currentSnapshot = CreateAnalyticsSnapshotUnsafe();
            _latestAnalytics = currentSnapshot;
        }

        NotifyAnalyticsCallbacks(previousSnapshot, currentSnapshot);
        return true;
    }

    /// <summary>
    /// Removes an as-applied feature by zone and session identifier.
    /// </summary>
    public bool RemoveVariety(string zoneId, string sessionId)
    {
        var zoneKey = CreateStableKey(NormalizeRequired(zoneId, nameof(zoneId)));
        var sessionKey = CreateStableKey(NormalizeRequired(sessionId, nameof(sessionId)));
        GeneticsAnalyticsSnapshot previousSnapshot;
        GeneticsAnalyticsSnapshot currentSnapshot;
        bool removed;

        lock (_sync)
        {
            removed = _varieties.Remove((zoneKey, sessionKey));
            if (!removed)
            {
                return false;
            }

            previousSnapshot = _latestAnalytics;
            currentSnapshot = CreateAnalyticsSnapshotUnsafe();
            _latestAnalytics = currentSnapshot;
        }

        NotifyAnalyticsCallbacks(previousSnapshot, currentSnapshot);
        return true;
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

    /// <summary>
    /// Gets the latest analytics snapshot produced by the pipeline.
    /// </summary>
    public GeneticsAnalyticsSnapshot LatestAnalytics
    {
        get
        {
            lock (_sync)
            {
                return _latestAnalytics;
            }
        }
    }

    /// <summary>
    /// Registers an analytics callback invoked whenever plan or variety features change.
    /// </summary>
    /// <param name="callback">Callback invoked with the latest and previous analytics snapshot.</param>
    /// <param name="replayLatest">When <c>true</c>, replays the current snapshot immediately.</param>
    public IDisposable RegisterAnalyticsCallback(GeneticsAnalyticsCallback callback, bool replayLatest = true)
    {
        if (callback is null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        var id = Guid.NewGuid();

        lock (_analyticsCallbacks)
        {
            _analyticsCallbacks[id] = callback;
        }

        if (replayLatest)
        {
            GeneticsAnalyticsSnapshot snapshot;
            lock (_sync)
            {
                snapshot = _latestAnalytics;
            }

            callback(snapshot, previous: null);
        }

        return new AnalyticsSubscription(this, id);
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

    private IReadOnlyList<GeneticsChangeLogEntry> ResolveChangeLog(
        IReadOnlyList<GeneticsChangeLogEntry>? requested,
        GeneticsVarietyFeature? existing,
        string actor,
        DateTimeOffset? lastModifiedAt,
        string brand,
        string product,
        string? traitStack,
        string? lot,
        string? treatment,
        string? source,
        string? barcode,
        string? notes)
    {
        if (requested is not null)
        {
            return requested;
        }

        if (existing is null)
        {
            return Array.Empty<GeneticsChangeLogEntry>();
        }

        var timestamp = lastModifiedAt ?? _clock();
        var entries = new List<GeneticsChangeLogEntry>(existing.ChangeLog);
        var mutations = DetectMutations(existing, brand, product, traitStack, lot, treatment, source, barcode, notes);
        foreach (var mutation in mutations)
        {
            entries.Add(new GeneticsChangeLogEntry(timestamp, actor, mutation.Field, mutation.Previous, mutation.Current));
        }

        return entries;
    }

    private static List<(string Field, string? Previous, string? Current)> DetectMutations(
        GeneticsVarietyFeature existing,
        string brand,
        string product,
        string? traitStack,
        string? lot,
        string? treatment,
        string? source,
        string? barcode,
        string? notes)
    {
        var mutations = new List<(string Field, string? Previous, string? Current)>();

        if (!string.Equals(existing.Brand, brand, StringComparison.Ordinal))
        {
            mutations.Add(("brand", existing.Brand, brand));
        }

        if (!string.Equals(existing.Product, product, StringComparison.Ordinal))
        {
            mutations.Add(("product", existing.Product, product));
        }

        if (!string.Equals(existing.TraitStack, traitStack, StringComparison.Ordinal))
        {
            mutations.Add(("traitStack", existing.TraitStack, traitStack));
        }

        if (!string.Equals(existing.Lot, lot, StringComparison.Ordinal))
        {
            mutations.Add(("lot", existing.Lot, lot));
        }

        if (!string.Equals(existing.Treatment, treatment, StringComparison.Ordinal))
        {
            mutations.Add(("treatment", existing.Treatment, treatment));
        }

        if (!string.Equals(existing.Source, source, StringComparison.Ordinal))
        {
            mutations.Add(("source", existing.Source, source));
        }

        if (!string.Equals(existing.Barcode, barcode, StringComparison.Ordinal))
        {
            mutations.Add(("barcode", existing.Barcode, barcode));
        }

        if (!string.Equals(existing.Notes, notes, StringComparison.Ordinal))
        {
            mutations.Add(("notes", existing.Notes, notes));
        }

        if (mutations.Count > 1)
        {
            mutations.Sort((left, right) => string.CompareOrdinal(left.Field, right.Field));
        }

        return mutations;
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
    var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return hex[..12];
    }

    private GeneticsAnalyticsSnapshot CreateAnalyticsSnapshotUnsafe()
    {
        var generatedAt = _clock();

        var planSummaries = _plans.Values
            .GroupBy(plan => new PlanKey(plan.JobId, plan.Brand, plan.Product, plan.TraitStack, plan.Lot, plan.Treatment), PlanKey.Comparer)
            .Select(group => new GeneticsPlanAnalytics(
                group.Key.JobId,
                group.Key.Brand,
                group.Key.Product,
                group.Key.TraitStack,
                group.Key.Lot,
                group.Key.Treatment,
                group.Count(),
                group.Sum(feature => feature.Geometry.AreaSquareMeters)))
            .OrderBy(summary => summary.Brand, StringComparer.Ordinal)
            .ThenBy(summary => summary.Product, StringComparer.Ordinal)
            .ThenBy(summary => summary.Lot, StringComparer.Ordinal)
            .ThenBy(summary => summary.JobId, StringComparer.Ordinal)
            .ToList();

        var varietySummaries = _varieties.Values
            .GroupBy(variety => new VarietyKey(variety.JobId, variety.Brand, variety.Product, variety.TraitStack, variety.Lot, variety.Treatment, variety.Barcode), VarietyKey.Comparer)
            .Select(group => new GeneticsVarietyAnalytics(
                group.Key.JobId,
                group.Key.Brand,
                group.Key.Product,
                group.Key.TraitStack,
                group.Key.Lot,
                group.Key.Treatment,
                group.Key.Barcode,
                group.Select(feature => feature.SessionId).Distinct(StringComparer.Ordinal).Count(),
                group.Count(),
                group.Sum(feature => feature.Geometry.AreaSquareMeters)))
            .OrderBy(summary => summary.JobId, StringComparer.Ordinal)
            .ThenBy(summary => summary.Brand, StringComparer.Ordinal)
            .ThenBy(summary => summary.Product, StringComparer.Ordinal)
            .ThenBy(summary => summary.Lot, StringComparer.Ordinal)
            .ThenBy(summary => summary.Barcode, StringComparer.Ordinal)
            .ToList();

        var plannedLots = _plans.Values
            .Where(feature => !string.IsNullOrEmpty(feature.Lot))
            .GroupBy(feature => feature.Lot!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => new LotAggregate(group.Count(), group.Sum(feature => feature.Geometry.AreaSquareMeters)),
                StringComparer.Ordinal);

        var appliedLots = _varieties.Values
            .Where(feature => !string.IsNullOrEmpty(feature.Lot))
            .GroupBy(feature => feature.Lot!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => new LotAggregate(group.Count(), group.Sum(feature => feature.Geometry.AreaSquareMeters)),
                StringComparer.Ordinal);

        var lotKeys = plannedLots.Keys
            .Concat(appliedLots.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        var lotSummaries = new List<GeneticsLotStatistic>(lotKeys.Count);
        foreach (var lotKey in lotKeys)
        {
            plannedLots.TryGetValue(lotKey, out var planned);
            appliedLots.TryGetValue(lotKey, out var applied);

            lotSummaries.Add(new GeneticsLotStatistic(
                lotKey,
                planned?.AreaSquareMeters ?? 0,
                applied?.AreaSquareMeters ?? 0,
                planned?.Count ?? 0,
                applied?.Count ?? 0));
        }

        return new GeneticsAnalyticsSnapshot(
            generatedAt,
            planSummaries,
            varietySummaries,
            lotSummaries);
    }

    private void NotifyAnalyticsCallbacks(GeneticsAnalyticsSnapshot previous, GeneticsAnalyticsSnapshot current)
    {
        GeneticsAnalyticsCallback[] callbacks;
        lock (_analyticsCallbacks)
        {
            if (_analyticsCallbacks.Count == 0)
            {
                return;
            }

            callbacks = _analyticsCallbacks.Values.ToArray();
        }

        foreach (var callback in callbacks)
        {
            callback(current, previous);
        }
    }

    private void UnregisterAnalyticsCallback(Guid id)
    {
        lock (_analyticsCallbacks)
        {
            _analyticsCallbacks.Remove(id);
        }
    }

    private readonly struct PlanKey
    {
        public PlanKey(string? jobId, string brand, string product, string? traitStack, string? lot, string? treatment)
        {
            JobId = jobId;
            Brand = brand;
            Product = product;
            TraitStack = traitStack;
            Lot = lot;
            Treatment = treatment;
        }

        public string? JobId { get; }

        public string Brand { get; }

        public string Product { get; }

        public string? TraitStack { get; }

        public string? Lot { get; }

        public string? Treatment { get; }

        public static IEqualityComparer<PlanKey> Comparer { get; } = new PlanKeyEqualityComparer();

        private sealed class PlanKeyEqualityComparer : IEqualityComparer<PlanKey>
        {
            public bool Equals(PlanKey x, PlanKey y)
            {
                return string.Equals(x.JobId, y.JobId, StringComparison.Ordinal)
                    && string.Equals(x.Brand, y.Brand, StringComparison.Ordinal)
                    && string.Equals(x.Product, y.Product, StringComparison.Ordinal)
                    && string.Equals(x.TraitStack, y.TraitStack, StringComparison.Ordinal)
                    && string.Equals(x.Lot, y.Lot, StringComparison.Ordinal)
                    && string.Equals(x.Treatment, y.Treatment, StringComparison.Ordinal);
            }

            public int GetHashCode(PlanKey obj)
            {
                var hash = new HashCode();
                hash.Add(obj.JobId, StringComparer.Ordinal);
                hash.Add(obj.Brand, StringComparer.Ordinal);
                hash.Add(obj.Product, StringComparer.Ordinal);
                hash.Add(obj.TraitStack, StringComparer.Ordinal);
                hash.Add(obj.Lot, StringComparer.Ordinal);
                hash.Add(obj.Treatment, StringComparer.Ordinal);
                return hash.ToHashCode();
            }
        }
    }

    private readonly struct VarietyKey
    {
        public VarietyKey(string jobId, string brand, string product, string? traitStack, string? lot, string? treatment, string? barcode)
        {
            JobId = jobId;
            Brand = brand;
            Product = product;
            TraitStack = traitStack;
            Lot = lot;
            Treatment = treatment;
            Barcode = barcode;
        }

        public string JobId { get; }

        public string Brand { get; }

        public string Product { get; }

        public string? TraitStack { get; }

        public string? Lot { get; }

        public string? Treatment { get; }

        public string? Barcode { get; }

        public static IEqualityComparer<VarietyKey> Comparer { get; } = new VarietyKeyEqualityComparer();

        private sealed class VarietyKeyEqualityComparer : IEqualityComparer<VarietyKey>
        {
            public bool Equals(VarietyKey x, VarietyKey y)
            {
                return string.Equals(x.JobId, y.JobId, StringComparison.Ordinal)
                    && string.Equals(x.Brand, y.Brand, StringComparison.Ordinal)
                    && string.Equals(x.Product, y.Product, StringComparison.Ordinal)
                    && string.Equals(x.TraitStack, y.TraitStack, StringComparison.Ordinal)
                    && string.Equals(x.Lot, y.Lot, StringComparison.Ordinal)
                    && string.Equals(x.Treatment, y.Treatment, StringComparison.Ordinal)
                    && string.Equals(x.Barcode, y.Barcode, StringComparison.Ordinal);
            }

            public int GetHashCode(VarietyKey obj)
            {
                var hash = new HashCode();
                hash.Add(obj.JobId?.ToUpperInvariant() ?? string.Empty);
                hash.Add(obj.Brand?.ToUpperInvariant() ?? string.Empty);
                hash.Add(obj.Product?.ToUpperInvariant() ?? string.Empty);
                hash.Add(obj.TraitStack?.ToUpperInvariant() ?? string.Empty);
                hash.Add(obj.Lot?.ToUpperInvariant() ?? string.Empty);
                hash.Add(obj.Treatment?.ToUpperInvariant() ?? string.Empty);
                hash.Add(obj.Barcode?.ToUpperInvariant() ?? string.Empty);
                return hash.ToHashCode();
            }
        }
    }

    private sealed class AnalyticsSubscription : IDisposable
    {
        private readonly GeneticsLayerIngestPipeline _pipeline;
        private readonly Guid _id;
        private bool _disposed;

        public AnalyticsSubscription(GeneticsLayerIngestPipeline pipeline, Guid id)
        {
            _pipeline = pipeline;
            _id = id;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _pipeline.UnregisterAnalyticsCallback(_id);
            _disposed = true;
        }
    }

    private sealed record LotAggregate(int Count, double AreaSquareMeters);
}
