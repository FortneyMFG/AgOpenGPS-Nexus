using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Reporting;

/// <summary>
/// Default implementation of the report builder backend described in ADR-051.
/// The service manages template registration, section orchestration, and export aggregation.
/// </summary>
public sealed class ReportBuilderService : IReportBuilderService
{
    private readonly Dictionary<string, ReportTemplate> _templates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReportSectionContributor> _contributors = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _mutex = new();

    /// <inheritdoc />
    public void RegisterTemplate(ReportTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        lock (_mutex)
        {
            if (_templates.ContainsKey(template.Id))
            {
                throw new InvalidOperationException($"A report template with id '{template.Id}' is already registered.");
            }

            _templates.Add(template.Id, template);
        }
    }

    /// <inheritdoc />
    public void RegisterSectionContributor(IReportSectionContributor contributor)
    {
        ArgumentNullException.ThrowIfNull(contributor);
        var descriptor = contributor.Descriptor ?? throw new ArgumentException("Section contributors must expose a descriptor.", nameof(contributor));

        lock (_mutex)
        {
            if (_contributors.ContainsKey(descriptor.SectionId))
            {
                throw new InvalidOperationException($"A report section contributor with id '{descriptor.SectionId}' is already registered.");
            }

            _contributors.Add(descriptor.SectionId, contributor);
        }
    }

