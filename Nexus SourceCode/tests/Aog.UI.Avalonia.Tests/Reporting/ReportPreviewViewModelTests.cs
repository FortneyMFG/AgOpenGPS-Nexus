using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Reporting;
using Aog.UI.Avalonia.Reporting;
using Aog.UI.Avalonia.ViewModels.Reporting;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Reporting;

public sealed class ReportPreviewViewModelTests
{
    [Fact]
    public void Load_PopulatesViewModelState()
    {
        var shareService = new FakeShareService();
        var viewModel = new ReportPreviewViewModel(shareService);
        var result = CreateReportResult();

        viewModel.Load(result);

        Assert.True(viewModel.HasReport);
        Assert.Equal("Template template:telemetry", viewModel.TemplateName);
        Assert.Equal("1.0.0", viewModel.TemplateVersion);
        Assert.Equal("Job — job-99", viewModel.ScopeDisplay);
        Assert.Equal(result.GeneratedAt.ToUniversalTime(), viewModel.GeneratedAt);
        Assert.Equal(result.Artifacts.Count, viewModel.TotalArtifacts);
        Assert.Equal("Report ready to share.", viewModel.StatusMessage);
        Assert.False(viewModel.HasError);
        Assert.NotEmpty(viewModel.AuditSummary);

        Assert.Equal(2, viewModel.Sections.Count);
        Assert.Equal("section:alpha", viewModel.Sections[0].SectionId);
        Assert.Equal(ReportSectionStatus.Success, viewModel.Sections[0].Status);
        Assert.Equal("section:beta", viewModel.Sections[1].SectionId);
        Assert.Equal(ReportSectionStatus.MissingData, viewModel.Sections[1].Status);

        Assert.Equal(2, viewModel.Outputs.Count);
        var pdf = Assert.Single(viewModel.Outputs, output => output.Format == "pdf");
        Assert.True(pdf.IsRequested);
        Assert.False(pdf.IsMissing);
        Assert.Contains("alpha.pdf", pdf.ArtifactNames);

        var geojson = Assert.Single(viewModel.Outputs, output => output.Format == "geojson");
        Assert.True(geojson.IsRequested);
        Assert.True(geojson.IsPartial);
        Assert.DoesNotContain(viewModel.Outputs, output => string.Equals(output.Format, "count", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ShareCommand_WhenExecuted_UsesShareService()
    {
        var shareService = new FakeShareService();
        var viewModel = new ReportPreviewViewModel(shareService);
        var result = CreateReportResult();
        viewModel.Load(result);

        Assert.True(viewModel.ShareCommand.CanExecute(null));

        viewModel.ShareCommand.Execute(null);

        Assert.Same(result, shareService.SharedResult);
        Assert.False(viewModel.HasError);
        Assert.Equal("Report shared successfully.", viewModel.StatusMessage);
    }

    private static ReportGenerationResult CreateReportResult()
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
            new ReportArtifact("pdf", "alpha.pdf", Encoding.UTF8.GetBytes("pdf")),
        };

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["requestedOutput.count"] = "2",
            ["requestedOutput.pdf"] = "true",
            ["requestedOutput.geojson"] = "true",
            ["outputs.produced.pdf.count"] = "1",
            ["outputs.produced.pdf.names"] = "alpha.pdf",
            ["outputs.produced.geojson.count"] = "2",
        };

        return new ReportGenerationResult(
            template,
            new ReportScope(ReportScopeKind.Job, "job-99"),
            new[] { "pdf", "geojson" },
            sections,
            artifacts,
            metadata,
            new DateTimeOffset(2025, 4, 2, 15, 45, 0, TimeSpan.Zero));
    }

    private sealed class FakeShareService : IReportShareService
    {
        public ReportGenerationResult? SharedResult { get; private set; }

        public Task ShareAsync(ReportGenerationResult result, CancellationToken cancellationToken = default)
        {
            SharedResult = result;
            return Task.CompletedTask;
        }
    }
}
