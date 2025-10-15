using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Reporting;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Provides field health content for report builder templates.
/// </summary>
public sealed class FieldHealthReportSectionContributor : IReportSectionContributor
{
    private const string SectionIdentifier = "fieldHealth.summary";

    private readonly IFieldHealthReportDataSource _dataSource;
    private readonly FieldHealthReportSectionOptions _options;
    private readonly ConcurrentDictionary<string, FieldHealthReportSnapshot> _snapshotCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initialises a new instance of the <see cref="FieldHealthReportSectionContributor"/> class.
    /// </summary>
    /// <param name="dataSource">Source providing report data snapshots.</param>
    /// <param name="options">Optional configuration controlling highlight behaviour.</param>
    public FieldHealthReportSectionContributor(
        IFieldHealthReportDataSource dataSource,
        FieldHealthReportSectionOptions? options = null)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _options = options ?? FieldHealthReportSectionOptions.Default;

        Descriptor = new ReportSectionDescriptor(
            SectionIdentifier,
            "Field Health Summary",
            capabilities: new[] { "fieldHealth", "risk" },
            requiredContext: new[] { "field", "season" },
            missingDataMessage: "Field health observations are not available for the selected scope.");
    }

    /// <inheritdoc />
    public ReportSectionDescriptor Descriptor { get; }

    /// <inheritdoc />
    public async ValueTask<ReportSectionPreparationResult> PrepareAsync(ReportGenerationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = await _dataSource.GetSnapshotAsync(context.Scope, cancellationToken).ConfigureAwait(false);
        var cacheKey = CreateCacheKey(context);

        if (!HasData(snapshot))
        {
            _snapshotCache.TryRemove(cacheKey, out _);
            return new ReportSectionPreparationResult(
                false,
                Descriptor.MissingDataMessage,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["reason"] = "no-data",
                });
        }

        _snapshotCache[cacheKey] = snapshot!;
        return new ReportSectionPreparationResult(true);
    }

    /// <inheritdoc />
    public async ValueTask<ReportSectionResult> RenderAsync(ReportSectionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = CreateCacheKey(context.Generation);
        if (!_snapshotCache.TryRemove(cacheKey, out var snapshot))
        {
            snapshot = await _dataSource.GetSnapshotAsync(context.Generation.Scope, cancellationToken).ConfigureAwait(false);
        }

        if (!HasData(snapshot))
        {
            return new ReportSectionResult(
                context.TemplateSection.SectionId,
                context.TemplateSection.IsOptional ? ReportSectionStatus.Skipped : ReportSectionStatus.MissingData,
                Descriptor.MissingDataMessage,
                diagnostics: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["reason"] = "no-data",
                });
        }

        var payload = BuildPayload(context.Generation, snapshot!);
        var diagnostics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["layerCount"] = payload.Layers.Count.ToString(CultureInfo.InvariantCulture),
            ["observationCount"] = payload.Layers.Sum(layer => layer.ObservationCount).ToString(CultureInfo.InvariantCulture),
        };

        var message = string.Format(
            CultureInfo.InvariantCulture,
            "Field health summary across {0} layer(s).",
            payload.Layers.Count);

        return new ReportSectionResult(
            context.TemplateSection.SectionId,
            ReportSectionStatus.Success,
            message,
            payload,
            diagnostics: diagnostics);
    }

    private FieldHealthReportPayload BuildPayload(ReportGenerationContext context, FieldHealthReportSnapshot snapshot)
    {
        var orderedLayers = snapshot.Layers
            .OrderBy(layer => layer.Kind, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var summaries = new List<FieldHealthReportLayerSummary>(orderedLayers.Length);
        var severityTotals = new FieldHealthSeverityCounts(0, 0, 0, 0, 0);
        double totalArea = 0;
        DateTimeOffset? lastSurveyed = null;

        foreach (var layer in orderedLayers)
        {
            var statistics = layer.Statistics;
            severityTotals = severityTotals.Add(statistics.SeverityCounts);
            totalArea += statistics.TotalAreaHa;

            if (statistics.LastSurveyedAt is { } surveyed)
            {
                lastSurveyed = lastSurveyed is { } existing && existing > surveyed ? existing : surveyed;
            }

            var highlights = BuildHighlights(layer);
            summaries.Add(new FieldHealthReportLayerSummary(
                layer.Kind,
                layer.Observations.Count,
                statistics.TotalAreaHa,
                statistics.SeverityCounts,
                statistics.LastSurveyedAt,
                layer.Notes,
                layer.Tags.ToArray(),
                highlights));
        }

        return new FieldHealthReportPayload(
            context.Scope.Identifier,
            snapshot.ScopeDisplayName,
            severityTotals,
            totalArea,
            lastSurveyed,
            summaries,
            snapshot.Summary);
    }

    private IReadOnlyList<FieldHealthReportObservationSummary> BuildHighlights(FieldHealthLayerMetadata layer)
    {
        if (_options.HighlightObservationCount == 0 || layer.Observations.Count == 0)
        {
            return Array.Empty<FieldHealthReportObservationSummary>();
        }

        return layer.Observations
            .OrderByDescending(obs => obs.Severity)
            .ThenByDescending(obs => obs.ObservedAt)
            .Take(_options.HighlightObservationCount)
            .Select(obs => new FieldHealthReportObservationSummary(
                obs.FeatureId,
                obs.Severity,
                obs.ObservedAt,
                obs.Observer,
                obs.Status,
                obs.AreaHa,
                obs.Label,
                obs.Notes))
            .ToArray();
    }

    private static bool HasData(FieldHealthReportSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Layers.Count == 0)
        {
            return false;
        }

        foreach (var layer in snapshot.Layers)
        {
            if (layer is not null && layer.Observations.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private string CreateCacheKey(ReportGenerationContext context)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}|{1}|{2}|{3}",
            Descriptor.SectionId,
            context.Template.Id,
            context.Scope.Kind,
            context.Scope.Identifier);
    }
}
