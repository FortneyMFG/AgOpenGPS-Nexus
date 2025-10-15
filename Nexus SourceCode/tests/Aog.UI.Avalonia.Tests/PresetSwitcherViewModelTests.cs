using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class PresetSwitcherViewModelTests
{
    [Fact]
    public void CreateSample_ShouldExposePresetsAndTasks()
    {
        var viewModel = PresetSwitcherViewModel.CreateSample();

        viewModel.Presets.Should().HaveCount(3);
        viewModel.SelectedPreset.Should().NotBeNull();
        viewModel.SelectedPreset!.Tasks.Should().NotBeEmpty();
        viewModel.ActivePreset.Should().Be(viewModel.SelectedPreset);
        viewModel.StatusMessage.Should().Contain("active");
    }

    [Fact]
    public void BeginApply_ShouldResetTasksAndSetStatus()
    {
        var viewModel = PresetSwitcherViewModel.CreateSample();
        var preset = viewModel.Presets.First();
        preset.Tasks.First().SetState(PresetTaskState.Succeeded);
        preset.Tasks.First().Progress = 1.0;

        viewModel.BeginApply(preset);

        viewModel.IsApplying.Should().BeTrue();
        viewModel.StatusMessage.Should().Contain("Applying");
        preset.Tasks.Should().AllSatisfy(task =>
        {
            task.State.Should().Be(PresetTaskState.Pending);
            task.Progress.Should().Be(0);
        });
    }

    [Fact]
    public void UpdateTaskState_ShouldUpdateProgressAndDetail()
    {
        var viewModel = PresetSwitcherViewModel.CreateSample();
        var preset = viewModel.SelectedPreset!;

        viewModel.UpdateTaskState(preset.Tasks.First().TaskId, PresetTaskState.Running, progress: 0.5, detail: "Halfway");

        var task = preset.Tasks.First();
        task.State.Should().Be(PresetTaskState.Running);
        task.Progress.Should().BeApproximately(0.5, 1e-6);
        task.Detail.Should().Be("Halfway");
    }

    [Fact]
    public void CompleteApply_ShouldMarkPresetActiveWhenSuccessful()
    {
        var viewModel = PresetSwitcherViewModel.CreateSample();
        var preset = viewModel.SelectedPreset!;

        viewModel.BeginApply(preset);
        viewModel.CompleteApply(preset, succeeded: true, message: "Preset ready");

        viewModel.IsApplying.Should().BeFalse();
        viewModel.ActivePreset.Should().Be(preset);
        viewModel.StatusMessage.Should().Be("Preset ready");
        preset.Tasks.Should().AllSatisfy(task => task.State.Should().NotBe(PresetTaskState.Pending));
    }
}
