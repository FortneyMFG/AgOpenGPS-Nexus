using System;
using System.Linq;
using Aog.Core.Layers.Controllers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aog.Core.Tests.Layers;

public class LayerControllerRegistryTests
{
    [Fact]
    public void AddLayerControllers_ShouldRegisterDescriptors()
    {
        var services = new ServiceCollection();

        services.AddLayerControllers(builder =>
        {
            builder.AddController(new LayerControllerDescriptor(
                "controller-1",
                "layer-1",
                TimeSpan.FromMilliseconds(100),
                LayerAggregationStrategy.Average));
            builder.AddController(provider =>
                new LayerControllerDescriptor(
                    "controller-2",
                    "layer-2",
                    TimeSpan.Zero,
                    LayerAggregationStrategy.Sum));
        });

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<ILayerControllerRegistry>();
        var descriptors = registry.GetDescriptors();

        descriptors.Should().HaveCount(2);
        descriptors.Select(descriptor => descriptor.ControllerId)
            .Should().BeEquivalentTo("controller-1", "controller-2");

        var factory = provider.GetRequiredService<ILayerControllerRuntimeFactory>();
        factory.CreateRuntime().Should().NotBeNull();

        provider.GetRequiredService<LayerControllerRuntime>().Should().NotBeNull();
    }

    [Fact]
    public void AddLayerControllers_ShouldThrowWhenNoDescriptorsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLayerControllers();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ILayerControllerRuntimeFactory>();

        var act = () => factory.CreateRuntime();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*layer controller*");
    }

    [Fact]
    public void AddLayerControllers_ShouldRejectDuplicateControllerIds()
    {
        var services = new ServiceCollection();

        services.AddLayerControllers(builder =>
        {
            builder.AddController(new LayerControllerDescriptor(
                "controller-1",
                "layer-1",
                TimeSpan.Zero,
                LayerAggregationStrategy.Average));
            builder.AddController(new LayerControllerDescriptor(
                "controller-1",
                "layer-2",
                TimeSpan.Zero,
                LayerAggregationStrategy.Sum));
        });

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ILayerControllerRegistry>();

        var act = () => registry.GetDescriptors();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*controller-1*");
    }
}
