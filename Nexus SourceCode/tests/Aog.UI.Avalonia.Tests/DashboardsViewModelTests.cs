using System;
using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class DashboardsViewModelTests
{
    [Fact]
    public void SteerDashboard_AcceptsHistoricalSamplesAndClampsGains()
    {
        var viewModel = new SteerDashboardViewModel();
        var crossTrack = Enumerable.Range(0, 10).Select(i => i * 0.1);
        var angles = Enumerable.Range(0, 10).Select(i => -5 + i);
        var outputs = Enumerable.Range(0, 10).Select(i => Math.Sin(i));

        viewModel.ApplyHistoricalSamples(crossTrack, angles, outputs);
        viewModel.ProportionalGain = 5; // Should clamp
        viewModel.IntegralGain = -1;    // Should clamp
        viewModel.DerivativeGain = 2;   // Should clamp

        var crossTrackSeries = viewModel.Series.Single(series => series.Id == "autosteer.crossTrack");
        crossTrackSeries.Values.Should().HaveCount(10);

        viewModel.Status.Should().Contain("Cross-track");
        viewModel.GainSummary.Should().Contain("P 2.00");
        viewModel.GainSummary.Should().Contain("I 0.00");
        viewModel.GainSummary.Should().Contain("D 1.00");

        viewModel.TuningParameters.Should().HaveCount(3);
        viewModel.TuningParameters.Select(parameter => parameter.Label)
            .Should().Contain(new[] { "P", "I", "D" });

        var proportionalParameter = viewModel.TuningParameters.Single(parameter => parameter.Id == "controller.p");
        proportionalParameter.Value = 1.23;
        viewModel.ProportionalGain.Should().BeApproximately(1.23, 1e-6);
    }

    [Fact]
    public void ReplayTimeline_UpdatesStatusOnExport()
    {
        var viewModel = new ReplayTimelineViewModel();
        var bookmarks = new[]
        {
            new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(10), "Test", "Note"),
        };

        viewModel.ApplySampleData(new[] { 1.0, 2.0 }, new[] { 0.0, 5.0 }, bookmarks);
        viewModel.ExportCsvCommand.Execute(null);

        viewModel.Bookmarks.Should().HaveCount(1);
        viewModel.ExportStatus.Should().Contain("CSV");
        viewModel.SpeedSamples.Should().HaveCount(2);
    }
}
