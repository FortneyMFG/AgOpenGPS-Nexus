using System;
using System.Threading.Tasks;
using Aog.Plugins.CombineYield;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class YieldImportServiceTests
{
    [Fact]
    public async Task ImportAsync_WithMeasurementsProducesPublication()
    {
        var options = new CombineYieldOptions
        {
            CellSizeMeters = 10,
            Source = "import:iso",
            Transform = "import:yield",
            Actor = "wizard:yield",
            Frame = "field",
            Crop = "Soybeans",
            Projection = "EPSG:32615",
            PublishInterval = TimeSpan.FromMinutes(1),
            SmoothingKernelSize = 1,
            OutlierClampFraction = 0,
            BinningBinCount = 3
        };

        var request = new YieldImportRequest(new[]
        {
            new CombineYieldMeasurement(0.5, 0.5, 8000, 18),
            new CombineYieldMeasurement(10.5, 0.5, 6000, 16)
        })
        {
            Options = options,
            Units = "bu/ac",
            Timestamp = new DateTimeOffset(2024, 9, 1, 12, 0, 0, TimeSpan.Zero)
        };

        var service = new YieldImportService();
        var result = await service.ImportAsync(request);

        result.MeasurementCount.Should().Be(2);
        result.Units.Should().Be("bu/ac");
        result.Publication.Layer.Crop.Should().Be("Soybeans");
        result.Publication.Layer.Cells.Should().HaveCount(2);
        result.Publication.Layer.Header.Timestamp.ToDateTimeOffset().Should().Be(request.Timestamp);
        result.Publication.Metadata.Grid.Projection.Should().Be("EPSG:32615");
        result.Publication.Metadata.Aggregation.Bins.Count.Should().Be(3);
        result.Publication.Provenance.Source.Should().Be("import:iso");
        result.Publication.Metadata.Statistics.SampleCount.Should().Be(2);
    }

    [Fact]
    public async Task ImportAsync_ThrowsWhenNoMeasurements()
    {
        var request = new YieldImportRequest(Array.Empty<CombineYieldMeasurement>());
        var service = new YieldImportService();

        var action = () => service.ImportAsync(request);

        await action.Should().ThrowAsync<ArgumentException>();
    }
}
