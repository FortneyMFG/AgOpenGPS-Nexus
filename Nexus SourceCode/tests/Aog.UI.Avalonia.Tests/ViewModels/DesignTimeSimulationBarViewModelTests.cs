using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.ViewModels;

public sealed class DesignTimeSimulationBarViewModelTests
{
    [Fact]
    public void Constructor_PopulatesSimulationState()
    {
        using var viewModel = new DesignTimeSimulationBarViewModel();

        viewModel.Routes.Should().NotBeEmpty();
        viewModel.StatusText.Should().NotBeNullOrEmpty();
        viewModel.PlaybackRates.Should().NotBeEmpty();
    }
}
