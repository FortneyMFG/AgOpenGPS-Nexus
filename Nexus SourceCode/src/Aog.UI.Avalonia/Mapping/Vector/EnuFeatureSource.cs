using System;
using System.Collections.Generic;
using Aog.UI.Avalonia.Mapping.Core;

namespace Aog.UI.Avalonia.Mapping.Vector;

/// <summary>
/// Lightweight in-memory feature source for ENU polygons/polylines.
/// </summary>
public sealed class EnuFeatureSource : IVectorSource
{
    private readonly IReadOnlyList<VectorFeature> _features;

    public EnuFeatureSource(IEnumerable<IReadOnlyList<Double3>> polygons, VectorFeatureType featureType = VectorFeatureType.Polygon)
    {
        if (polygons is null)
        {
            throw new ArgumentNullException(nameof(polygons));
        }

        var list = new List<VectorFeature>();
        foreach (var polygon in polygons)
        {
            list.Add(new VectorFeature(featureType, new List<Double3>(polygon)));
        }

        _features = list;
    }

    public EnuFeatureSource(params VectorFeature[] features)
    {
        _features = features ?? Array.Empty<VectorFeature>();
    }

    public IReadOnlyList<VectorFeature> GetSnapshot() => _features;
}
