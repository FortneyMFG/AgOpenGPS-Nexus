using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Aog.Core.Reporting.Tests;

public sealed class ReportExportAuditorTests
{
    [Fact]
    public void GenerateSummary_ReturnsDeterministicSnapshot()
    {
        var template = new ReportTemplate(
            "template:telemetry",
            "Template template:telemetry",
            "1.0.0",
            ReportScopeKind.Job,
            new[]
            {
                new ReportTemplateSection("section:alpha"),
                new ReportTemplateSection("section:beta", isOptional: true),
            },
            new[]
            {
                new ReportTemplateOutput("pdf", "PDF"),
                new ReportTemplateOutput("geojson", "GeoJSON"),
            });

        var sections = new[]
        {
            new ReportSectionResult(
                "section:alpha",
                ReportSectionStatus.Success,
                message: "Completed",
                diagnostics: new Dictionary<string, string> { ["source"] = "telemetry" }),
            new ReportSectionResult(
                "section:beta",
                ReportSectionStatus.MissingData,
                message: "Weather feed unavailable."),
        };

        var artifacts = new List<ReportArtifact>
        {
            new ReportArtifact("geojson", "alpha.geojson", Encoding.UTF8.GetBytes("{}")),
            new ReportArtifact("pdf", "alpha.pdf", Encoding.UTF8.GetBytes("pdf")),
        };

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["requestedOutput.count"] = "2",
            ["requestedOutput.pdf"] = "true",
            ["requestedOutput.geojson"] = "true",
            ["outputs.produced.geojson.count"] = "1",
            ["outputs.produced.pdf.count"] = "1",
            ["outputs.produced.pdf.names"] = "alpha.pdf",
        };

        var result = new ReportGenerationResult(
            template,
            new ReportScope(ReportScopeKind.Job, "job-99"),
            new[] { "pdf", "geojson" },
            sections,
            artifacts,
            metadata,
            new DateTimeOffset(2025, 4, 2, 15, 45, 0, TimeSpan.Zero));

        var summary = ReportExportAuditor.GenerateSummary(result);

        var expected = "Template: Template template:telemetry (template:telemetry) v1.0.0\n" +
                       "Scope: Job — job-99\n" +
                       "GeneratedAtUtc: 2025-04-02T15:45:00.0000000Z\n" +
                       "Outputs:\n" +
                       "  - geojson (requested, 1 artifact(s))\n" +
                       "    • alpha.geojson\n" +
                       "  - pdf (requested, 1 artifact(s))\n" +
                       "    • alpha.pdf\n" +
                       "Sections:\n" +
                       "  - section:alpha: Success\n" +
                       "    message: Completed\n" +
                       "    diag[source]=telemetry\n" +
                       "  - section:beta: MissingData\n" +
                       "    message: Weather feed unavailable.\n" +
                       "Metadata:\n" +
                       "  outputs.produced.geojson.count = 1\n" +
                       "  outputs.produced.pdf.count = 1\n" +
                       "  outputs.produced.pdf.names = alpha.pdf\n" +
                       "  requestedOutput.count = 2\n" +
                       "  requestedOutput.geojson = true\n" +
                       "  requestedOutput.pdf = true";

        Assert.Equal(expected, summary);
    }
}
