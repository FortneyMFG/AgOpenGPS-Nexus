using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Aog.Core.Layers;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Converts profit rollups and spatial samples into exportable layer documents and
/// summary rows.
/// </summary>
public sealed class ProfitExportPipeline
{
    private readonly ProfitExportOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfitExportPipeline"/> class.
    /// </summary>
    public ProfitExportPipeline(ProfitExportOptions options, TimeProvider? timeProvider = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Creates an agronomic layer representing profit per cell.
    /// </summary>
    /// <param name="cells">Profit cells to export.</param>
    /// <param name="timestamp">Optional timestamp override.</param>
    public AgronomicLayerDocument CreateProfitLayer(IEnumerable<ProfitCell> cells, DateTimeOffset? timestamp = null)
    {
        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        var orderedCells = cells
            .Select(cell => cell ?? throw new ArgumentException("Cells cannot contain null entries.", nameof(cells)))
            .OrderBy(cell => cell.Position.Northing)
            .ThenBy(cell => cell.Position.Easting)
            .ToList();

        if (orderedCells.Count == 0)
        {
            throw new InvalidOperationException("At least one profit cell is required.");
        }

        var createdAt = timestamp ?? _timeProvider.GetUtcNow();
        var agronomicCells = orderedCells
            .Select(cell => new AgronomicLayerCell(
                cell.Position,
                cell.CellSizeMeters,
                decimal.ToDouble(cell.Profit)))
            .ToList();

        var provenance = new LayerProvenance(
            _options.Source,
            _options.Transform,
            ComputeHash(orderedCells),
            createdAt,
            _options.Actor);

        return new AgronomicLayerDocument(
            _options.LayerId,
            _options.LayerKind,
            _options.Units,
            createdAt,
            _options.CreatedBy,
            agronomicCells,
            provenance);
    }

    /// <summary>
    /// Produces per-currency summary rows derived from a rollup.
    /// </summary>
    /// <param name="rollup">Rollup to convert.</param>
    public IReadOnlyList<ProfitExportRow> CreateSummaryRows(ProfitRollup rollup)
    {
        if (rollup is null)
        {
            throw new ArgumentNullException(nameof(rollup));
        }

        var currencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var currency in rollup.RevenueByCurrency.Keys)
        {
            currencies.Add(currency);
        }

        foreach (var currency in rollup.CostByCurrency.Keys)
        {
            currencies.Add(currency);
        }

        foreach (var currency in rollup.ProfitByCurrency.Keys)
        {
            currencies.Add(currency);
        }

        var rows = currencies
            .Select(currency => new ProfitExportRow(
                currency,
                rollup.GetRevenue(currency),
                rollup.GetCost(currency),
                rollup.GetProfit(currency),
                rollup.GetMargin(currency)))
            .OrderBy(row => row.Currency, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return rows;
    }

    private static string ComputeHash(IReadOnlyList<ProfitCell> cells)
    {
        using var sha = SHA256.Create();
        var builder = new StringBuilder();
        foreach (var cell in cells)
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:F3},{1:F3},{2:F3},{3:F2},{4:F2},{5:F2};",
                cell.Position.Easting,
                cell.Position.Northing,
                cell.CellSizeMeters,
                cell.Profit,
                cell.Revenue,
                cell.Cost);
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
