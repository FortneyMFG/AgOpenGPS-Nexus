using System;
using System.Collections.Generic;
using Aog.Core.Machines.Axle;
using Aog.Core.Machines.Calibration;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Machines.Calibration;

public sealed class CalibrationWorkflowTests
{
    [Fact]
    public void AckermannWizard_ComputesResidual()
    {
        var samples = new List<AckermannCalibrationSample>
        {
            new(17.2, 12.5),
            new(16.9, 12.4),
            new(17.1, 12.6)
        };

        var result = CalibrationWorkflows.RunAckermannWizard(samples, wheelbaseMeters: 3.0, trackWidthMeters: 2.5);

        result.Pass.Should().BeTrue();
        result.RmsResidualDegrees.Should().BeLessThan(0.2);
    }

    [Fact]
    public void HitchZeroing_ComputesOffset()
    {
        var samples = new[] { 0.05, -0.02, 0.01, -0.04 };
        var result = CalibrationWorkflows.CalculateHitchZero(samples, toleranceDegrees: 0.2);

        result.Pass.Should().BeTrue();
        Math.Abs(result.MeanOffsetDegrees).Should().BeLessThan(0.1);
        result.SpreadDegrees.Should().BeLessThan(0.2);
    }

    [Fact]
    public void SlipSanity_ChecksDerate()
    {
        var samples = new List<SlipSample>
        {
            new(0.12, 2.5),
            new(0.08, 2.2),
            new(0.10, 2.3)
        };

        var mode = new AxleModeProfile("field", curvatureLimit: 0.3, slipLimit: 0.12, DriveDirectionPolicy.Bidirectional);
        var result = CalibrationWorkflows.EvaluateSlip(samples, mode);

        result.AverageSlip.Should().BeGreaterThan(0);
        result.Derate.Should().Be(0);
    }

    [Fact]
    public void TransportLockValidation_ComputesMismatches()
    {
        var commanded = new[] { true, true, false, false };
        var observed = new[] { true, false, false, false };

        var result = CalibrationWorkflows.ValidateTransportLock(commanded, observed);

        result.MismatchSamples.Should().Be(1);
        result.MatchedSamples.Should().Be(3);
    }
}
