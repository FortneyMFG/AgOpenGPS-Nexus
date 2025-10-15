using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Reporting;

/// <summary>
/// Coordinates report template registration, section discovery, and generation orchestration.
/// </summary>
public interface IReportBuilderService
{
    /// <summary>
    /// Registers a report template with the service.
    /// </summary>
    void RegisterTemplate(ReportTemplate template);

    /// <summary>
    /// Registers a section contributor.
    /// </summary>
    void RegisterSectionContributor(IReportSectionContributor contributor);

    /// <summary>
    /// Attempts to retrieve the specified template.
    /// </summary>
    bool TryGetTemplate(string templateId, out ReportTemplate? template);

    /// <summary>
    /// Lists the registered templates ordered by display name then identifier.
    /// </summary>
    IReadOnlyList<ReportTemplate> ListTemplates();

    /// <summary>
    /// Generates a report using the specified template.
    /// </summary>
    Task<ReportGenerationResult> GenerateAsync(
        string templateId,
        ReportScope scope,
        ReportGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a report generation request.
/// </summary>
public sealed record ReportGenerationResult(
    ReportTemplate Template,
    ReportScope Scope,
    IReadOnlyCollection<string> Outputs,
    IReadOnlyList<ReportSectionResult> Sections,
    IReadOnlyList<ReportArtifact> Artifacts,
    IReadOnlyDictionary<string, string> Metadata,
    System.DateTimeOffset GeneratedAt);
