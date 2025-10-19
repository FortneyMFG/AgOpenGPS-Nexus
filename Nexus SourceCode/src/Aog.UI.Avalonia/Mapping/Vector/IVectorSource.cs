using System.Collections.Generic;

namespace Aog.UI.Avalonia.Mapping.Vector;

/// <summary>
/// Supplies vector features to the map pipeline.
/// </summary>
public interface IVectorSource
{
    /// <summary>
    /// Obtains a full snapshot of all vector features. Implementations should cache results to avoid churn.
    /// </summary>
    IReadOnlyList<VectorFeature> GetSnapshot();
}
