using System;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.ViewModels;

public sealed class ReplayTimelineBookmarkViewModelTests
{
    [Fact]
    public void TimestampDisplay_WithSubHourTimestamp_FormatsAsMinutesAndSeconds()
    {
        var viewModel = new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(95), "Label", "Notes");

        viewModel.TimestampDisplay.Should().Be("01:35");
    }

    [Fact]
    public void TimestampDisplay_WithHourTimestamp_PreservesHourComponent()
    {
        var viewModel = new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(3723), "Label", "Notes");

        viewModel.TimestampDisplay.Should().Be("01:02:03");
    }
}
