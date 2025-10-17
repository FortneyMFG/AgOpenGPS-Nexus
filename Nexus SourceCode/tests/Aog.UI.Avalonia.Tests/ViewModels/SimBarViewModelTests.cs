using System;
using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.ViewModels;

public sealed class SimBarViewModelTests
{
    [Fact]
    public void PlaybackRates_UseMultiplicationSymbol()
    {
        var viewModel = new SimBarViewModel();

        viewModel.PlaybackRates.Select(option => option.Label)
            .Should()
            .ContainInOrder("0.5×", "1×", "2×");
    }

    [Fact]
    public void SelectingPlaybackRate_UpdatesSelection()
    {
        var viewModel = new SimBarViewModel();
        var doubleRate = viewModel.PlaybackRates.Single(option => Math.Abs(option.Rate - 2.0) < 1e-6);

        doubleRate.SelectCommand.Execute(null);

        viewModel.SelectedPlaybackRate.Should().Be(2.0);
    }
}
