namespace Aog.Plugins.CostProfit;

/// <summary>
/// Represents a per-currency summary row emitted by the profit export pipeline.
/// </summary>
/// <param name="Currency">Currency code.</param>
/// <param name="Revenue">Aggregated revenue.</param>
/// <param name="Cost">Aggregated cost.</param>
/// <param name="Profit">Net profit.</param>
/// <param name="Margin">Gross margin ratio (0-1).</param>
public sealed record ProfitExportRow(string Currency, decimal Revenue, decimal Cost, decimal Profit, decimal Margin);
