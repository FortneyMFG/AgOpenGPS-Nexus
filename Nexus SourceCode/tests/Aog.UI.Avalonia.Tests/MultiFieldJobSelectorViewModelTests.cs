using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class MultiFieldJobSelectorViewModelTests
{
    [Fact]
    public void Constructor_ComputesInitialSelection()
    {
        var selector = CreateSelector();

        selector.TotalFieldCount.Should().Be(2);
        selector.SelectedFieldCount.Should().Be(2);
        selector.SelectedAreaHectares.Should().BeApproximately(21.0, 1e-6);
        selector.SelectionSummary.Should().Be("All 2 fields selected · 21 ha");
        selector.SelectedFieldsDisplay.Should().Be("North 40 & Driveway West");
        selector.AverageCoverageDisplay.Should().Be("35% covered");
        selector.HasSelection.Should().BeTrue();
    }

    [Fact]
    public void ClearAndSelectAll_UpdatesAggregates()
    {
        var selector = CreateSelector();

        selector.ClearSelectionCommand.CanExecute(null).Should().BeTrue();
        selector.ClearSelectionCommand.Execute(null);

        selector.SelectedFieldCount.Should().Be(0);
        selector.HasSelection.Should().BeFalse();
        selector.SelectionSummary.Should().Be("No fields selected · —");
        selector.SelectedFieldsDisplay.Should().Be("No fields selected");

        selector.SelectAllCommand.Execute(null);

        selector.SelectedFieldCount.Should().Be(2);
        selector.HasSelection.Should().BeTrue();
        selector.SelectionSummary.Should().Be("All 2 fields selected · 21 ha");
    }

    [Fact]
    public void CaptureSelectedFields_ReturnsSnapshots()
    {
        var selector = CreateSelector();

        selector.Fields[1].ToggleSelectionCommand.Execute(null);

        var snapshots = selector.CaptureSelectedFields();

        snapshots.Should().ContainSingle();
        snapshots[0].FieldId.Should().Be("field:north");
        snapshots[0].CoveragePercent.Should().Be(42);
    }

    private static MultiFieldJobSelectorViewModel CreateSelector()
    {
        var definitions = new[]
        {
            new JobFieldDefinition("field:north", "North 40", 16.2, 42),
            new JobFieldDefinition("field:drive", "Driveway West", 4.8, 28),
        };

        return new MultiFieldJobSelectorViewModel(definitions);
    }
}
