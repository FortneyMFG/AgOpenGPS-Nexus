using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Safety;
using Aog.Core.V1;
using Aog.Plugins.AutoSteer;
using Aog.Plugins.Sections;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class ConstraintGateFaultInjectionTests
{
    [Fact]
    public void AutosteerKeepOutGate_DisablesSteeringUntilCleared()
    {
        var controller = new AutoSteerLiteController();
        var path = new List<PathPoint>
        {
            new(0, 0),
            new(10, 0),
            new(20, 0),
        };
        var state = new VehicleState(0, 1, headingRadians: 0.05, speedMetersPerSecond: 4, wheelbaseMeters: 2.8);
        var keepOutGate = ConstraintGateSnapshot.FromZoneMask(new PoseZoneMask { InsideKeepOut = true }, DateTimeOffset.UtcNow);

        var suppressed = controller.ComputeSteeringAngle(state, path, keepOutGate);
        suppressed.Should().Be(0);

        var clearGate = ConstraintGateSnapshot.CreateClear(DateTimeOffset.UtcNow);
        var resumed = controller.ComputeSteeringAngle(state, path, clearGate);
        resumed.Should().NotBe(0);
    }

    [Fact]
    public async Task SectionsGateTransitions_PublishZeroThenResumeAsync()
    {
        var eventBus = new InMemoryEventBus();
        var calculator = new SectionMaskCalculator(sectionCount: 2, minimumSpeedMps: 0, lookAheadSeconds: 0);
        var orchestrator = new SectionIoOrchestrator(eventBus, calculator);
        var published = new List<uint>();

        using var subscription = eventBus.Subscribe<SectionMask>((mask, _) =>
        {
            published.Add(mask.Mask);
            return ValueTask.CompletedTask;
        });

        var sections = new[]
        {
            new SectionObservation(true, null),
            new SectionObservation(false, null),
        };

        var gate = ConstraintGateSnapshot.FromZoneMask(new PoseZoneMask { InsideWorkDisabled = true }, DateTimeOffset.UtcNow);
        orchestrator.UpdateConstraintGate(gate);
        await orchestrator.ApplyAsync(4, sections, CancellationToken.None);

        orchestrator.UpdateConstraintGate(ConstraintGateSnapshot.CreateClear(DateTimeOffset.UtcNow));
        await orchestrator.ApplyAsync(4, sections, CancellationToken.None);

        published.Should().Equal(0u, 0b01u);
    }

    [Fact]
    public void VariableRateGateTransitions_ZeroAndRestoreRates()
    {
        var placements = new List<SectionPlacement>
        {
            new(new PlanarPoint(0, 0)),
            new(new PlanarPoint(10, 0)),
        };

        var cells = new List<AgronomicLayerCell>
        {
            new(new PlanarPoint(0, 0), 12, 100),
            new(new PlanarPoint(10, 0), 12, 110),
        };

        var layer = new AgronomicLayerDocument(
            "layer:rate",
            "rate",
            "L/ha",
            DateTimeOffset.UtcNow,
            "user",
            cells,
            new LayerProvenance("import", "none", "hash", DateTimeOffset.UtcNow, "user"));

        var controller = new VariableRateController(2, 50, 150, 90);
        var gate = ConstraintGateSnapshot.FromZoneMask(new PoseZoneMask { InsideKeepOut = true }, DateTimeOffset.UtcNow);

        var suppressed = controller.ComputeRates(placements, layer, gate);
        suppressed.Should().OnlyContain(rate => Math.Abs(rate) < 1e-6);

        var resumed = controller.ComputeRates(placements, layer, ConstraintGateSnapshot.CreateClear(DateTimeOffset.UtcNow));
        resumed.Should().Equal(100, 110);
    }
}
