using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Surfaces profitability analytics and heatmap metadata in the UI shell.
/// </summary>
public sealed class ProfitAnalyticsViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfitAnalyticsViewModel"/> class.
    /// </summary>
    /// <param name="scopeSummary">Scope describing the active profit layer.</param>
    /// <param name="layerId">Layer identifier rendered on the map.</param>
    /// <param name="lastUpdated">Timestamp of the last profit calculation.</param>
    /// <param name="currencySymbol">Currency symbol used for formatting.</param>
    /// <param name="breakdowns">Breakdown entries describing profit hot spots.</param>
    /// <param name="legend">Legend bands explaining the colour ramp.</param>
    /// <param name="alerts">Optional alerts surfaced to the operator.</param>
    public ProfitAnalyticsViewModel(
        string scopeSummary,
        string layerId,
        DateTimeOffset lastUpdated,
        string currencySymbol,
        IEnumerable<ProfitBreakdownViewModel> breakdowns,
        IEnumerable<ProfitLegendBandViewModel> legend,
        IEnumerable<string>? alerts = null)
    {
        if (string.IsNullOrWhiteSpace(scopeSummary))
        {
            throw new ArgumentException("Scope summary is required.", nameof(scopeSummary));
        }

        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        if (string.IsNullOrWhiteSpace(currencySymbol))
        {
            throw new ArgumentException("Currency symbol is required.", nameof(currencySymbol));
        }

        ArgumentNullException.ThrowIfNull(breakdowns);
        ArgumentNullException.ThrowIfNull(legend);

        ScopeSummary = scopeSummary.Trim();
        LayerId = layerId.Trim();
        LastUpdatedDisplay = lastUpdated.ToString("dd MMM yyyy • HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture);
        CurrencySymbol = currencySymbol.Trim();

        Breakdowns = new ReadOnlyCollection<ProfitBreakdownViewModel>(breakdowns.ToArray());
        Legend = new ReadOnlyCollection<ProfitLegendBandViewModel>(legend.ToArray());
        Alerts = new ReadOnlyCollection<string>((alerts ?? Array.Empty<string>()).ToArray());

        var totalArea = Breakdowns.Sum(b => b.AreaHectares);
        var totalRevenue = Breakdowns.Sum(b => b.Revenue);
        var totalCost = Breakdowns.Sum(b => b.Cost);
        var totalProfit = totalRevenue - totalCost;
        var averageProfitPerHectare = totalArea <= 0
            ? 0
            : totalProfit / (decimal)totalArea;

        TotalRevenueDisplay = ProfitFormatting.FormatCurrency(totalRevenue, CurrencySymbol, format: "N0");
        TotalCostDisplay = ProfitFormatting.FormatCurrency(totalCost, CurrencySymbol, format: "N0");
        NetProfitDisplay = ProfitFormatting.FormatCurrency(totalProfit, CurrencySymbol, showSign: true, format: "N0");
        AverageProfitPerHectareDisplay = ProfitFormatting.FormatCurrency(
            averageProfitPerHectare,
            CurrencySymbol,
            showSign: true,
            format: "N0",
            suffix: "/ha");

        MarginDisplay = ProfitFormatting.FormatPercentage(totalProfit, totalRevenue);
        PositiveShareDisplay = FormatPositiveShare(Breakdowns);
    }

    /// <summary>Gets a summary describing the active scope.</summary>
    public string ScopeSummary { get; }

    /// <summary>Gets the layer identifier rendered on the map.</summary>
    public string LayerId { get; }

    /// <summary>Gets a formatted timestamp describing when the layer was last updated.</summary>
    public string LastUpdatedDisplay { get; }

    /// <summary>Gets the currency symbol used for formatting.</summary>
    public string CurrencySymbol { get; }

    /// <summary>Gets the formatted total revenue.</summary>
    public string TotalRevenueDisplay { get; }

    /// <summary>Gets the formatted total cost.</summary>
    public string TotalCostDisplay { get; }

    /// <summary>Gets the formatted net profit.</summary>
    public string NetProfitDisplay { get; }

    /// <summary>Gets the formatted average profit per hectare.</summary>
    public string AverageProfitPerHectareDisplay { get; }

    /// <summary>Gets the formatted season margin.</summary>
    public string MarginDisplay { get; }

    /// <summary>Gets the share of hectares that are profitable.</summary>
    public string PositiveShareDisplay { get; }

    /// <summary>Gets the breakdown entries describing profitability hot spots.</summary>
    public IReadOnlyList<ProfitBreakdownViewModel> Breakdowns { get; }

    /// <summary>Gets the legend bands describing the colour ramp.</summary>
    public IReadOnlyList<ProfitLegendBandViewModel> Legend { get; }

    /// <summary>Gets the operator alerts surfaced for the profit layer.</summary>
    public IReadOnlyList<string> Alerts { get; }

    /// <summary>Gets a value indicating whether any alerts are present.</summary>
    public bool HasAlerts => Alerts.Count > 0;

    /// <summary>
    /// Creates a sample view-model showcasing profit analytics.
    /// </summary>
    public static ProfitAnalyticsViewModel CreateSample()
    {
        const string currency = "$";

        var breakdowns = new[]
        {
            new ProfitBreakdownViewModel("North headland", 6.4, 48230m, 24780m, currency),
            new ProfitBreakdownViewModel("Mid slope", 12.1, 91860m, 71240m, currency),
            new ProfitBreakdownViewModel("Center plateau", 8.7, 74210m, 39820m, currency),
            new ProfitBreakdownViewModel("Waterway buffer", 3.3, 1840m, 6120m, currency),
        };

        var legend = new[]
        {
            new ProfitLegendBandViewModel("Exceptional", "> +$350/ha", Color.FromArgb(255, 16, 110, 85)),
            new ProfitLegendBandViewModel("Healthy", "+$150 – +$350/ha", Color.FromArgb(255, 38, 143, 99)),
            new ProfitLegendBandViewModel("Breakeven", "-$25 – +$150/ha", Color.FromArgb(255, 223, 194, 102)),
            new ProfitLegendBandViewModel("Thin margin", "-$150 – -$25/ha", Color.FromArgb(255, 205, 123, 88)),
            new ProfitLegendBandViewModel("Loss", "< -$150/ha", Color.FromArgb(255, 178, 62, 94)),
        };

        var alerts = new[]
        {
            "3.2 ha fell below the configured profit threshold",
            "Ledger pending: confirm diesel transfer for session 2025-04-11-AM",
        };

        return new ProfitAnalyticsViewModel(
            scopeSummary: "Field 18 • Harvest 2025",
            layerId: "layer:profit.net.harvest-2025",
            lastUpdated: new DateTimeOffset(2025, 4, 11, 14, 32, 0, TimeSpan.Zero),
            currencySymbol: currency,
            breakdowns: breakdowns,
            legend: legend,
            alerts: alerts);
    }

    private static string FormatPositiveShare(IEnumerable<ProfitBreakdownViewModel> breakdowns)
    {
        var totalArea = breakdowns.Sum(b => b.AreaHectares);
        if (totalArea <= 0)
        {
            return "—";
        }

        var positiveArea = breakdowns
            .Where(b => !b.IsNegative)
            .Sum(b => b.AreaHectares);

        var share = (decimal)(positiveArea / totalArea);
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:P0} profitable hectares", share);
    }
}
