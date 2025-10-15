using System;
using System.Collections.Generic;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Aggregates revenue contributions with ledger cost summaries to produce profit
/// rollups for reporting and export pipelines.
/// </summary>
public sealed class ProfitAnalyticsRollupService
{
    private readonly CostLedger _ledger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfitAnalyticsRollupService"/> class.
    /// </summary>
    /// <param name="ledger">Ledger that stores cost entries.</param>
    public ProfitAnalyticsRollupService(CostLedger ledger)
    {
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    }

    /// <summary>
    /// Creates a profit rollup for the specified scope filter using the supplied
    /// revenue contributions.
    /// </summary>
    /// <param name="filter">Scope filter to evaluate.</param>
    /// <param name="revenueContributions">Revenue contributions to include.</param>
    public ProfitRollup CreateRollup(CostScopeFilter filter, IEnumerable<RevenueContribution> revenueContributions)
    {
        if (filter is null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        if (revenueContributions is null)
        {
            throw new ArgumentNullException(nameof(revenueContributions));
        }

        filter.Normalize();
        var revenueTotals = AggregateRevenue(filter, revenueContributions);
        var costSummary = _ledger.Summarize(filter);
        var profitTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var marginTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in revenueTotals)
        {
            var currency = kvp.Key;
            var revenue = kvp.Value;
            costSummary.TotalsByCurrency.TryGetValue(currency, out var cost);
            var profit = decimal.Round(revenue - cost, 2, MidpointRounding.AwayFromZero);
            profitTotals[currency] = profit;

            if (revenue > 0)
            {
                marginTotals[currency] = decimal.Round(profit / revenue, 4, MidpointRounding.AwayFromZero);
            }
            else
            {
                marginTotals[currency] = 0m;
            }
        }

        foreach (var cost in costSummary.TotalsByCurrency)
        {
            if (profitTotals.ContainsKey(cost.Key))
            {
                continue;
            }

            var profit = decimal.Round(-cost.Value, 2, MidpointRounding.AwayFromZero);
            profitTotals[cost.Key] = profit;
            marginTotals[cost.Key] = 0m;
        }

        return new ProfitRollup(filter, revenueTotals, new Dictionary<string, decimal>(costSummary.TotalsByCurrency), profitTotals, marginTotals);
    }

    private static Dictionary<string, decimal> AggregateRevenue(
        CostScopeFilter filter,
        IEnumerable<RevenueContribution> contributions)
    {
        var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var contribution in contributions)
        {
            if (contribution is null)
            {
                throw new ArgumentException("Revenue contributions cannot contain null entries.", nameof(contributions));
            }

            if (!contribution.Scope.Matches(filter))
            {
                continue;
            }

            if (totals.TryGetValue(contribution.Currency, out var current))
            {
                totals[contribution.Currency] = current + contribution.Amount;
            }
            else
            {
                totals[contribution.Currency] = contribution.Amount;
            }
        }

        return totals;
    }
}
