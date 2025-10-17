using System;
using System.Collections.Generic;
using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.ViewModels;

public sealed class ReplayTimelineViewModelTests
{
    [Fact]
    public void Dispose_DetachesTimelineAndExporterHandlers()
    {
        var timeline = new StubTimeline();
        var exporter = new StubExporter();
        var viewModel = new ReplayTimelineViewModel(TimeProvider.System, timeline, exporter);

        timeline.SamplesHandlerCount.Should().Be(1);
        timeline.BookmarksHandlerCount.Should().Be(1);
        exporter.StatusHandlerCount.Should().Be(1);

        viewModel.Dispose();

        timeline.SamplesHandlerCount.Should().Be(0);
        timeline.BookmarksHandlerCount.Should().Be(0);
        exporter.StatusHandlerCount.Should().Be(0);
    }

    private sealed class StubTimeline : IReplayTimeline
    {
        public event EventHandler<ReplayTimelineSamplesChangedEventArgs>? SamplesChanged;
        public event EventHandler<ReplayTimelineBookmarksChangedEventArgs>? BookmarksChanged;

        public IReadOnlyList<double> SpeedSamples { get; private set; } = Array.Empty<double>();
        public IReadOnlyList<double> HeadingSamples { get; private set; } = Array.Empty<double>();
        public IReadOnlyList<ReplayTimelineBookmark> Bookmarks { get; private set; } = Array.Empty<ReplayTimelineBookmark>();

        public int SamplesHandlerCount => SamplesChanged?.GetInvocationList().Length ?? 0;
        public int BookmarksHandlerCount => BookmarksChanged?.GetInvocationList().Length ?? 0;

        public void PublishSamples(IEnumerable<double> speedSamples, IEnumerable<double> headingSamples)
        {
            SpeedSamples = (speedSamples ?? Enumerable.Empty<double>()).ToArray();
            HeadingSamples = (headingSamples ?? Enumerable.Empty<double>()).ToArray();
            SamplesChanged?.Invoke(this, new ReplayTimelineSamplesChangedEventArgs(SpeedSamples, HeadingSamples));
        }

        public void PublishBookmarks(IEnumerable<ReplayTimelineBookmark> bookmarks)
        {
            Bookmarks = (bookmarks ?? Array.Empty<ReplayTimelineBookmark>()).ToArray();
            BookmarksChanged?.Invoke(this, new ReplayTimelineBookmarksChangedEventArgs(Bookmarks));
        }
    }

    private sealed class StubExporter : IReplayTimelineExporter
    {
        public event EventHandler<ReplayExportStatusChangedEventArgs>? ExportStatusChanged;

        public int StatusHandlerCount => ExportStatusChanged?.GetInvocationList().Length ?? 0;

        public void PublishStatus(string status)
        {
            ExportStatusChanged?.Invoke(this, new ReplayExportStatusChangedEventArgs(status));
        }
    }
}
