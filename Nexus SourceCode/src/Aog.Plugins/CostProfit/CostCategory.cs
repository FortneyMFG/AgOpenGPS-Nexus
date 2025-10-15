namespace Aog.Plugins.CostProfit;

/// <summary>
/// Enumerates the supported cost categories tracked by the cost/profit plugin.
/// </summary>
public enum CostCategory
{
    /// <summary>
    /// Seed acquisition or usage costs.
    /// </summary>
    Seed,

    /// <summary>
    /// Chemical product costs (fertiliser, herbicide, fungicide, etc.).
    /// </summary>
    Chemistry,

    /// <summary>
    /// Fuel and energy costs.
    /// </summary>
    Fuel,

    /// <summary>
    /// Labour expenses associated with operations.
    /// </summary>
    Labour,

    /// <summary>
    /// Machinery or implement rental and depreciation costs.
    /// </summary>
    Equipment,

    /// <summary>
    /// Miscellaneous costs that do not fall into the predefined buckets.
    /// </summary>
    Miscellaneous
}
