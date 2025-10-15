using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Reporting;

namespace Aog.Plugins.Crop;

/// <summary>
/// Contributes crop analytics summaries to the report builder.
/// </summary>
public sealed class CropReportSectionContributor : IReportSectionContributor
{
    private readonly ICropAnalyticsProvider _provider;
    private readonly ConcurrentDictionary<string, CropAnalyticsSnapshot> _prepared = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="CropReportSectionContributor"/> class.
    /// </summary>
    /// <param name="provider">Analytics provider supplying crop snapshots.</param>
    public CropReportSectionContributor(ICropAnalyticsProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    public ReportSectionDescriptor Descriptor { get; } = new(
        sectionId: "crop.analytics.summary",
        displayName: "Crop Analytics Summary",
        capabilities: new[] { "crop.analytics", "rotation" },
        requiredContext: new[] { "seasonId", "jobId" },
        missingDataMessage: "Crop analytics snapshot is unavailable for this context.");

    /// <inheritdoc />
    public async ValueTask<ReportSectionPreparationResult> PrepareAsync(ReportGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = await _provider.GetSnapshotAsync(context, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
        {
            return new ReportSectionPreparationResult(false, Descriptor.MissingDataMessage);
        }

        _prepared[CreateKey(context)] = snapshot;
        return new ReportSectionPreparationResult(true);
    }

    /// <inheritdoc />
    public async ValueTask<ReportSectionResult> RenderAsync(ReportSectionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var key = CreateKey(context.Generation);
        if (!_prepared.TryRemove(key, out var snapshot))
        {
            snapshot = await _provider.GetSnapshotAsync(context.Generation, cancellationToken).ConfigureAwait(false);
        }

        if (snapshot is null)
        {
            return new ReportSectionResult(
                Descriptor.SectionId,
                ReportSectionStatus.MissingData,
                Descriptor.MissingDataMessage);
        }

        var payload = new CropReportSectionPayload(
            snapshot.GeneratedAt,
            snapshot.Planned,
            snapshot.Actual,
            snapshot.Historical,
            snapshot.Rotations,
            snapshot.LatestByField,
            snapshot.HistoryByRecency);

        return new ReportSectionResult(Descriptor.SectionId, ReportSectionStatus.Success, payload: payload);
    }

    private static string CreateKey(ReportGenerationContext context)
    {
        return string.Concat(context.Template.Id, "|", context.Scope.Kind, "|", context.Scope.Identifier);
    }
}

/// <summary>
/// Structured payload returned by the crop analytics report section.
/// </summary>
public sealed class CropReportSectionPayload
{
    public CropReportSectionPayload(
        DateTimeOffset generatedAt,
        IEnumerable<CropAcreageSummary> planned,
        IEnumerable<CropAcreageSummary> actual,
        IEnumerable<CropAcreageSummary> historical,
        IEnumerable<CropRotationSummary> rotations,
        IReadOnlyDictionary<string, CropHistoryRecord> latestByField,
        IEnumerable<CropHistoryRecord> historyByRecency)
    {
        GeneratedAt = generatedAt;
        Planned = new ReadOnlyCollection<CropAcreageSummary>((planned ?? Array.Empty<CropAcreageSummary>()).ToArray());
        Actual = new ReadOnlyCollection<CropAcreageSummary>((actual ?? Array.Empty<CropAcreageSummary>()).ToArray());
        Historical = new ReadOnlyCollection<CropAcreageSummary>((historical ?? Array.Empty<CropAcreageSummary>()).ToArray());
        Rotations = new ReadOnlyCollection<CropRotationSummary>((rotations ?? Array.Empty<CropRotationSummary>()).ToArray());

        var fieldLookup = new Dictionary<string, CropHistoryRecord>(StringComparer.OrdinalIgnoreCase);
        if (latestByField is not null)
        {
            foreach (var kvp in latestByField)
            {
                fieldLookup[kvp.Key] = kvp.Value.Clone();
            }
        }

        LatestByField = new ReadOnlyDictionary<string, CropHistoryRecord>(fieldLookup);
        History = new ReadOnlyCollection<CropHistoryRecord>((historyByRecency ?? Array.Empty<CropHistoryRecord>())
            .Select(record => record.Clone())
            .ToArray());
    }

    /// <summary>
    /// Gets the timestamp when the analytics snapshot was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>
    /// Gets aggregated acreage summaries for planned crop layers.
    /// </summary>
    public IReadOnlyList<CropAcreageSummary> Planned { get; }

    /// <summary>
    /// Gets aggregated acreage summaries for actual crop layers.
    /// </summary>
    public IReadOnlyList<CropAcreageSummary> Actual { get; }

    /// <summary>
    /// Gets aggregated acreage summaries for historical crop layers.
    /// </summary>
    public IReadOnlyList<CropAcreageSummary> Historical { get; }

    /// <summary>
    /// Gets rotation summaries derived from history.
    /// </summary>
    public IReadOnlyList<CropRotationSummary> Rotations { get; }

    /// <summary>
    /// Gets the most recent crop record per field.
    /// </summary>
    public IReadOnlyDictionary<string, CropHistoryRecord> LatestByField { get; }

    /// <summary>
    /// Gets historical records ordered by recency.
    /// </summary>
    public IReadOnlyList<CropHistoryRecord> History { get; }
}
