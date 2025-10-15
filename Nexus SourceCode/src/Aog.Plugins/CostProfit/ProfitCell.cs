using System;
using Aog.Core.Paths;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Represents a spatial profit sample that combines revenue and cost values.
/// </summary>
public sealed class ProfitCell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfitCell"/> class.
    /// </summary>
    public ProfitCell(PlanarPoint position, double cellSizeMeters, decimal revenue, decimal cost)
    {
        if (cellSizeMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSizeMeters), "Cell size must be greater than zero.");
        }

        if (revenue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revenue), "Revenue cannot be negative.");
        }

        if (cost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), "Cost cannot be negative.");
        }

        Position = position;
        CellSizeMeters = cellSizeMeters;
        Revenue = decimal.Round(revenue, 2, MidpointRounding.AwayFromZero);
        Cost = decimal.Round(cost, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Gets the planar position of the cell centre.
    /// </summary>
    public PlanarPoint Position { get; }

    /// <summary>
    /// Gets the edge length of the cell in metres.
    /// </summary>
    public double CellSizeMeters { get; }

    /// <summary>
    /// Gets the revenue associated with the cell.
    /// </summary>
    public decimal Revenue { get; }

    /// <summary>
    /// Gets the cost associated with the cell.
    /// </summary>
    public decimal Cost { get; }

    /// <summary>
    /// Gets the profit (revenue minus cost) for the cell.
    /// </summary>
    public decimal Profit => decimal.Round(Revenue - Cost, 2, MidpointRounding.AwayFromZero);
}
