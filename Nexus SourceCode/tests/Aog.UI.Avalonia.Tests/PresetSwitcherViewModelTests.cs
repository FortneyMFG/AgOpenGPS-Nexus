using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class PresetSwitcherViewModelTests
{
    [Fact]
    public void CreateSample_ShouldInitializeActivePreset()
    {
        var switcher = PresetSwitcherViewModel.CreateSample();

        switcher.ActivePresetDisplay.Should().Be("Planter – 16R");
        switcher.IsBusy.Should().BeFalse();
        switcher.HasActivePresetAlerts.Should().BeFalse();
        switcher.Presets.Should().HaveCountGreaterThan(1);
        switcher.Presets.Any(p => p.IsActive).Should().BeTrue();
    }

    [Fact]
    public void ApplyPreset_ShouldUpdateActiveState()
    {
        var switcher = PresetSwitcherViewModel.CreateSample();
        var runningPreset = switcher.Presets.Single(preset => preset.OrchestrationState == PresetOrchestrationState.Running);

        runningPreset.ApplyCommand.Execute(null);

        switcher.ActivePresetDisplay.Should().Be(runningPreset.DisplayName);
        runningPreset.IsActive.Should().BeTrue();
        switcher.IsBusy.Should().BeTrue();
        switcher.HasActivePresetAlerts.Should().BeTrue();
        switcher.ActivePresetAlerts.Should().Contain("Mapping plugin warming caches");
    }
}
