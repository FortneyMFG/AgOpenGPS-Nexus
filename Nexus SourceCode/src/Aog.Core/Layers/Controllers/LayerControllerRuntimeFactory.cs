using System;
using System.Buffers;
using Aog.Core.Paths;

namespace Aog.Core.Layers.Controllers;

internal sealed class LayerControllerRuntimeFactory : ILayerControllerRuntimeFactory
{
    private readonly ILayerControllerRegistry _registry;
    private readonly TimeProvider _timeProvider;
    private readonly ArrayPool<PlanarPoint> _positionBufferPool;

    public LayerControllerRuntimeFactory(
        ILayerControllerRegistry registry,
        TimeProvider timeProvider,
        ArrayPool<PlanarPoint> positionBufferPool)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _positionBufferPool = positionBufferPool ?? throw new ArgumentNullException(nameof(positionBufferPool));
    }

    public LayerControllerRuntime CreateRuntime()
    {
        return new LayerControllerRuntime(_registry.GetDescriptors(), _timeProvider, _positionBufferPool);
    }
}
