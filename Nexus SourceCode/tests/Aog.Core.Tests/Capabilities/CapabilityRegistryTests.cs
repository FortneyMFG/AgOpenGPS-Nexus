using System.Linq;
using Aog.Core.Capabilities;

namespace Aog.Core.Tests.Capabilities;

public sealed class CapabilityRegistryTests
{
    [Theory]
    [InlineData("mapping:raster", CapabilityCategory.Mapping)]
    [InlineData("mapping:vector", CapabilityCategory.Mapping)]
    [InlineData("mapping:offline", CapabilityCategory.Mapping)]
    [InlineData("mapping:unavailable", CapabilityCategory.Mapping)]
    [InlineData("zones:evaluate", CapabilityCategory.Zones)]
    [InlineData("zones:registry", CapabilityCategory.Zones)]
    [InlineData("zones:edit", CapabilityCategory.Zones)]
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
