namespace Aog.Plugins.CombineYield;

/// <summary>
/// Result returned by <see cref="YieldImportService"/> after processing an import request.
/// </summary>
/// <param name="Publication">Aggregated layer publication containing smoothing metadata.</param>
/// <param name="Units">Units associated with the imported dataset.</param>
/// <param name="MeasurementCount">Number of measurements ingested.</param>
public sealed record YieldImportResult(
    CombineYieldLayerPublication Publication,
    string Units,
    int MeasurementCount);
