using System.Collections.Generic;
using System.Linq;

namespace Aog.Plugins.Mapping.Vector;

public sealed class InMemoryVectorSource : IVectorSource
{
    private readonly IReadOnlyList<VectorFeature> _features;

    public InMemoryVectorSource(IEnumerable<VectorFeature> features)
    {
        _features = features?.ToArray() ?? [];
    }

    public IReadOnlyList<VectorFeature> GetSnapshot() => _features;
}
