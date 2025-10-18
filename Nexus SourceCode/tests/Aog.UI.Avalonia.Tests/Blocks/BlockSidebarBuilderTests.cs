using System.Collections.Generic;
using System.Linq;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Blocks;

public sealed class BlockSidebarBuilderTests
{
    [Fact]
    public void Build_ReturnsCommandButtonsForRegion()
    {
        var catalog = new BlockCatalog(new IBlockProvider[] { new CoreBlockProvider() });
        var messages = new List<string>();
        var builder = new BlockSidebarBuilder(catalog, messages.Add);
        var instances = new[]
        {
            new BlockInstance
            {
                DefinitionId = new BlockDefinitionId("Cmd.Start"),
                Region = BlockRegion.Bottom,
                Order = 2,
                Origin = BlockOrigin.Clone,
            },
            new BlockInstance
            {
                DefinitionId = new BlockDefinitionId("Cmd.Pause"),
                Region = BlockRegion.Bottom,
                Order = 1,
                Origin = BlockOrigin.Clone,
            },
        };

        var buttons = builder.Build(BlockRegion.Bottom, instances);

        buttons.Should().HaveCount(2);
        buttons.Select(button => button.Label)
            .Should().ContainInOrder("Pause", "Start");

        buttons.First().Command.Execute(null);
        messages.Should().ContainSingle(message => message == "Autoguidance paused.");
    }

    [Fact]
    public void Build_IgnoresUnknownDefinitions()
    {
        var catalog = new BlockCatalog(new IBlockProvider[] { new CoreBlockProvider() });
        var builder = new BlockSidebarBuilder(catalog, _ => { });
        var instances = new[]
        {
            new BlockInstance
            {
                DefinitionId = new BlockDefinitionId("Cmd.Unknown"),
                Region = BlockRegion.Bottom,
                Order = 0,
            },
        };

        var buttons = builder.Build(BlockRegion.Bottom, instances);

        buttons.Should().BeEmpty();
    }
}
