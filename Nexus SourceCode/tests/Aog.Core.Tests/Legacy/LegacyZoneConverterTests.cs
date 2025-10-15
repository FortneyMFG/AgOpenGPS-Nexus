using System;
using System.IO;
using System.Linq;
using Aog.Core.Legacy;
using Aog.Core.Zones;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Legacy;

public sealed class LegacyZoneConverterTests
{
    private const string SampleFieldPath = "Legacy/Data/SampleField";

    [Fact]
    public void Convert_GeneratesBoundaryAndHeadlandZones()
    {
        using var temp = CreateSampleField();
        var importer = new LegacyFieldImporter();
        var field = importer.Import(temp.Path);
        var converter = new LegacyZoneConverter();
        var options = new LegacyZoneImportOptions
        {
            FieldId = "field-west",
            BoundaryWorkBufferMeters = 2.0,
            HeadlandWorkBufferMeters = 1.5,
        };

        var zones = converter.Convert(field, options);

        zones.Should().HaveCount(3);
        zones.Select(z => z.ZoneId).Should().Contain(new[]
        {
            "field-west:boundary:01",
            "field-west:boundary:02",
            "field-west:headland:01:01",
        });

        var boundary = zones.Single(z => z.ZoneId == "field-west:boundary:01");
        boundary.Type.Should().Be(ZoneType.Boundary);
        boundary.Label.Should().Be("North Farm West Boundary 1");
        boundary.Buffers.WorkMeters.Should().BeApproximately(2.0, 1e-6);
        boundary.Geometry.Exterior.Vertices.Should().HaveCount(5);

        var headland = zones.Single(z => z.ZoneId == "field-west:headland:01:01");
        headland.Type.Should().Be(ZoneType.Headland);
        headland.Label.Should().Be("North Farm West Headland 1-1");
        headland.Buffers.WorkMeters.Should().BeApproximately(1.5, 1e-6);
        headland.Geometry.Exterior.Vertices.Should().HaveCount(5);
    }

    [Fact]
    public void Convert_CarriesProvenanceFromLegacyOverview()
    {
        using var temp = CreateSampleField();
        var importer = new LegacyFieldImporter();
        var field = importer.Import(temp.Path);
        var converter = new LegacyZoneConverter();
        var options = new LegacyZoneImportOptions
        {
            FieldId = "field-west",
            Source = "legacy.migration",
            Note = "Bulk import",
        };

        var zones = converter.Convert(field, options);

        zones.Should().NotBeEmpty();
        foreach (var zone in zones)
        {
            zone.Provenance.Should().NotBeNull();
            zone.Provenance!.Source.Should().Be("legacy.migration");
            zone.Provenance.Timestamp.Should().Be(field.Overview!.CreatedAtUtc);
            zone.Provenance.Note.Should().Be("Bulk import");
        }
    }

    [Fact]
    public void ValidateOptions_RejectsNegativeBuffers()
    {
        var options = new LegacyZoneImportOptions
        {
            FieldId = "field",
            BoundaryDriveBufferMeters = -1,
        };

        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static TempDirectory CreateSampleField()
    {
        var temp = new TempDirectory();
        var source = Path.Combine(AppContext.BaseDirectory, SampleFieldPath);
        Directory.CreateDirectory(temp.Path);

        foreach (var file in Directory.EnumerateFiles(source, "*.txt"))
        {
            var fileName = Path.GetFileName(file);
            if (fileName is null)
            {
                continue;
            }

            File.Copy(file, Path.Combine(temp.Path, fileName), overwrite: true);
        }

        var pngPath = Path.Combine(temp.Path, "BackPic.png");
        var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PtX9YQAAAABJRU5ErkJggg==");
        File.WriteAllBytes(pngPath, pngBytes);

        return temp;
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
