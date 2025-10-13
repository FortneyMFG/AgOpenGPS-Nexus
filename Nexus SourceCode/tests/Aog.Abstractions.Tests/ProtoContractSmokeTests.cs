using System;
using Aog.Core.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aog.Abstractions.Tests;

public class ProtoContractSmokeTests
{
    [Fact]
    public void PoseMessageRoundTripsThroughSerialization()
    {
        var timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(new DateTime(2024, 1, 1, 0, 0, 0), DateTimeKind.Utc));
        var original = new Pose
        {
            Header = new Header
            {
                Sequence = 42,
                Timestamp = timestamp,
                Frame = "earth",
                Source = "unit-test"
            },
            LatitudeDeg = 51.2345,
            LongitudeDeg = -1.2345,
            AltitudeM = 123.4,
            HeadingRad = 1.5708,
            RollRad = 0.05,
            PitchRad = -0.02,
            SpeedMps = 4.2,
            YawRateRadps = 0.1
        };

        var serialized = original.ToByteArray();
        var deserialized = Pose.Parser.ParseFrom(serialized);

        Assert.Equal(original.Header.Sequence, deserialized.Header.Sequence);
        Assert.Equal(original.Header.Timestamp, deserialized.Header.Timestamp);
        Assert.Equal(original.Header.Frame, deserialized.Header.Frame);
        Assert.Equal(original.Header.Source, deserialized.Header.Source);
        Assert.Equal(original.LatitudeDeg, deserialized.LatitudeDeg);
        Assert.Equal(original.LongitudeDeg, deserialized.LongitudeDeg);
        Assert.Equal(original.AltitudeM, deserialized.AltitudeM);
        Assert.Equal(original.HeadingRad, deserialized.HeadingRad);
        Assert.Equal(original.RollRad, deserialized.RollRad);
        Assert.Equal(original.PitchRad, deserialized.PitchRad);
        Assert.Equal(original.SpeedMps, deserialized.SpeedMps);
        Assert.Equal(original.YawRateRadps, deserialized.YawRateRadps);
    }

    [Fact]
    public void PlanterRowStatusRoundTripsThroughSerialization()
    {
        var timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(new DateTime(2024, 2, 15, 6, 30, 0), DateTimeKind.Utc));
        var original = new PlanterRowStatus
        {
            Header = new Header
            {
                Sequence = 7,
                Timestamp = timestamp,
                Frame = "vehicle",
                Source = "sim"
            },
            RowIndex = 5,
            TargetPopulationPerMeter = 9.5,
            ActualPopulationPerMeter = 8.75,
            SkipRate = 0.2,
            DoubleRate = 0.0,
            Quality = PlanterRowQuality.Skip
        };

        var serialized = original.ToByteArray();
        var deserialized = PlanterRowStatus.Parser.ParseFrom(serialized);

        Assert.Equal(original.Header.Sequence, deserialized.Header.Sequence);
        Assert.Equal(original.Header.Timestamp, deserialized.Header.Timestamp);
        Assert.Equal(original.Header.Frame, deserialized.Header.Frame);
        Assert.Equal(original.Header.Source, deserialized.Header.Source);
        Assert.Equal(original.RowIndex, deserialized.RowIndex);
        Assert.Equal(original.TargetPopulationPerMeter, deserialized.TargetPopulationPerMeter);
        Assert.Equal(original.ActualPopulationPerMeter, deserialized.ActualPopulationPerMeter);
        Assert.Equal(original.SkipRate, deserialized.SkipRate);
        Assert.Equal(original.DoubleRate, deserialized.DoubleRate);
        Assert.Equal(original.Quality, deserialized.Quality);
    }
}
