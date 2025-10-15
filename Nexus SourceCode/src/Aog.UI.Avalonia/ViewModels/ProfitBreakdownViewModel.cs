using System;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a profit breakdown entry for a particular zone or slice.
/// </summary>
public sealed class ProfitBreakdownViewModel
{
    private readonly string _currencySymbol;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfitBreakdownViewModel"/> class.
    /// </summary>
    /// <param name="zoneName">Name of the zone represented by the entry.</param>
    /// <param name="areaHectares">Area of the zone in hectares.</param>
    /// <param name="revenue">Revenue attributed to the zone.</param>
    /// <param name="cost">Costs attributed to the zone.</param>
    /// <param name="currencySymbol">Currency symbol used for formatting.</param>
    public ProfitBreakdownViewModel(
        string zoneName,
        double areaHectares,
        decimal revenue,
        decimal cost,
        string currencySymbol)
    {
        if (string.IsNullOrWhiteSpace(zoneName))
        {
            throw new ArgumentException("Zone name is required.", nameof(zoneName));
        }

        if (double.IsNaN(areaHectares) || double.IsInfinity(areaHectares) || areaHectares < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(areaHectares));
        }

        ZoneName = zoneName.Trim();
        AreaHectares = areaHectares;
        Revenue = revenue;
        Cost = cost;
        _currencySymbol = currencySymbol ?? throw new ArgumentNullException(nameof(currencySymbol));

        AreaDisplay = ProfitFormatting.FormatHectares(areaHectares);
        RevenueDisplay = ProfitFormatting.FormatCurrency(revenue, _currencySymbol);
        CostDisplay = ProfitFormatting.FormatCurrency(cost, _currencySymbol);
        ProfitDisplay = ProfitFormatting.FormatCurrency(Profit, _currencySymbol, showSign: true);
        ProfitPerHectareDisplay = ProfitFormatting.FormatCurrency(
            ProfitPerHectare,
            _currencySymbol,
            showSign: true,
            suffix: "/ha");
        MarginDisplay = ProfitFormatting.FormatPercentage(Profit, Revenue);
        ProfitBrush = new SolidColorBrush(Profit >= 0
            ? Color.FromArgb(255, 34, 139, 99)
            : Color.FromArgb(255, 186, 54, 89));
    }

    /// <summary>Gets the zone name.</summary>
    public string ZoneName { get; }

    /// <summary>Gets the zone area in hectares.</summary>
    public double AreaHectares { get; }

    /// <summary>Gets the formatted area string.</summary>
    public string AreaDisplay { get; }

    /// <summary>Gets the revenue attributed to the zone.</summary>
    public decimal Revenue { get; }

    /// <summary>Gets the formatted revenue string.</summary>
    public string RevenueDisplay { get; }

    /// <summary>Gets the costs attributed to the zone.</summary>
    public decimal Cost { get; }

    /// <summary>Gets the formatted cost string.</summary>
    public string CostDisplay { get; }

    /// <summary>Gets the calculated profit (revenue minus cost).</summary>
    public decimal Profit => Revenue - Cost;

    /// <summary>Gets the formatted profit string including sign.</summary>
    public string ProfitDisplay { get; }

    /// <summary>Gets the calculated profit per hectare.</summary>
    public decimal ProfitPerHectare => AreaHectares <= 0
        ? 0
        : Profit / (decimal)AreaHectares;

    /// <summary>Gets the formatted profit per hectare string.</summary>
    public string ProfitPerHectareDisplay { get; }

    /// <summary>Gets the margin display derived from revenue vs. cost.</summary>
    public string MarginDisplay { get; }

    /// <summary>Gets a value indicating whether the profit is negative.</summary>
    public bool IsNegative => Profit < 0;

    /// <summary>Gets the brush used to accentuate the profit value.</summary>
    public IBrush ProfitBrush { get; }
}
