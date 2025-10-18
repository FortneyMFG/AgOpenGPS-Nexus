using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
        var angles = Enumerable.Range(0, 10).Select(i => -5.0 + i);
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
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 34, 56, TimeSpan.Zero));
        var viewModel = new ReplayTimelineViewModel(timeProvider);
        var bookmarks = new[]
        {
            new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(10), "Test", "Note"),
            new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(10), "Duplicate", "Note"),
        };

        viewModel.ApplySampleData(new[] { 1.0, 2.0 }, new[] { 0.0, 5.0 }, bookmarks);
        viewModel.ExportCsvCommand.Execute(null);

        viewModel.Bookmarks.Should().HaveCount(1);
        viewModel.Bookmarks.Single().Label.Should().Be("Test");
        viewModel.ExportStatus.Should().Be("Export queued: CSV snapshot at 12:34:56");
        viewModel.SpeedSamples.Should().HaveCount(2);
    }

    [Fact]
    public void ReplayTimeline_BookmarksAreReadOnly()
    {
        var viewModel = new ReplayTimelineViewModel();
        var bookmarks = new[]
        {
            new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(10), "Test", "Note"),
        };

        viewModel.ApplySampleData(Array.Empty<double>(), Array.Empty<double>(), bookmarks);

        viewModel.Bookmarks.Should()
            .NotBeAssignableTo<ObservableCollection<ReplayTimelineBookmarkViewModel>>();

        var modifyingAction = () => ((ICollection<ReplayTimelineBookmarkViewModel>)viewModel.Bookmarks)
            .Add(new ReplayTimelineBookmarkViewModel(TimeSpan.Zero, "Injected", "Should fail"));

        modifyingAction.Should().Throw<NotSupportedException>();
    }

        [Fact]
        public void ReplayTimelineBookmarks_DisplayInvariantTimestamps()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var bookmark = new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(65), "Label", "Notes");

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-EG");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-EG");

                bookmark.TimestampDisplay.Should().Be("01:05");

                var longBookmark = new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(3723), "Long", "Notes");
                longBookmark.TimestampDisplay.Should().Be("01:02:03");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Fact]
        public void ReplayTimeline_RaisesPropertyChangedForExportProgress()
        {
            var timeProvider = new FixedTimeProvider(new DateTimeOffset(2024, 1, 1, 7, 0, 0, TimeSpan.Zero));
            var viewModel = new ReplayTimelineViewModel(timeProvider);
            var observed = new List<string>();
            viewModel.PropertyChanged += (_, args) => observed.Add(args.PropertyName ?? string.Empty);

            viewModel.ExportGeoJsonCommand.Execute(0.25);

            observed.Should().Contain(nameof(ReplayTimelineViewModel.ExportStatus));
            observed.Should().Contain(nameof(ReplayTimelineViewModel.ExportProgress));
            observed.Should().Contain(nameof(ReplayTimelineViewModel.IsExportInProgress));
            viewModel.ExportProgress.Should().Be(0.25);
            viewModel.IsExportInProgress.Should().BeTrue();

            observed.Clear();
            viewModel.ExportGeoJsonCommand.Execute(1.0);

            observed.Should().Contain(nameof(ReplayTimelineViewModel.ExportProgress));
            observed.Should().Contain(nameof(ReplayTimelineViewModel.IsExportInProgress));
            viewModel.ExportProgress.Should().Be(1.0);
            viewModel.IsExportInProgress.Should().BeFalse();
        }
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _localNow;
        private readonly TimeZoneInfo _timeZone;

        public FixedTimeProvider(DateTimeOffset localNow)
        {
            _localNow = localNow;
            var offset = localNow.Offset;
            _timeZone = TimeZoneInfo.CreateCustomTimeZone(
                $"FixedOffset_{offset.Ticks}",
                offset,
                "Fixed offset",
                "Fixed offset");
        }

        public override DateTimeOffset GetUtcNow() => _localNow.ToUniversalTime();

        public override TimeZoneInfo LocalTimeZone => _timeZone;
    }
}
