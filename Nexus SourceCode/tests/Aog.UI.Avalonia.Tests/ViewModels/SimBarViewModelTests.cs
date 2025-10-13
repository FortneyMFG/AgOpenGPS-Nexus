using System;
using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.ViewModels;

public class SimBarViewModelTests
{
    [Fact]
    public void TogglePlayPauseCommand_TogglesIsPlaying()
    {
        var viewModel = new SimBarViewModel();

        viewModel.IsPlaying.Should().BeTrue();

        viewModel.TogglePlayPauseCommand.Execute(null);
        viewModel.IsPlaying.Should().BeFalse();

        viewModel.TogglePlayPauseCommand.Execute(null);
        viewModel.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void Seek_ClampsToDuration()
    {
        var viewModel = new SimBarViewModel();
        viewModel.SetDuration(TimeSpan.FromMinutes(1));

        viewModel.Seek(TimeSpan.FromSeconds(30));
        viewModel.CurrentPositionSeconds.Should().Be(30);

        viewModel.Seek(TimeSpan.FromMinutes(2));
        viewModel.CurrentPositionSeconds.Should().Be(60);

        viewModel.Seek(TimeSpan.FromSeconds(-5));
        viewModel.CurrentPositionSeconds.Should().Be(0);
    }

    [Fact]
    public void SelectingPlaybackRate_UpdatesSelection()
    {
        var viewModel = new SimBarViewModel();

        viewModel.SelectedPlaybackRate.Should().Be(1.0);
        var fastRate = viewModel.PlaybackRates.Single(rate => Math.Abs(rate.Rate - 2.0) < 0.001);

        fastRate.IsSelected = true;

        viewModel.SelectedPlaybackRate.Should().Be(2.0);
        viewModel.PlaybackRates.Where(rate => rate != fastRate).Should().OnlyContain(rate => !rate.IsSelected);
    }

    [Fact]
    public void StreamRoute_AllowsChangingSelectedSource()
    {
        var viewModel = new SimBarViewModel();
        var firstRoute = viewModel.StreamRoutes.First();

        firstRoute.SelectedSource.Should().Be("Scenario simulator");
        firstRoute.SelectedSource = "Replay file";

        firstRoute.SelectedSource.Should().Be("Replay file");
    }
}
