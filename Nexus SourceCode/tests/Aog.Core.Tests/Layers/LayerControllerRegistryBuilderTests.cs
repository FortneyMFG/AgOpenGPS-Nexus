using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aog.Core.Eventing;
using Aog.Core.Layers.Controllers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aog.Core.Tests.Layers;

public class LayerControllerRegistryBuilderTests
{
    [Fact]
    public void Build_ShouldThrowWhenNoControllersRegistered()
    {
        var builder = new LayerControllerRegistryBuilder();

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*At least one layer controller descriptor*");
    }

    [Fact]
    public void Add_ShouldRejectDuplicateControllerIdentifiers()
    {
        var builder = new LayerControllerRegistryBuilder();
        builder.Add(new LayerControllerDescriptor("controller-1", "layer-1", TimeSpan.FromMilliseconds(100), LayerAggregationStrategy.Average));

        var act = () => builder.Add(new LayerControllerDescriptor("controller-1", "layer-2", TimeSpan.FromMilliseconds(100), LayerAggregationStrategy.Sum));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*controller-1*");
    }

    [Fact]
    public void Build_ShouldReturnReadOnlyDescriptors()
    {
        var builder = new LayerControllerRegistryBuilder();
        builder.Add("controller-1", "layer-1", TimeSpan.FromMilliseconds(100), LayerAggregationStrategy.Average);

        var descriptors = builder.Build();

        descriptors.Should().BeOfType<ReadOnlyCollection<LayerControllerDescriptor>>();
        descriptors.Should().ContainSingle(d => d.ControllerId == "controller-1");

        var act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddLayerControllerRuntime_ShouldRegisterRuntimeAndDescriptors()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IEventBus, InMemoryEventBus>();

        services.AddLayerControllerRuntime(builder =>
        {
            builder.Add("controller-1", "layer-1", TimeSpan.FromMilliseconds(200), LayerAggregationStrategy.Average);
        });

        using var provider = services.BuildServiceProvider();

        var runtime = provider.GetRequiredService<LayerControllerRuntime>();
        runtime.Should().NotBeNull();

        var descriptors = provider.GetRequiredService<IReadOnlyList<LayerControllerDescriptor>>();
        descriptors.Should().ContainSingle(d => d.ControllerId == "controller-1");

        var bufferPool = provider.GetRequiredService<LayerControllerBufferPool>();
        bufferPool.Should().NotBeNull();

        var poseStream = provider.GetRequiredService<ILayerControllerPoseStream>();
        poseStream.Should().NotBeNull();

        var tileWriter = provider.GetRequiredService<LayerControllerTileWriter>();
        tileWriter.Should().NotBeNull();

        var diagnosticsPublisher = provider.GetRequiredService<LayerControllerDiagnosticsPublisher>();
        diagnosticsPublisher.Should().NotBeNull();
    }
}
