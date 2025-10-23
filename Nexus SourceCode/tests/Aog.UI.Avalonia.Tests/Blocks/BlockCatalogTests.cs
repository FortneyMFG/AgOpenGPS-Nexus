using System;
using System.Collections.Generic;
using Aog.UI.Avalonia.Blocks;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Blocks;

public class BlockCatalogTests
{
    [Fact]
    public void All_ReturnsDefinitionsFromProviders()
    {
        var provider = new TestProvider(
            new BlockDefinition { Id = new BlockDefinitionId("Cmd.Test"), Label = "Test" },
            new BlockDefinition { Id = new BlockDefinitionId("Cmd.Other"), Label = "Other" });
        var catalog = new BlockCatalog(new[] { provider });

        catalog.All().Should().HaveCount(2)
            .And.Contain(d => d.Id.Value == "Cmd.Test")
            .And.Contain(d => d.Id.Value == "Cmd.Other");
    }

    [Fact]
    public void Get_ReturnsNullWhenUnknown()
    {
        var catalog = new BlockCatalog(Array.Empty<IBlockProvider>());

        catalog.Get(new BlockDefinitionId("missing")).Should().BeNull();
    }

    [Fact]
    public void Constructor_ThrowsWhenDuplicateIdsDetected()
    {
        var provider = new TestProvider(
            new BlockDefinition { Id = new BlockDefinitionId("Cmd.Test"), Label = "One" },
            new BlockDefinition { Id = new BlockDefinitionId("Cmd.Test"), Label = "Two" });

        var act = () => new BlockCatalog(new[] { provider });

        act.Should().Throw<InvalidOperationException>();
    }

    private sealed class TestProvider : IBlockProvider
    {
        private readonly IEnumerable<BlockDefinition> _definitions;

        public TestProvider(params BlockDefinition[] definitions)
        {
            _definitions = definitions;
        }

        public IEnumerable<BlockDefinition> GetBlocks() => _definitions;
    }
}