    /// <inheritdoc />
    public bool TryGetTemplate(string templateId, out ReportTemplate? template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        lock (_mutex)
        {
            return _templates.TryGetValue(templateId, out template);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ReportTemplate> ListTemplates()
    {
        lock (_mutex)
        {
            return _templates.Values
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    /// <inheritdoc />
    public async Task<ReportGenerationResult> GenerateAsync(
        string templateId,
        ReportScope scope,
        ReportGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        ArgumentNullException.ThrowIfNull(scope);

        ReportTemplate template;
        Dictionary<string, IReportSectionContributor> contributorSnapshot;
        lock (_mutex)
        {
            if (!_templates.TryGetValue(templateId, out var resolvedTemplate))
            {
                throw new KeyNotFoundException($"Report template '{templateId}' is not registered.");
            }

            template = resolvedTemplate;
            contributorSnapshot = new Dictionary<string, IReportSectionContributor>(_contributors, StringComparer.OrdinalIgnoreCase);
        }

        var normalisedOptions = options ?? new ReportGenerationOptions();
        var generationContext = new ReportGenerationContext(template, scope, normalisedOptions);

        var activeContributors = new Dictionary<string, IReportSectionContributor>(StringComparer.OrdinalIgnoreCase);
        var missingRequired = new List<string>();
        foreach (var section in template.Sections)
        {
            if (contributorSnapshot.TryGetValue(section.SectionId, out var contributor))
            {
                activeContributors[section.SectionId] = contributor;
            }
            else if (!section.IsOptional)
            {
                missingRequired.Add(section.SectionId);
            }
        }

        if (missingRequired.Count > 0)
        {
            throw new InvalidOperationException($"Template '{template.Id}' requires section(s) '{string.Join(", ", missingRequired)}' but no contributor is registered.");
        }

        var readiness = new Dictionary<string, ReportSectionPreparationResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var (sectionId, contributor) in activeContributors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var prepareResult = await contributor.PrepareAsync(generationContext, cancellationToken).ConfigureAwait(false)
                    ?? new ReportSectionPreparationResult(false, "Section contributor returned null from PrepareAsync.");
                readiness[sectionId] = prepareResult;
            }
            catch (Exception ex)
            {
                readiness[sectionId] = new ReportSectionPreparationResult(
                    false,
                    $"Preparation failed: {ex.Message}",
                    new Dictionary<string, string>
                    {
                        ["exception"] = ex.GetType().FullName ?? ex.GetType().Name,
                    });
            }
        }

        var requestedOutputs = normalisedOptions.RequestedOutputs.Count > 0
            ? new HashSet<string>(normalisedOptions.RequestedOutputs, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(template.Outputs.Select(o => o.Format), StringComparer.OrdinalIgnoreCase);

        var sectionResults = new List<ReportSectionResult>(template.Sections.Count);
        foreach (var section in template.Sections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!activeContributors.TryGetValue(section.SectionId, out var contributor))
            {
                var message = $"Optional section '{section.SectionId}' is unavailable.";
                sectionResults.Add(new ReportSectionResult(section.SectionId, ReportSectionStatus.Skipped, message));
                continue;
            }

            readiness.TryGetValue(section.SectionId, out var readinessResult);
            readinessResult ??= new ReportSectionPreparationResult(false, "Section preparation did not return a result.");

            if (!readinessResult.IsReady)
            {
                var descriptor = contributor.Descriptor;
                var message = readinessResult.Reason ?? descriptor.MissingDataMessage ?? "Section prerequisites are not satisfied.";
                var status = section.IsOptional ? ReportSectionStatus.Skipped : ReportSectionStatus.MissingData;
                sectionResults.Add(new ReportSectionResult(section.SectionId, status, message, diagnostics: readinessResult.Diagnostics));
                continue;
            }

            try
            {
                var context = new ReportSectionContext(generationContext, section);
                var renderResult = await contributor.RenderAsync(context, cancellationToken).ConfigureAwait(false)
                    ?? new ReportSectionResult(section.SectionId, ReportSectionStatus.Error, "Section contributor returned null from RenderAsync.");

                if (!string.Equals(renderResult.SectionId, section.SectionId, StringComparison.OrdinalIgnoreCase))
                {
                    renderResult = new ReportSectionResult(
                        section.SectionId,
                        renderResult.Status,
                        renderResult.Message,
                        renderResult.Payload,
                        renderResult.Artifacts,
                        renderResult.Diagnostics);
                }

                sectionResults.Add(renderResult);
            }
            catch (Exception ex)
            {
                sectionResults.Add(new ReportSectionResult(
                    section.SectionId,
                    ReportSectionStatus.Error,
                    $"Rendering failed: {ex.Message}",
                    diagnostics: new Dictionary<string, string>
                    {
                        ["exception"] = ex.GetType().FullName ?? ex.GetType().Name,
                    }));
            }
        }

        var artifacts = new List<ReportArtifact>();
        foreach (var result in sectionResults)
        {
            foreach (var artifact in result.Artifacts)
            {
                if (requestedOutputs.Contains(artifact.Format))
                {
                    artifacts.Add(artifact);
                }
            }
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["templateId"] = template.Id,
            ["templateVersion"] = template.Version,
            ["scopeKind"] = scope.Kind.ToString(),
            ["scopeId"] = scope.Identifier,
            ["requestedOutput.count"] = requestedOutputs.Count
                .ToString(CultureInfo.InvariantCulture),
        };

        foreach (var format in requestedOutputs.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            metadata[$"requestedOutput.{format}"] = "true";
        }

        if (sectionResults.Count > 0)
        {
            metadata["sections.count"] = sectionResults.Count
                .ToString(CultureInfo.InvariantCulture);
            metadata["sections.success"] = sectionResults.Count(result => result.Status == ReportSectionStatus.Success)
                .ToString(CultureInfo.InvariantCulture);
            metadata["sections.missing"] = sectionResults.Count(result => result.Status == ReportSectionStatus.MissingData)
                .ToString(CultureInfo.InvariantCulture);
            metadata["sections.error"] = sectionResults.Count(result => result.Status == ReportSectionStatus.Error)
                .ToString(CultureInfo.InvariantCulture);
            metadata["sections.skipped"] = sectionResults.Count(result => result.Status == ReportSectionStatus.Skipped)
                .ToString(CultureInfo.InvariantCulture);
        }

        foreach (var section in sectionResults)
        {
            var keyPrefix = $"section.{section.SectionId}";
            metadata[$"{keyPrefix}.status"] = section.Status.ToString();
            if (!string.IsNullOrWhiteSpace(section.Message))
            {
                metadata[$"{keyPrefix}.message"] = section.Message!;
            }

            if (section.Diagnostics.Count > 0)
            {
                metadata[$"{keyPrefix}.diagnostics"] = string.Join(",",
                    section.Diagnostics
                        .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(pair => $"{pair.Key}={pair.Value}"));
            }
        }

        if (artifacts.Count > 0)
        {
            metadata["outputs.produced.count"] = artifacts.Count
                .ToString(CultureInfo.InvariantCulture);
        }

        foreach (var artifactGroup in artifacts
            .GroupBy(artifact => artifact.Format, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var names = artifactGroup
                .Select(artifact => artifact.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();

            metadata[$"outputs.produced.{artifactGroup.Key}.count"] = artifactGroup.Count()
                .ToString(CultureInfo.InvariantCulture);

            if (names.Length > 0)
            {
                metadata[$"outputs.produced.{artifactGroup.Key}.names"] = string.Join(",", names);
            }
        }

        return new ReportGenerationResult(
            template,
            scope,
            requestedOutputs.ToArray(),
            sectionResults,
            artifacts,
            metadata,
            DateTimeOffset.UtcNow);
    }
}
