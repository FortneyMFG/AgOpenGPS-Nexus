using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a per-currency profitability summary surfaced in the analytics card.
/// </summary>
public sealed class ProfitAnalyticsCurrencyViewModel
{
    private static readonly IBrush PositiveBrush = BrushCache.Create(Color.FromArgb(255, 34, 139, 34));
    private static readonly IBrush NegativeBrush = BrushCache.Create(Color.FromArgb(255, 192, 57, 43));

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfitAnalyticsCurrencyViewModel"/> class.
    /// </summary>
    /// <param name="currency">ISO currency code.</param>
    /// <param name="revenue">Total revenue recorded for the scope.</param>
    /// <param name="cost">Total cost recorded for the scope.</param>
    /// <param name="profit">Net profit (revenue minus cost).</param>
    public ProfitAnalyticsCurrencyViewModel(string currency, decimal revenue, decimal cost, decimal profit)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency code is required.", nameof(currency));
        }

        Currency = currency.Trim().ToUpperInvariant();
        Revenue = revenue;
        Cost = cost;
        Profit = profit;
        Margin = revenue == 0 ? 0 : profit / revenue;
    }

    /// <summary>Gets the ISO currency code.</summary>
    public string Currency { get; }

    /// <summary>Gets the total revenue recorded for the scope.</summary>
    public decimal Revenue { get; }

    /// <summary>Gets the total cost recorded for the scope.</summary>
    public decimal Cost { get; }

    /// <summary>Gets the net profit (revenue minus cost).</summary>
    public decimal Profit { get; }

    /// <summary>Gets the profit margin expressed as a fraction.</summary>
    public decimal Margin { get; }

    /// <summary>Gets the formatted revenue display string.</summary>
    public string RevenueDisplay => FormatCurrency(Currency, Revenue);

    /// <summary>Gets the formatted cost display string.</summary>
    public string CostDisplay => FormatCurrency(Currency, Cost);

    /// <summary>Gets the formatted profit display string with sign.</summary>
    public string ProfitDisplay => FormatCurrencyWithSign(Currency, Profit);

    /// <summary>Gets the formatted margin display.</summary>
    public string MarginDisplay => string.Format(CultureInfo.InvariantCulture, "{0:P1}", Margin);

    /// <summary>Gets a brush used to render the profit based on sign.</summary>
    public IBrush ProfitBrush => Profit >= 0 ? PositiveBrush : NegativeBrush;

    private static string FormatCurrency(string currency, decimal amount)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} {1:N2}", currency, amount);
    }

    private static string FormatCurrencyWithSign(string currency, decimal amount)
    {
        var formatted = string.Format(CultureInfo.InvariantCulture, "{0} {1:N2}", currency, Math.Abs(amount));
        if (amount > 0)
        {
            return "+" + formatted;
        }

        if (amount < 0)
        {
            return "−" + formatted;
        }

        return formatted;
    }
}

internal static class BrushCache
{
    public static IBrush Create(Color color) => new ImmutableSolidColorBrush(color);
}
