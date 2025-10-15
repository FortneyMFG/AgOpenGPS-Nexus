using System.Linq;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class RadioProvisioningPanelViewModelTests
{
    [Fact]
    public void CreateSample_PopulatesProvisioningState()
    {
        var panel = RadioProvisioningPanelViewModel.CreateSample();

        panel.Summary.Should().Contain("radio bridges");
        panel.Devices.Should().NotBeEmpty();
        panel.Devices.SelectMany(device => device.Steps).Should().NotBeEmpty();
        panel.Profiles.Should().NotBeEmpty();
        panel.AuditTrail.Should().NotBeEmpty();
        panel.Devices.First().StatusDisplay.Should().NotBeNullOrWhiteSpace();
    }
}
