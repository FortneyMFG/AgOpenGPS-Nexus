using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Safety;
using Aog.Core.V1;
using Aog.Plugins.Sections;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aog.Plugins.Tests.Sections;

public sealed class SectionIoOrchestratorTests
{
    [Fact]
    public async Task ApplyAsync_PublishesMaskWhenStateChanges()
    {
        var eventBus = new InMemoryEventBus();
        var calculator = new SectionMaskCalculator(sectionCount: 2, minimumSpeedMps: 0.1, lookAheadSeconds: 0.5);
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2024, 04, 05, 12, 30, 0, TimeSpan.Zero));
        var orchestrator = new SectionIoOrchestrator(eventBus, calculator, source: "sections/test", frame: "implement", timeProvider);

        var published = new List<SectionMask>();
        using var subscription = eventBus.Subscribe<SectionMask>((mask, _) =>
        {
            published.Add(mask.Clone());
            return ValueTask.CompletedTask;
        });

        var sections = new[]
        {
            new SectionObservation(hasCoverage: true, distanceToCoverageStartMeters: null),
            new SectionObservation(hasCoverage: false, distanceToCoverageStartMeters: null),
        };

        await orchestrator.ApplyAsync(2.5, sections);

        published.Should().ContainSingle();
        published[0].Mask.Should().Be(0b01u);
        published[0].SectionCount.Should().Be(2);
        published[0].Header.Should().NotBeNull();
        published[0].Header!.Source.Should().Be("sections/test");
        published[0].Header.Frame.Should().Be("implement");
        published[0].Header.Timestamp.Should().Be(Timestamp.FromDateTime(timeProvider.UtcNowDateTime));
    }

    [Fact]
    public async Task ApplyAsync_SuppressesDuplicateMasks()
    {
        var eventBus = new InMemoryEventBus();
        var calculator = new SectionMaskCalculator(sectionCount: 2, minimumSpeedMps: 0.0, lookAheadSeconds: 0.0);
        var orchestrator = new SectionIoOrchestrator(eventBus, calculator);

        var publishCount = 0;
        using var subscription = eventBus.Subscribe<SectionMask>((_, _) =>
        {
            publishCount++;
            return ValueTask.CompletedTask;
        });

        var sections = new[]
        {
            new SectionObservation(true, null),
            new SectionObservation(false, null),
        };

        await orchestrator.ApplyAsync(1.0, sections);
        await orchestrator.ApplyAsync(1.0, sections);

        publishCount.Should().Be(1);
    }

    [Fact]
    public async Task ApplyAsync_PublishesZeroMaskWhenSpeedBelowThreshold()
    {
        var eventBus = new InMemoryEventBus();
        var calculator = new SectionMaskCalculator(sectionCount: 3, minimumSpeedMps: 2.0, lookAheadSeconds: 0.5);
        var orchestrator = new SectionIoOrchestrator(eventBus, calculator);

        var published = new List<SectionMask>();
        using var subscription = eventBus.Subscribe<SectionMask>((mask, _) =>
        {
            published.Add(mask.Clone());
            return ValueTask.CompletedTask;
        });

        var sections = new[]
        {
            new SectionObservation(true, null),
            new SectionObservation(true, null),
            new SectionObservation(false, null),
        };

        await orchestrator.ApplyAsync(0.5, sections);

        published.Should().ContainSingle();
        published[0].Mask.Should().Be(0u);
        published[0].SectionCount.Should().Be(3);
    }

    [Fact]
    public async Task Reset_AllowsPublishingSameMaskAgain()
    {
        var eventBus = new InMemoryEventBus();
        var calculator = new SectionMaskCalculator(sectionCount: 2, minimumSpeedMps: 0.0, lookAheadSeconds: 0.0);
        var orchestrator = new SectionIoOrchestrator(eventBus, calculator);

        var publishCount = 0;
        using var subscription = eventBus.Subscribe<SectionMask>((_, _) =>
        {
            publishCount++;
            return ValueTask.CompletedTask;
        });

        var sections = new[]
        {
            new SectionObservation(true, null),
            new SectionObservation(false, null),
        };

        await orchestrator.ApplyAsync(1.0, sections);
        orchestrator.Reset();
        await orchestrator.ApplyAsync(1.0, sections);

        publishCount.Should().Be(2);
    }

    [Fact]
    public async Task ApplyAsync_ConstraintGateForcesZeroMask()
    {
        var eventBus = new InMemoryEventBus();
        var calculator = new SectionMaskCalculator(sectionCount: 2, minimumSpeedMps: 0.0, lookAheadSeconds: 0.0);
        var orchestrator = new SectionIoOrchestrator(eventBus, calculator);

        var gate = ConstraintGateSnapshot.FromZoneMask(new PoseZoneMask { InsideWorkDisabled = true }, DateTimeOffset.UtcNow);
        orchestrator.UpdateConstraintGate(gate);

        var published = new List<SectionMask>();
        using var subscription = eventBus.Subscribe<SectionMask>((mask, _) =>
        {
            published.Add(mask.Clone());
            return ValueTask.CompletedTask;
        });

        var sections = new[]
        {
            new SectionObservation(true, null),
            new SectionObservation(true, null),
        };

        await orchestrator.ApplyAsync(4.0, sections);

        published.Should().ContainSingle();
        published[0].Mask.Should().Be(0u);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public DateTime UtcNowDateTime => _utcNow.UtcDateTime;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta)
        {
            _utcNow += delta;
        }
    }
}
