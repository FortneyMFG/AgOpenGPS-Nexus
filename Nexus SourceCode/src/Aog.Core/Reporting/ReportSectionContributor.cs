using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Reporting;

/// <summary>
/// Describes metadata about a report section contributor.
/// </summary>
public sealed record ReportSectionDescriptor
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ReportSectionDescriptor"/> class.
    /// </summary>
    /// <param name="sectionId">Stable identifier of the section.</param>
    /// <param name="displayName">Human readable name of the section.</param>
    /// <param name="capabilities">Capabilities exposed by the section (for example, data domains).</param>
    /// <param name="requiredContext">Context properties required to render the section.</param>
    /// <param name="missingDataMessage">Fallback message surfaced when dependencies are unavailable.</param>
    public ReportSectionDescriptor(
        string sectionId,
        string displayName,
        IReadOnlyCollection<string>? capabilities = null,
        IReadOnlyCollection<string>? requiredContext = null,
        string? missingDataMessage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        SectionId = sectionId;
        DisplayName = displayName;
        Capabilities = capabilities is null
            ? Array.Empty<string>()
            : new List<string>(capabilities);
        RequiredContext = requiredContext is null
            ? Array.Empty<string>()
            : new List<string>(requiredContext);
        MissingDataMessage = missingDataMessage;
    }

    /// <summary>
    /// Stable identifier of the section.
    /// </summary>
    public string SectionId { get; }

    /// <summary>
    /// Human readable name of the section.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Capabilities exposed by the section (for example, data domains).
    /// </summary>
    public IReadOnlyCollection<string> Capabilities { get; }

    /// <summary>
    /// Context properties required to render the section.
    /// </summary>
    public IReadOnlyCollection<string> RequiredContext { get; }

    /// <summary>
    /// Fallback message surfaced when dependencies are unavailable.
    /// </summary>
    public string? MissingDataMessage { get; }
}

/// <summary>
/// Result of preparing a section prior to rendering.
/// </summary>
public sealed record ReportSectionPreparationResult
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ReportSectionPreparationResult"/> class.
    /// </summary>
    /// <param name="isReady">Indicates whether the section is ready to render.</param>
    /// <param name="reason">Optional reason when the section cannot render.</param>
    /// <param name="diagnostics">Additional diagnostic key/value pairs.</param>
    public ReportSectionPreparationResult(
        bool isReady,
        string? reason = null,
        IReadOnlyDictionary<string, string>? diagnostics = null)
    {
        IsReady = isReady;
        Reason = reason;
        Diagnostics = diagnostics is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(diagnostics, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Indicates whether the section is ready to render.
    /// </summary>
    public bool IsReady { get; }

    /// <summary>
    /// Optional reason when the section cannot render.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Additional diagnostic key/value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, string> Diagnostics { get; }
}

/// <summary>
/// Indicates the status of a rendered section.
/// </summary>
public enum ReportSectionStatus
{
    /// <summary>
    /// The section rendered successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The section could not render due to missing data or prerequisites.
    /// </summary>
    MissingData,

    /// <summary>
    /// The section was intentionally skipped (for example, optional with missing provider).
    /// </summary>
    Skipped,

    /// <summary>
    /// The section encountered an error while rendering.
    /// </summary>
    Error,
}

/// <summary>
/// Represents a rendered section result.
/// </summary>
public sealed record ReportSectionResult
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ReportSectionResult"/> class.
    /// </summary>
    /// <param name="sectionId">Identifier of the section.</param>
    /// <param name="status">Outcome status.</param>
    /// <param name="message">Optional human readable message (for example, fallback text).</param>
    /// <param name="payload">Structured payload returned by the section.</param>
    /// <param name="artifacts">Artifacts contributed by the section.</param>
    /// <param name="diagnostics">Additional diagnostic metadata.</param>
    public ReportSectionResult(
        string sectionId,
        ReportSectionStatus status,
        string? message = null,
        object? payload = null,
        IReadOnlyList<ReportArtifact>? artifacts = null,
        IReadOnlyDictionary<string, string>? diagnostics = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);

        SectionId = sectionId;
        Status = status;
        Message = message;
        Payload = payload;
        Artifacts = artifacts ?? Array.Empty<ReportArtifact>();
        Diagnostics = diagnostics is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(diagnostics, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Identifier of the section.
    /// </summary>
    public string SectionId { get; }

    /// <summary>
    /// Outcome status.
    /// </summary>
    public ReportSectionStatus Status { get; }

    /// <summary>
    /// Optional human readable message (for example, fallback text).
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Structured payload returned by the section.
    /// </summary>
    public object? Payload { get; }

    /// <summary>
    /// Artifacts contributed by the section.
    /// </summary>
    public IReadOnlyList<ReportArtifact> Artifacts { get; }

    /// <summary>
    /// Additional diagnostic metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Diagnostics { get; }
}

/// <summary>
/// Represents an artifact produced by a report section.
/// </summary>
public sealed record ReportArtifact
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ReportArtifact"/> class.
    /// </summary>
    /// <param name="format">Artifact format identifier (for example, <c>pdf</c> or <c>csv</c>).</param>
    /// <param name="name">Suggested artifact name.</param>
    /// <param name="content">Binary content of the artifact.</param>
    /// <param name="metadata">Additional metadata describing the artifact.</param>
    public ReportArtifact(
        string format,
        string name,
        ReadOnlyMemory<byte> content,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Format = format;
        Name = name;
        Content = content;
        Metadata = metadata is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Artifact format identifier (for example, <c>pdf</c> or <c>csv</c>).
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Suggested artifact name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Binary content of the artifact.
    /// </summary>
    public ReadOnlyMemory<byte> Content { get; }

    /// <summary>
    /// Additional metadata describing the artifact.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}

/// <summary>
/// Common context shared across the generation process.
/// </summary>
public sealed record ReportGenerationContext(
    ReportTemplate Template,
    ReportScope Scope,
    ReportGenerationOptions Options);

/// <summary>
/// Context supplied to a section renderer.
/// </summary>
public sealed record ReportSectionContext(
    ReportGenerationContext Generation,
    ReportTemplateSection TemplateSection);

/// <summary>
/// Contract for plugins contributing report sections.
/// </summary>
public interface IReportSectionContributor
{
    /// <summary>
    /// Gets the descriptor associated with the section contributor.
    /// </summary>
    ReportSectionDescriptor Descriptor { get; }

    /// <summary>
    /// Performs pre-generation preparation, allowing expensive analytics or caching to be scheduled once.
    /// </summary>
    ValueTask<ReportSectionPreparationResult> PrepareAsync(ReportGenerationContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Renders the section into structured payloads and artifacts.
    /// </summary>
    ValueTask<ReportSectionResult> RenderAsync(ReportSectionContext context, CancellationToken cancellationToken);
}
