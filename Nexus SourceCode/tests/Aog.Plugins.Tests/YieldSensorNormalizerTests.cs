using System;
using System.Linq;
using Aog.Plugins.CombineYield;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class YieldSensorNormalizerTests
{
    [Fact]
    public void TryNormalize_ComputesCalibratedYieldAndMoisture()
    {
        var options = new YieldSensorNormalizationOptions
        {
            MassFlowGain = 1.2,
            MassFlowOffset = -0.5,
            MoistureGain = 1.05,
            MoistureOffset = -1.0,
            FlowSmoothingFactor = 0,
            MoistureSmoothingFactor = 0,
            MinimumGroundSpeedMps = 0.1,
            MinimumSwathWidthMeters = 0.5,
            MinimumFlowKgPerSecond = 0.1
        };

        var normalizer = new YieldSensorNormalizer(options);
        var timestamp = new DateTimeOffset(2024, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var sample = new YieldSensorSample(timestamp, 12, 5, 8, 1.5, 9, 18);

        var result = normalizer.TryNormalize(sample, out var measurement);

        result.Should().BeTrue();
        measurement.EastingMeters.Should().Be(12);
        measurement.NorthingMeters.Should().Be(5);
        measurement.YieldKgPerHectare.Should().BeApproximately(6740.7407, 1e-4);
        measurement.MoisturePercent.Should().BeApproximately(17.9, 1e-4);
    }

    [Fact]
    public void TryNormalize_AppliesExponentialSmoothing()
    {
        var options = new YieldSensorNormalizationOptions
        {
            FlowSmoothingFactor = 0.5,
            MoistureSmoothingFactor = 0.5,
            MinimumFlowKgPerSecond = 0.1,
            MinimumGroundSpeedMps = 0.1,
            MinimumSwathWidthMeters = 0.5
        };

        var normalizer = new YieldSensorNormalizer(options);
        var baseTime = new DateTimeOffset(2024, 6, 1, 7, 0, 0, TimeSpan.Zero);

        var firstSample = new YieldSensorSample(baseTime, 0, 0, 10, 2, 8, 18);
        var secondSample = new YieldSensorSample(baseTime.AddSeconds(1), 5, 5, 12, 2, 8, 20);

        normalizer.TryNormalize(firstSample, out var firstMeasurement).Should().BeTrue();
        normalizer.TryNormalize(secondSample, out var secondMeasurement).Should().BeTrue();

        firstMeasurement.YieldKgPerHectare.Should().BeApproximately(6250, 1e-6);
        secondMeasurement.YieldKgPerHectare.Should().BeApproximately(6875, 1e-6);
        secondMeasurement.MoisturePercent.Should().BeApproximately(19, 1e-6);
    }

    [Fact]
    public void TryNormalize_RespectsLagBuffer()
    {
        var options = new YieldSensorNormalizationOptions
        {
            Lag = TimeSpan.FromSeconds(2),
            FlowSmoothingFactor = 0,
            MoistureSmoothingFactor = 0,
            MinimumGroundSpeedMps = 0.1,
            MinimumSwathWidthMeters = 0.5,
            MinimumFlowKgPerSecond = 0.1
        };

        var normalizer = new YieldSensorNormalizer(options);
        var baseTime = new DateTimeOffset(2024, 5, 1, 10, 0, 0, TimeSpan.Zero);

        var sample1 = new YieldSensorSample(baseTime, 0, 0, 8, 1.5, 9, 18);
        var sample2 = new YieldSensorSample(baseTime.AddSeconds(1), 5, 0, 9, 1.5, 9, 17);
        var sample3 = new YieldSensorSample(baseTime.AddSeconds(2), 10, 0, 10, 1.5, 9, 16);

        normalizer.TryNormalize(sample1, out _).Should().BeFalse();
        normalizer.TryNormalize(sample2, out _).Should().BeFalse();
        normalizer.TryNormalize(sample3, out var released).Should().BeTrue();

        released.EastingMeters.Should().Be(sample1.EastingMeters);
        released.NorthingMeters.Should().Be(sample1.NorthingMeters);

        var remaining = normalizer.FlushLagged().ToList();
        remaining.Should().HaveCount(2);
        remaining[0].EastingMeters.Should().Be(sample2.EastingMeters);
        remaining[1].EastingMeters.Should().Be(sample3.EastingMeters);
    }

    [Fact]
    public void TryNormalize_InvalidSampleStillFlushesLaggedMeasurement()
    {
        var options = new YieldSensorNormalizationOptions
        {
            Lag = TimeSpan.FromSeconds(1),
            FlowSmoothingFactor = 0,
            MoistureSmoothingFactor = 0,
            MinimumGroundSpeedMps = 0.2,
            MinimumSwathWidthMeters = 0.5,
            MinimumFlowKgPerSecond = 0.1
        };

        var normalizer = new YieldSensorNormalizer(options);
        var baseTime = new DateTimeOffset(2024, 7, 1, 8, 0, 0, TimeSpan.Zero);

        var validSample = new YieldSensorSample(baseTime, 0, 0, 8, 1, 8, 18);
        var slowSample = new YieldSensorSample(baseTime.AddSeconds(1), 1, 0, 8, 0.05, 8, 18);

        normalizer.TryNormalize(validSample, out _).Should().BeFalse();
        normalizer.TryNormalize(slowSample, out var released).Should().BeTrue();

        released.EastingMeters.Should().Be(validSample.EastingMeters);
    }

    [Fact]
    public void TryNormalize_ClampsYieldAndMoisture()
    {
        var options = new YieldSensorNormalizationOptions
        {
            MinimumYieldKgPerHectare = 1000,
            MaximumYieldKgPerHectare = 8000,
            ClampMoistureToValidRange = true,
            FlowSmoothingFactor = 0,
            MoistureSmoothingFactor = 0,
            MinimumGroundSpeedMps = 0.1,
            MinimumSwathWidthMeters = 0.5,
            MinimumFlowKgPerSecond = 0.1
        };

        var normalizer = new YieldSensorNormalizer(options);
        var timestamp = new DateTimeOffset(2024, 8, 1, 9, 0, 0, TimeSpan.Zero);

        var lowFlowSample = new YieldSensorSample(timestamp, 0, 0, 0.12, 1, 8, -5);
        var highFlowSample = new YieldSensorSample(timestamp.AddSeconds(1), 0, 0, 20, 1, 8, 150);

        normalizer.TryNormalize(lowFlowSample, out var lowMeasurement).Should().BeTrue();
        lowMeasurement.YieldKgPerHectare.Should().Be(1000);
        lowMeasurement.MoisturePercent.Should().Be(0);

        normalizer.TryNormalize(highFlowSample, out var highMeasurement).Should().BeTrue();
        highMeasurement.YieldKgPerHectare.Should().Be(8000);
        highMeasurement.MoisturePercent.Should().Be(100);
    }
}
