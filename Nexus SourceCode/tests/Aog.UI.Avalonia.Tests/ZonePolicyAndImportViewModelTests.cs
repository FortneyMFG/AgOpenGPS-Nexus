using System;
using System.Collections.Generic;
using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class ZonePolicyAndImportViewModelTests
{
    [Fact]
    public void ZoneConstraintPolicy_TracksOverridesAndSummaries()
    {
        var clock = new TestClock(new DateTimeOffset(2025, 4, 12, 13, 0, 0, TimeSpan.Zero));
        var policy = new ZoneConstraintPolicyViewModel(clock.GetCurrentTime);

        policy.Toggles.Should().HaveCount(4);
        policy.PolicySummary.Should().Contain("All zone constraints");
        policy.HasOverrideHistory.Should().BeFalse();
        policy.HasNoOverrideHistory.Should().BeTrue();

        var keepOut = policy.Toggles.Single(toggle => toggle.ZoneType == "keepout");
        keepOut.SupportsManualOverride.Should().BeTrue();

        keepOut.ApplyOverrideCommand.Execute(null);

        policy.HasOverrideHistory.Should().BeTrue();
        policy.HasNoOverrideHistory.Should().BeFalse();
        policy.OverrideHistory.Should().HaveCount(1);
        policy.OverrideHistory[0].ActionDisplay.Should().Be("Override applied");
        policy.OverrideHistory[0].ZoneTypeDisplay.Should().Contain("Keep-out");

        clock.Advance(TimeSpan.FromMinutes(1));
        keepOut.ClearOverrideCommand.Execute(null);

        policy.OverrideHistory.Should().HaveCount(2);
        policy.OverrideHistory[0].ActionDisplay.Should().Be("Override cleared");

        var headland = policy.Toggles.Single(toggle => toggle.ZoneType == "headland");
        headland.IsEnabled = false;
        policy.PolicySummary.Should().Contain("Headland zones");
    }

    [Fact]
    public void ZoneImportExportPanel_RunsWorkflowAndLogsActivity()
    {
        var clock = new TestClock(new DateTimeOffset(2025, 4, 12, 9, 30, 0, TimeSpan.Zero));
        var panel = new ZoneImportExportPanelViewModel(clock.GetCurrentTime);

        panel.Workflows.Should().HaveCount(3);
        panel.HasActivity.Should().BeFalse();
        panel.HasNoActivity.Should().BeTrue();

        var shapefile = panel.Workflows.First();
        var observedProperties = new List<string>();
        shapefile.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.PropertyName))
            {
                observedProperties.Add(args.PropertyName);
            }
        };

        shapefile.ExecuteCommand.Execute(null);

        shapefile.ExportProgress.Should().Be(1.0);
        shapefile.ExportProgressPercent.Should().Be(100.0);
        observedProperties.Should().Contain(nameof(ZoneImportWorkflowViewModel.ExportProgress));
        observedProperties.Should().Contain(nameof(ZoneImportWorkflowViewModel.ExportProgressPercent));
        shapefile.StatusDisplay.Should().Contain("Imported");
        shapefile.LastRunDisplay.Should().NotBe("—");

        panel.HasActivity.Should().BeTrue();
        panel.HasNoActivity.Should().BeFalse();
        panel.ActivityLog.Should().HaveCount(1);
        panel.ActivityLog[0].ActionDisplay.Should().Be("Import completed");
        panel.StatusMessage.Should().Contain("Imported");
    }

    private sealed class TestClock
    {
        public TestClock(DateTimeOffset start)
        {
            CurrentTime = start;
        }

        public DateTimeOffset CurrentTime { get; private set; }

        public DateTimeOffset GetCurrentTime()
        {
            return CurrentTime;
        }

        public void Advance(TimeSpan delta)
        {
            CurrentTime += delta;
        }
    }
}
