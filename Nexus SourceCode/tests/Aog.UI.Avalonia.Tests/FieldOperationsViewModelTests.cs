using System;
using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public class FieldOperationsViewModelTests
{
    [Fact]
    public void BoundaryTool_SimplifySelectedReducesVertices()
    {
        var viewModel = BoundaryToolViewModel.CreateSample();
        var originalVertices = viewModel.SelectedPolygon!.VertexCount;

        viewModel.SimplifySelectedCommand.Execute(null);

        viewModel.SelectedPolygon.VertexCount.Should().BeLessThan(originalVertices);
        viewModel.StatusMessage.Should().Contain("Simplified");
        viewModel.OperationJournal.First().Action.Should().Be("Simplified");
    }

    [Fact]
    public void BoundaryTool_MergeRemovesPartner()
    {
        var viewModel = BoundaryToolViewModel.CreateSample();
        var initialCount = viewModel.Polygons.Count;

        viewModel.MergeWithNextCommand.Execute(null);

        viewModel.Polygons.Count.Should().Be(initialCount - 1);
        viewModel.OperationJournal.First().Action.Should().Be("Merged");
    }

    [Fact]
    public void FlagManager_FilterAppliesCategoryAndSearch()
    {
        var viewModel = FlagManagerDialogViewModel.CreateSample();
        viewModel.SelectedCategory = "Boundary";
        viewModel.SearchText = "wet";

        viewModel.FilteredFlags.Should().HaveCount(1);
        viewModel.FilteredFlags[0].Label.Should().Contain("Wet spot");

        viewModel.ClearFilterCommand.Execute(null);
        viewModel.FilteredFlags.Should().HaveCount(viewModel.Flags.Count);
    }

    [Fact]
    public void ShiftPosition_NudgesAndApplyUpdatesState()
    {
        var viewModel = new ShiftPositionDialogViewModel(0.2)
        {
            OffsetEastMeters = 0,
            OffsetNorthMeters = 0,
            HeadingOffsetDegrees = 0,
        };

        viewModel.NudgeEastCommand.Execute(null);
        viewModel.NudgeNorthCommand.Execute(null);
        viewModel.RotateRightCommand.Execute(null);
        viewModel.ApplyCommand.Execute(null);

        viewModel.AppliedEastMeters.Should().BeApproximately(0.2, 0.001);
        viewModel.AppliedNorthMeters.Should().BeApproximately(0.2, 0.001);
        viewModel.AppliedHeadingDegrees.Should().BeApproximately(0.25, 0.001);
        viewModel.ApplicationCount.Should().Be(1);
        viewModel.StatusMessage.Should().Contain("Applied offsets");
    }
}
