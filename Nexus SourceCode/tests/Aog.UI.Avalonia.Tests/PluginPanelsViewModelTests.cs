using System.Collections.Generic;
using Aog.Core.V1;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class PluginPanelsViewModelTests
{
    [Fact]
    public void SteerPanel_ApplySteerCommandUpdatesProperties()
    {
        var viewModel = new SteerPanelViewModel();
        var command = new SteerCmd
        {
            Enable = true,
            TargetWheelAngleDeg = 12.3,
            FeedForward = 0.25,
            ControllerOutput = -0.42,
        };

        viewModel.ApplySteerCommand(command);

        viewModel.IsEnabled.Should().BeTrue();
        viewModel.TargetWheelAngleDegrees.Should().BeApproximately(12.3, 1e-6);
        viewModel.FeedForward.Should().BeApproximately(0.25, 1e-6);
        viewModel.ControllerOutput.Should().BeApproximately(-0.42, 1e-6);
    }

    [Fact]
    public void SectionsPanel_ApplyMaskAndManualOverride()
    {
        var viewModel = new SectionsPanelViewModel();
        var mask = new SectionMask { SectionCount = 6, Mask = 0b001011 };

        viewModel.ApplySectionMask(mask);

        viewModel.IsAutoEnabled.Should().BeTrue();
        viewModel.SectionCount.Should().Be(6);
        viewModel.CurrentMask.Should().Be(0b001011u);
        viewModel.Sections[0].IsEnabled.Should().BeTrue();
        viewModel.Sections[3].IsEnabled.Should().BeTrue();
        viewModel.Sections[5].IsVisible.Should().BeTrue();
        viewModel.Sections[7].IsVisible.Should().BeFalse();

        // Toggle a section to force manual mode.
        viewModel.Sections[0].IsEnabled = false;

        viewModel.IsAutoEnabled.Should().BeFalse();
        viewModel.CurrentMask.Should().Be(0b001010u);
    }

    [Fact]
    public void PlanterPanel_AppliesStatusesAndSummarises()
    {
        var viewModel = new PlanterPanelViewModel();
        var statuses = new List<PlanterRowStatus>
        {
            new() { RowIndex = 0, TargetPopulationPerMeter = 10, ActualPopulationPerMeter = 10, Quality = PlanterRowQuality.Ok },
            new() { RowIndex = 1, TargetPopulationPerMeter = 10, ActualPopulationPerMeter = 8, SkipRate = 0.2, Quality = PlanterRowQuality.Skip },
            new() { RowIndex = 2, TargetPopulationPerMeter = 10, ActualPopulationPerMeter = 11, DoubleRate = 0.1, Quality = PlanterRowQuality.Double },
        };

        viewModel.ApplyRowStatuses(statuses);

        viewModel.Rows.Should().HaveCount(3);
        viewModel.Rows[1].Quality.Should().Be(PlanterRowQuality.Skip);
        viewModel.Rows[2].DoubleRate.Should().BeApproximately(0.1, 1e-6);
        viewModel.Summary.Should().Contain("Rows: 3");
        viewModel.Summary.Should().Contain("Skips 1");
        viewModel.Summary.Should().Contain("Doubles 1");
    }

    [Fact]
    public void PlanterPanel_IgnoresOutOfRangeRowIndexes()
    {
        var viewModel = new PlanterPanelViewModel();
        var statuses = new List<PlanterRowStatus>
        {
            new() { RowIndex = uint.MaxValue, TargetPopulationPerMeter = 12, ActualPopulationPerMeter = 12, Quality = PlanterRowQuality.Ok },
            new() { RowIndex = 0, TargetPopulationPerMeter = 10, ActualPopulationPerMeter = 10, Quality = PlanterRowQuality.Ok },
        };

        viewModel.ApplyRowStatuses(statuses);

        viewModel.Rows.Should().HaveCount(1);
        viewModel.Rows[0].RowIndex.Should().Be(0);
        viewModel.Summary.Should().Contain("Rows: 1");
        viewModel.Summary.Should().Contain("Ignored 1 invalid update");
    }
}
