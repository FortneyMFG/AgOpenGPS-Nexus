namespace Aog.Plugins.CombineYield;

/// <summary>
/// Enumeration describing the binning scheme applied to yield layers.
/// </summary>
public enum YieldBinningScheme
{
    /// <summary>Bins are calculated using weighted quantiles.</summary>
    Quantile,

    /// <summary>Bins divide the total range into equal intervals.</summary>
    EqualInterval,

    /// <summary>Bins follow explicit breakpoints provided by the caller.</summary>
    Custom
}
