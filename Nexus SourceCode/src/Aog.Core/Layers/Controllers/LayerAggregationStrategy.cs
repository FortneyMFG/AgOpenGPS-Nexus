namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Defines how engineering values should be aggregated when producing snapshots.
/// </summary>
public enum LayerAggregationStrategy
{
    /// <summary>
    /// Engineering values are averaged using the accumulated area as weight.
    /// </summary>
    Average = 0,

    /// <summary>
    /// Engineering values represent totals and are summed without normalisation.
    /// </summary>
    Sum = 1,

    /// <summary>
    /// Engineering values should hold the last observed sample when no fresh data is present.
    /// </summary>
    Hold = 2,
}
