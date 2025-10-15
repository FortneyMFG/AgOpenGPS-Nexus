using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class MeshSharePanelViewModelTests
{
    [Fact]
    public void CreateSample_PopulatesDevicesAndGrants()
    {
        var panel = MeshSharePanelViewModel.CreateSample();

        panel.Devices.Should().NotBeEmpty();
        panel.Summary.Should().Contain("mesh device");

        var device = panel.Devices.First();
        device.DisplayName.Should().NotBeNullOrWhiteSpace();
        device.HasShareGrants.Should().BeTrue();
        device.ShareLayerSummary.Should().NotBeNull();
        device.SubscribeGrants.Should().NotBeEmpty();
    }
}
