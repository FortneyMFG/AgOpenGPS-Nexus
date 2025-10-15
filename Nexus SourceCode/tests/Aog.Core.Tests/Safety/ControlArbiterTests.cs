using System;
using Aog.Core.Safety;
using Aog.Core.V1;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Safety;

public sealed class ControlArbiterTests
{
    [Fact]
    public void InitialSnapshot_AllowsOutputs()
    {
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2025, 03, 15, 10, 00, 00, TimeSpan.Zero));
        var arbiter = new ControlArbiter(timeProvider);

        var snapshot = arbiter.Snapshot;

        snapshot.AutosteerAllowed.Should().BeTrue();
        snapshot.SectionsAllowed.Should().BeTrue();
        snapshot.ActiveReason.Should().BeNull();
        snapshot.TimestampUtc.Should().Be(timeProvider.GetUtcNow());
    }

    [Fact]
    public void KeepOutBlocksAutosteerAndSections()
    {
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2025, 03, 15, 12, 00, 00, TimeSpan.Zero));
        var arbiter = new ControlArbiter(timeProvider);
        var mask = new PoseZoneMask
        {
            InsideKeepOut = true,
            ZoneRegistryHash = "abc123",
        };
        mask.ActiveZoneIds.Add("zone-a");

        timeProvider.Advance(TimeSpan.FromSeconds(10));
        var transition = arbiter.UpdateConstraintState(mask);

        transition.StateChanged.Should().BeTrue();
        transition.GateEngaged.Should().BeTrue();
        transition.GateReleased.Should().BeFalse();
        transition.ShouldLogEvent.Should().BeTrue();
        transition.LoggedReason.Should().Be(ConstraintGateReason.KeepOut);

        var snapshot = transition.Current;
        snapshot.AutosteerAllowed.Should().BeFalse();
        snapshot.SectionsAllowed.Should().BeFalse();
        snapshot.ActiveReason.Should().Be(ConstraintGateReason.KeepOut);
        snapshot.ActiveZoneIds.Should().ContainSingle().Which.Should().Be("zone-a");
        snapshot.ZoneRegistryHash.Should().Be("abc123");
        snapshot.TimestampUtc.Should().Be(timeProvider.GetUtcNow());
    }

    [Fact]
    public void WorkDisabledAllowsAutosteerButDisablesSections()
    {
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2025, 03, 15, 13, 00, 00, TimeSpan.Zero));
        var arbiter = new ControlArbiter(timeProvider);

        var mask = new PoseZoneMask
        {
            InsideWorkDisabled = true,
        };
        mask.ActiveZoneIds.Add("zone-b");

        var transition = arbiter.UpdateConstraintState(mask, timeProvider.GetUtcNow() + TimeSpan.FromSeconds(30));

        transition.StateChanged.Should().BeTrue();
        transition.GateEngaged.Should().BeTrue();
        transition.ShouldLogEvent.Should().BeTrue();
        transition.LoggedReason.Should().Be(ConstraintGateReason.WorkDisabled);

        var snapshot = transition.Current;
        snapshot.AutosteerAllowed.Should().BeTrue();
        snapshot.SectionsAllowed.Should().BeFalse();
        snapshot.ActiveReason.Should().Be(ConstraintGateReason.WorkDisabled);
        snapshot.ActiveZoneIds.Should().ContainSingle().Which.Should().Be("zone-b");
    }

    [Fact]
    public void ClearingMaskReleasesGate()
    {
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2025, 03, 15, 14, 00, 00, TimeSpan.Zero));
        var arbiter = new ControlArbiter(timeProvider);

        var mask = new PoseZoneMask { InsideKeepOut = true };
        arbiter.UpdateConstraintState(mask);

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        var transition = arbiter.UpdateConstraintState(null);

        transition.StateChanged.Should().BeTrue();
        transition.GateEngaged.Should().BeFalse();
        transition.GateReleased.Should().BeTrue();
        transition.ShouldLogEvent.Should().BeFalse();

        var snapshot = transition.Current;
        snapshot.AutosteerAllowed.Should().BeTrue();
        snapshot.SectionsAllowed.Should().BeTrue();
        snapshot.ActiveReason.Should().BeNull();
        snapshot.ActiveZoneIds.Should().BeEmpty();
    }

    [Fact]
    public void KeepOutTakesPriorityOverWorkDisabled()
    {
        var arbiter = new ControlArbiter(new TestTimeProvider(DateTimeOffset.UtcNow));
        var mask = new PoseZoneMask
        {
            InsideKeepOut = true,
            InsideWorkDisabled = true,
        };

        var transition = arbiter.UpdateConstraintState(mask);

        transition.Current.ActiveReason.Should().Be(ConstraintGateReason.KeepOut);
        transition.Current.AutosteerAllowed.Should().BeFalse();
        transition.Current.SectionsAllowed.Should().BeFalse();
    }

    [Fact]
    public void ZoneChangeTriggersLogging()
    {
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2025, 03, 15, 15, 00, 00, TimeSpan.Zero));
        var arbiter = new ControlArbiter(timeProvider);
        var firstMask = new PoseZoneMask { InsideKeepOut = true };
        firstMask.ActiveZoneIds.Add("zone-a");
        arbiter.UpdateConstraintState(firstMask);

        var secondMask = new PoseZoneMask { InsideKeepOut = true };
        secondMask.ActiveZoneIds.Add("zone-b");

        var transition = arbiter.UpdateConstraintState(secondMask, timeProvider.GetUtcNow() + TimeSpan.FromSeconds(1));

        transition.StateChanged.Should().BeTrue();
        transition.ShouldLogEvent.Should().BeTrue();
        transition.LoggedReason.Should().Be(ConstraintGateReason.KeepOut);
        transition.Current.ActiveZoneIds.Should().ContainSingle().Which.Should().Be("zone-b");
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow += delta;
    }
}
