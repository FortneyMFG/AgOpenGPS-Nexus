using System;
using System.IO;
using Aog.Core.Legacy;
using Aog.Core.Paths;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Legacy;

public sealed class LegacyFieldImporterTests
{
    private static readonly string SampleFieldPath = Path.Combine("Legacy", "Data", "SampleField");

    [Fact]
    public void Import_LoadsTracksBoundariesAndHeadlands()
    {
        var importer = new LegacyFieldImporter();
        var fieldPath = Path.Combine(AppContext.BaseDirectory, SampleFieldPath);

        var data = importer.Import(fieldPath);

        data.Tracks.Should().HaveCount(2);
        data.Tracks[0].Name.Should().Be("Main AB");
        data.Tracks[0].Mode.Should().Be(LegacyTrackMode.AbLine);
        data.Tracks[0].PointA.Should().Be(new PlanarPoint(0, 0));
        data.Tracks[0].PointB.Should().Be(new PlanarPoint(0, 100));

        data.Tracks[1].CurvePoints.Should().HaveCount(3);
        data.Tracks[1].CurvePoints[0].Point.Should().Be(new PlanarPoint(12, 12));

        data.Boundaries.Should().HaveCount(2);
        data.Boundaries[0].IsDriveThrough.Should().BeTrue();
        data.Boundaries[0].Perimeter.Should().HaveCount(4);
        data.Boundaries[0].Headlands.Should().HaveCount(1);
        data.Boundaries[0].Headlands[0].Vertices.Should().HaveCount(4);

        data.Boundaries[1].IsDriveThrough.Should().BeFalse();
        data.Boundaries[1].Perimeter.Should().HaveCount(3);

        data.Overview.Should().NotBeNull();
        data.Overview!.FieldName.Should().Be("North Farm West");
        data.Overview.OperatorName.Should().Be("Casey Jensen");
        data.Overview.Origin.Should().Be(new GeographicCoordinate(51.123456, -114.123789));
        data.Overview.ConvergenceAngleDegrees.Should().BeApproximately(0.45, 1e-6);

        data.Flags.Should().HaveCount(3);
        data.Flags[0].Label.Should().Be("North Rock");
        data.Flags[0].Location.Should().Be(new PlanarPoint(5, 12.5));
        data.Flags[1].Color.Should().Be("Blue");

        data.Contour.IsRecording.Should().BeTrue();
        data.Contour.SavedStrips.Should().HaveCount(2);
        data.Contour.PendingStrip.Should().NotBeNull();
        data.Contour.PendingStrip!.Vertices.Should().Contain(new PlanarPoint(6, 7));

        data.RecordedPaths.Should().HaveCount(2);
        data.RecordedPaths[0].Name.Should().Be("Training Pass");
        data.RecordedPaths[0].Samples.Should().HaveCount(3);

        data.TramTemplates.Should().HaveCount(2);
        data.TramTemplates[0].Name.Should().Be("North Tram");
        data.TramTemplates[0].Passes.Should().HaveCount(2);

        data.WorkedArea.LayerId.Should().Be("layer:coverage.actual");
        data.WorkedArea.CellSizeMeters.Should().BeApproximately(5.5, 1e-9);
        data.WorkedArea.SavedCells.Should().HaveCount(2);
        data.WorkedArea.PendingCells.Should().HaveCount(2);
    }

    [Fact]
    public void Import_ReturnsEmpty_WhenFilesMissing()
    {
        using var temp = new TempDirectory();
        var importer = new LegacyFieldImporter();

        var data = importer.Import(temp.Path);

        data.Tracks.Should().BeEmpty();
        data.Boundaries.Should().BeEmpty();
        data.Flags.Should().BeEmpty();
        data.RecordedPaths.Should().BeEmpty();
        data.TramTemplates.Should().BeEmpty();
        data.Contour.IsRecording.Should().BeFalse();
        data.WorkedArea.SavedCells.Should().BeEmpty();
        data.WorkedArea.PendingCells.Should().BeEmpty();
        data.Overview.Should().BeNull();
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
