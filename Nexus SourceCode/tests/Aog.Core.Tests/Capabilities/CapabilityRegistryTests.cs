using System.Linq;
using Aog.Core.Capabilities;

namespace Aog.Core.Tests.Capabilities;

public sealed class CapabilityRegistryTests
{
    [Theory]
    [InlineData("guidance.telemetry", CapabilityCategory.Guidance)]
    [InlineData("mapping:raster", CapabilityCategory.Mapping)]
    [InlineData("mapping:vector", CapabilityCategory.Mapping)]
    [InlineData("mapping:offline", CapabilityCategory.Mapping)]
    [InlineData("mapping:unavailable", CapabilityCategory.Mapping)]
    [InlineData("zones:evaluate", CapabilityCategory.Zones)]
    [InlineData("zones:registry", CapabilityCategory.Zones)]
    [InlineData("zones:edit", CapabilityCategory.Zones)]
    [InlineData("sections.control", CapabilityCategory.Sections)]
    [InlineData("sections.telemetry", CapabilityCategory.Sections)]
    [InlineData("fileio.import", CapabilityCategory.DataOperations)]
    [InlineData("fileio.export", CapabilityCategory.DataOperations)]
    [InlineData("replay.guidance", CapabilityCategory.Replay)]
    [InlineData("replay.pose", CapabilityCategory.Replay)]
    [InlineData("devices.inventory", CapabilityCategory.Devices)]
    [InlineData("devices.health", CapabilityCategory.Devices)]
    [InlineData("devices.firmware", CapabilityCategory.Devices)]
    [InlineData("navigation.pose", CapabilityCategory.Navigation)]
    [InlineData("navigation.imu", CapabilityCategory.Navigation)]
    [InlineData("navigation.pose.quality", CapabilityCategory.Navigation)]
    [InlineData("isobus.task-controller", CapabilityCategory.Transports)]
    [InlineData("isobus.universal-terminal", CapabilityCategory.Transports)]
    [InlineData("bridge.udp-mirror", CapabilityCategory.Transports)]
    [InlineData("gnss.corrections", CapabilityCategory.Navigation)]
    [InlineData("telemetry.corrections", CapabilityCategory.Telemetry)]
    [InlineData("telemetry.logging", CapabilityCategory.Telemetry)]
    [InlineData("planter.monitor.telemetry", CapabilityCategory.Agronomy)]
    [InlineData("planter.monitor.analytics", CapabilityCategory.Agronomy)]
    public void TryGetDefinition_KnownCapabilities_ReturnMetadata(string capability, CapabilityCategory expectedCategory)
    {
        var success = CapabilityRegistry.TryGetDefinition(capability, out var definition);

        Assert.True(success);
        Assert.NotNull(definition);
        Assert.Equal(expectedCategory, definition.Category);
        Assert.Equal(capability, definition!.Name);
        Assert.False(string.IsNullOrWhiteSpace(definition.Summary));
        Assert.False(string.IsNullOrWhiteSpace(definition.DefaultVersion));
    }

    [Fact]
    public void All_DefinitionsAreUniqueByName()
    {
        var groups = CapabilityRegistry.All
            .GroupBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.All(groups, group => Assert.Single(group));
    }

    [Fact]
    public void DefinitionsExposeAttributes()
    {
        var found = CapabilityRegistry.TryGetDefinition("mapping:raster", out var definition);

        Assert.True(found);
        Assert.NotNull(definition);

        Assert.True(definition!.Attributes.ContainsKey("surface"));
        Assert.Equal("raster", definition.Attributes["surface"]);
    }
}
