using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.Settings;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Blocks;

public sealed class BlockJsonConvertersTests
{
    [Fact]
    public void BlockDefinitionId_RoundTripsThroughJsonString()
    {
        var options = CreateOptions();
        var identifier = new BlockDefinitionId("Cmd.Start");

        var json = JsonSerializer.Serialize(identifier, options);
        json.Should().Be("\"Cmd.Start\"");

        var result = JsonSerializer.Deserialize<BlockDefinitionId>(json, options);
        result.Should().NotBeNull();
        result!.Value.Should().Be("Cmd.Start");
    }

    [Fact]
    public void BlockInstanceId_RoundTripsThroughJsonString()
    {
        var options = CreateOptions();
        var guid = Guid.NewGuid();
        var identifier = new BlockInstanceId(guid);

        var json = JsonSerializer.Serialize(identifier, options);
        json.Should().Be($"\"{guid}\"");

        var result = JsonSerializer.Deserialize<BlockInstanceId>(json, options);
        result.Should().NotBeNull();
        result!.Value.Should().Be(guid);
    }

    [Fact]
    public void BlockInstanceId_ParsesLegacyObjectShape()
    {
        var options = CreateOptions();
        var guid = Guid.NewGuid();
        var json = $"{{\"Value\":\"{guid}\"}}";

        var result = JsonSerializer.Deserialize<BlockInstanceId>(json, options);
        result.Should().NotBeNull();
        result!.Value.Should().Be(guid);
    }

    [Fact]
    public void UiPreferences_RoundTripsWithBlockInstances()
    {
        var options = CreateOptions();
        options.WriteIndented = false;
        var guid = Guid.NewGuid();

        var preferences = new UiPreferences
        {
            ShellLayout = new ShellLayoutPreferences
            {
                Instances = new List<BlockInstance>
                {
                    new()
                    {
                        InstanceId = new BlockInstanceId(guid),
                        DefinitionId = new BlockDefinitionId("Cmd.Start"),
                        Region = BlockRegion.Bottom,
                        Order = 2,
                        Origin = BlockOrigin.Clone,
                    },
                },
            },
        };

        var json = JsonSerializer.Serialize(preferences, options);
        json.Should().Contain("Cmd.Start");
        json.Should().Contain(guid.ToString());

        var result = JsonSerializer.Deserialize<UiPreferences>(json, options);
        result.Should().NotBeNull();
        var instance = result!.ShellLayout.Instances.Single();
        instance.DefinitionId.Value.Should().Be("Cmd.Start");
        instance.InstanceId.Value.Should().Be(guid);
        instance.Region.Should().Be(BlockRegion.Bottom);
        instance.Origin.Should().Be(BlockOrigin.Clone);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        BlockJsonConverters.Configure(options);
        return options;
    }
}
