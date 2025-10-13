using System;
using System.IO;
using System.Threading.Tasks;
using Aog.Core.Legacy;
using Aog.Core.Paths;
using Aog.Core.Simulation.Configuration;
using Aog.UI.Avalonia.ViewModels;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class LegacyImportWizardViewModelTests
{
    [Fact]
    public async Task ImportAsync_WithMissingFiles_SetsErrorState()
    {
        var result = CreateSampleResult();
        var viewModel = new LegacyImportWizardViewModel(new StubImportService(result));

        viewModel.SetAbLineCsvPath("/tmp/missing.csv");
        viewModel.SetBoundaryShapePath("/tmp/missing.shp");

        await viewModel.ImportAsync();

        Assert.True(viewModel.HasError);
        Assert.False(viewModel.HasImportResult);
    }

    [Fact]
    public async Task ImportAsync_WithValidInputs_PopulatesSummary()
    {
        var sample = CreateSampleResult();
        var applied = false;
        using var temp = new TemporaryDirectory();
        var csvPath = System.IO.Path.Combine(temp.Path, "ablines.csv");
        await File.WriteAllTextAsync(csvPath, "Name,LatA,LonA,LatB,LonB\nAlpha,0,0,0,0");
        var shpPath = System.IO.Path.Combine(temp.Path, "field.shp");
        await File.WriteAllTextAsync(shpPath, string.Empty);

        var viewModel = new LegacyImportWizardViewModel(new StubImportService(sample), result =>
        {
            applied = true;
            return true;
        })
        {
            FieldName = "SampleField"
        };

        viewModel.SetAbLineCsvPath(csvPath);
        viewModel.SetBoundaryShapePath(shpPath);

        await viewModel.ImportAsync();

        Assert.True(viewModel.HasImportResult);
        Assert.False(viewModel.HasError);
        Assert.Single(viewModel.AbLines);
        Assert.Contains("boundary", viewModel.BoundarySummary, StringComparison.OrdinalIgnoreCase);

        var appliedResult = viewModel.TryApplyRoutes();
        Assert.True(appliedResult);
        Assert.True(applied);
        Assert.Contains("Routes applied", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static LegacyGuidanceImportResult CreateSampleResult()
    {
        var abLines = new[]
        {
            new LegacyAbLinePlanar(
                "Alpha",
                new GeographicCoordinate(51.0, -114.0),
                new GeographicCoordinate(51.001, -114.0),
                new PlanarPoint(0, 0),
                new PlanarPoint(10, 0),
                0,
                10),
        };

        var boundary = new[]
        {
            new PlanarPoint(0, 0),
            new PlanarPoint(10, 0),
            new PlanarPoint(10, 5),
            new PlanarPoint(0, 5),
        };

        var scenario = new SimulationScenarioConfiguration(
            "legacy:sample",
            "Imported guidance",
            new[] { new SimulationRouteConfiguration("pose", "legacy/udp/main_gps", "hardware") },
            options: null);

        return new LegacyGuidanceImportResult(
            "SampleField",
            new GeographicCoordinate(51.0, -114.0),
            abLines,
            boundary,
            scenario);
    }

    private sealed class StubImportService : ILegacyGuidanceImportService
    {
        private readonly LegacyGuidanceImportResult _result;

        public StubImportService(LegacyGuidanceImportResult result)
        {
            _result = result;
        }

        public LegacyGuidanceImportResult Import(string fieldName, Stream abLineCsv, string boundaryShapefilePath)
        {
            return _result;
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nexus-ui-import-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests.
            }
        }
    }
}
