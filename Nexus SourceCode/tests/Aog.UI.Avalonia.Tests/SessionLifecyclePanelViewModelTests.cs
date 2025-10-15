using System;
using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class SessionLifecyclePanelViewModelTests
{
    [Fact]
    public void StartPauseResumeComplete_TracksLifecycle()
    {
        var clock = new TestClock(new DateTimeOffset(2025, 4, 10, 8, 0, 0, TimeSpan.Zero));
        var panel = CreatePanel(clock);

        panel.SessionNotes = "Wind 12 kph";
        panel.StartSessionCommand.Execute(null);

        panel.ActiveSession.Should().NotBeNull();
        panel.FieldSelector.HasSelection.Should().BeTrue();
        panel.StatusMessage.Should().Be("Started Session 1.");
        panel.FieldSelector.StatusMessage.Should().Contain("Mounted 2 field(s)");

        var active = panel.ActiveSession!;
        active.State.Should().Be(SessionLifecycleState.Active);
        active.Notes.Should().ContainSingle(note => note.Text == "Wind 12 kph");
        active.FieldSummary.Should().Contain("North 40");

        clock.Advance(TimeSpan.FromMinutes(15));
        panel.PauseSessionCommand.Execute(null);

        active.State.Should().Be(SessionLifecycleState.Paused);
        active.ActiveDuration.Should().Be(TimeSpan.FromMinutes(15));
        panel.StatusMessage.Should().Be("Session 1 paused.");

        clock.Advance(TimeSpan.FromMinutes(5));
        panel.ResumeSessionCommand.Execute(null);

        active.State.Should().Be(SessionLifecycleState.Active);
        panel.StatusMessage.Should().Be("Session 1 resumed.");

        panel.SessionNotes = "Completed warmup passes";
        clock.Advance(TimeSpan.FromMinutes(45));
        panel.CompleteSessionCommand.Execute(null);

        panel.ActiveSession.Should().BeNull();
        panel.StatusMessage.Should().Be("Session 1 completed.");
        panel.SessionName.Should().Be("Session 2");
        panel.FieldSelector.StatusMessage.Should().Contain("Session closed");

        var entry = panel.SessionTimeline.Should().ContainSingle().Subject;
        entry.State.Should().Be(SessionLifecycleState.Completed);
        entry.ActiveDuration.Should().Be(TimeSpan.FromMinutes(60));
        entry.Notes.Select(note => note.Text).Should().Contain(new[] { "Wind 12 kph", "Completed warmup passes" });
        entry.EndedAt.Should().Be(clock.CurrentTime);
    }

    [Fact]
    public void StartSessionCommand_WithoutFields_ShowsError()
    {
        var clock = new TestClock(new DateTimeOffset(2025, 4, 10, 8, 0, 0, TimeSpan.Zero));
        var panel = CreatePanel(clock);

        panel.FieldSelector.ClearSelectionCommand.Execute(null);
        panel.StartSessionCommand.Execute(null);

        panel.StatusMessage.Should().Be("Select at least one field before starting a session.");
        panel.HasError.Should().BeTrue();
        panel.SessionTimeline.Should().BeEmpty();
    }

    private static SessionLifecyclePanelViewModel CreatePanel(TestClock clock)
    {
        var fields = new[]
        {
            new JobFieldDefinition("field:north", "North 40", 16.2, 42),
            new JobFieldDefinition("field:drive", "Driveway West", 4.8, 28),
        };

        return new SessionLifecyclePanelViewModel("job:2025-plant-corn", fields, clock.GetCurrentTime);
    }

    private sealed class TestClock
    {
        public TestClock(DateTimeOffset start)
        {
            CurrentTime = start;
        }

        public DateTimeOffset CurrentTime { get; private set; }

        public DateTimeOffset GetCurrentTime()
        {
            return CurrentTime;
        }

        public void Advance(TimeSpan delta)
        {
            CurrentTime += delta;
        }
    }
}
