using System;
using System.Collections.Generic;
using Aog.Core.Reporting;

namespace Aog.UI.Avalonia.ViewModels.Reporting;

/// <summary>
/// Read-only view-model describing a section within a generated report.
/// </summary>
public sealed class ReportPreviewSectionViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReportPreviewSectionViewModel"/> class.
    /// </summary>
    /// <param name="result">Underlying section result.</param>
    public ReportPreviewSectionViewModel(ReportSectionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        SectionId = result.SectionId;
        Status = result.Status;
        Message = result.Message;
        Diagnostics = result.Diagnostics;
    }

    /// <summary>Gets the section identifier.</summary>
    public string SectionId { get; }

    /// <summary>Gets the section status.</summary>
    public ReportSectionStatus Status { get; }

    /// <summary>Gets the message provided by the section, when available.</summary>
    public string? Message { get; }

    /// <summary>Gets diagnostics supplied by the section contributor.</summary>
    public IReadOnlyDictionary<string, string> Diagnostics { get; }

    /// <summary>Gets a value indicating whether any diagnostics are available.</summary>
    public bool HasDiagnostics => Diagnostics.Count > 0;
}
