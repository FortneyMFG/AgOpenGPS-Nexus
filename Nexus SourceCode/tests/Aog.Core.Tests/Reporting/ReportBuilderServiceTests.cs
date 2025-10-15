using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aog.Core.Reporting.Tests;

public sealed class ReportBuilderServiceTests
{
    [Fact]
    public void RegisterTemplate_WhenDuplicateId_Throws()
    {
        var service = new ReportBuilderService();
        var template = CreateTemplate("template:alpha", sections: new[] { new ReportTemplateSection("section:alpha") });

        service.RegisterTemplate(template);

        var ex = Assert.Throws<InvalidOperationException>(() => service.RegisterTemplate(template));
        Assert.Contains("already registered", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_WhenRequiredSectionMissing_Throws()
    {
        var service = new ReportBuilderService();
        var template = CreateTemplate("template:missing", sections: new[] { new ReportTemplateSection("section:beta") });
        service.RegisterTemplate(template);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync(
            template.Id,
            new ReportScope(ReportScopeKind.Job, "job-1")));
    }

    [Fact]
    public async Task GenerateAsync_WhenContributorNotReady_ReturnsMissingData()
    {
        var service = new ReportBuilderService();
        var template = CreateTemplate("template:prep", sections: new[] { new ReportTemplateSection("section:delta") });
        service.RegisterTemplate(template);

        var contributor = new FakeSectionContributor("section:delta")
        {
            PreparationResult = new ReportSectionPreparationResult(false, "Yield data unavailable."),
        };
        service.RegisterSectionContributor(contributor);

        var result = await service.GenerateAsync(template.Id, new ReportScope(ReportScopeKind.Job, "job-42"));

        Assert.Single(result.Sections);
        var section = result.Sections[0];
        Assert.Equal(ReportSectionStatus.MissingData, section.Status);
        Assert.Contains("Yield data", section.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Artifacts);
        Assert.True(contributor.PrepareCalled);
        Assert.False(contributor.RenderCalled);
    }

    [Fact]
    public async Task GenerateAsync_WhenRenderingSucceeds_AggregatesArtifacts()
    {
        var service = new ReportBuilderService();
        var template = CreateTemplate(
            "template:success",
            sections: new[] { new ReportTemplateSection("section:epsilon") },
            outputs: new[]
            {
                new ReportTemplateOutput("pdf", "Portable Document"),
                new ReportTemplateOutput("csv", "Comma Separated"),
            });
        service.RegisterTemplate(template);

        var contributor = new FakeSectionContributor("section:epsilon")
        {
            PreparationResult = new ReportSectionPreparationResult(true),
            RenderResult = new ReportSectionResult(
                "section:epsilon",
                ReportSectionStatus.Success,
                payload: new Dictionary<string, object?> { ["value"] = 123 },
                artifacts: new List<ReportArtifact>
                {
                    new ReportArtifact("pdf", "crop-report.pdf", Encoding.UTF8.GetBytes("pdf-bytes")),
                    new ReportArtifact("csv", "crop-report.csv", Encoding.UTF8.GetBytes("csv-bytes")),
                }),
        };
        service.RegisterSectionContributor(contributor);

        var options = new ReportGenerationOptions(requestedOutputs: new[] { "pdf" });
        var result = await service.GenerateAsync(template.Id, new ReportScope(ReportScopeKind.Season, "season-2024"), options);

        Assert.Single(result.Artifacts);
        Assert.Equal("pdf", result.Artifacts[0].Format);
        Assert.Single(result.Sections);
        Assert.Equal(ReportSectionStatus.Success, result.Sections[0].Status);
        Assert.True(contributor.PrepareCalled);
        Assert.True(contributor.RenderCalled);
        Assert.Contains("template:success", result.Metadata["templateId"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_WhenRenderingThrows_ReturnsErrorSection()
    {
        var service = new ReportBuilderService();
        var template = CreateTemplate("template:error", sections: new[] { new ReportTemplateSection("section:zeta") });
        service.RegisterTemplate(template);

        var contributor = new FakeSectionContributor("section:zeta")
        {
            PreparationResult = new ReportSectionPreparationResult(true),
            ThrowOnRender = true,
        };
        service.RegisterSectionContributor(contributor);

        var result = await service.GenerateAsync(template.Id, new ReportScope(ReportScopeKind.Session, "session-1"));

        Assert.Single(result.Sections);
        var section = result.Sections[0];
        Assert.Equal(ReportSectionStatus.Error, section.Status);
        Assert.Contains("Rendering failed", section.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Artifacts);
        Assert.True(contributor.PrepareCalled);
        Assert.True(contributor.RenderCalled);
    }

    private static ReportTemplate CreateTemplate(
        string id,
        IEnumerable<ReportTemplateSection>? sections = null,
        IEnumerable<ReportTemplateOutput>? outputs = null)
    {
        var sectionList = sections ?? new[] { new ReportTemplateSection("section:alpha") };
        var outputList = outputs ?? new[] { new ReportTemplateOutput("pdf", "Portable Document") };
        return new ReportTemplate(id, $"Template {id}", "1.0.0", ReportScopeKind.Job, sectionList, outputList);
    }

    private sealed class FakeSectionContributor : IReportSectionContributor
    {
        public FakeSectionContributor(string sectionId)
        {
            Descriptor = new ReportSectionDescriptor(sectionId, sectionId);
        }

        public ReportSectionDescriptor Descriptor { get; }

        public ReportSectionPreparationResult PreparationResult { get; set; } = new(true);

        public ReportSectionResult RenderResult { get; set; } = new(
            "section:default",
            ReportSectionStatus.Success,
            artifacts: new List<ReportArtifact>());

        public bool ThrowOnPrepare { get; set; }

        public bool ThrowOnRender { get; set; }

        public bool PrepareCalled { get; private set; }

        public bool RenderCalled { get; private set; }

        public ValueTask<ReportSectionPreparationResult> PrepareAsync(ReportGenerationContext context, CancellationToken cancellationToken)
        {
            PrepareCalled = true;
            if (ThrowOnPrepare)
            {
                throw new InvalidOperationException("prepare failed");
            }

            return ValueTask.FromResult(PreparationResult);
        }

        public ValueTask<ReportSectionResult> RenderAsync(ReportSectionContext context, CancellationToken cancellationToken)
        {
            RenderCalled = true;
            if (ThrowOnRender)
            {
                throw new InvalidOperationException("render failed");
            }

            return ValueTask.FromResult(RenderResult);
        }
    }
}
