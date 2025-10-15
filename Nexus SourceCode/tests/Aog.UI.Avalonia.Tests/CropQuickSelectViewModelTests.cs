using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class CropQuickSelectViewModelTests
{
    [Fact]
    public void CreateSample_SeedsGroupsAndSelection()
    {
        var viewModel = CropQuickSelectViewModel.CreateSample();

        viewModel.Groups.Should().HaveCount(3);
        viewModel.Groups.SelectMany(group => group.Options)
            .Single(option => option.OptionId == "rotation-soy-2025").IsSelected.Should().BeTrue();

        viewModel.SelectionStatus.Should().Contain("Soybeans");
        viewModel.SelectionStatus.Should().Contain("North 80");
        viewModel.SelectionDetails.Should().Contain("2025 Planned");
        viewModel.HasSelection.Should().BeTrue();
    }

    [Fact]
    public void SelectingOptionUpdatesStatusAndSelection()
    {
        var viewModel = CropQuickSelectViewModel.CreateSample();
        var target = viewModel.Groups.SelectMany(group => group.Options)
            .Single(option => option.OptionId == "favorite-alfalfa");
        var previouslySelected = viewModel.Groups.SelectMany(group => group.Options)
            .Single(option => option.IsSelected);

        target.SelectCommand.Execute(null);

        target.IsSelected.Should().BeTrue();
        previouslySelected.IsSelected.Should().BeFalse();
        viewModel.SelectionStatus.Should().Contain("Alfalfa");
        viewModel.SelectionStatus.Should().Contain("Queued");
        viewModel.SelectionDetails.Should().Contain("Perennial");
    }
}
