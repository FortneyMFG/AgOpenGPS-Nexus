using System;
using System.Globalization;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a profitability hotspot or loss zone rendered in the analytics card.
/// </summary>
public sealed class ProfitHotspotViewModel
{
    private static readonly IBrush PositiveBrush = BrushCache.Create(Color.FromArgb(255, 46, 204, 113));
    private static readonly IBrush NegativeBrush = BrushCache.Create(Color.FromArgb(255, 231, 76, 60));

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfitHotspotViewModel"/> class.
    /// </summary>
    /// <param name="name">Name describing the hotspot.</param>
    /// <param name="currency">Currency used to display profit.</param>
    /// <param name="profitPerHectare">Profit or loss per hectare.</param>
    /// <param name="areaHectares">Area covered by the hotspot.</param>
    /// <param name="notes">Optional notes describing contributing factors.</param>
    public ProfitHotspotViewModel(string name, string currency, decimal profitPerHectare, double areaHectares, string notes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Hotspot name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency code is required.", nameof(currency));
        }

        Name = name.Trim();
        Currency = currency.Trim().ToUpperInvariant();
        ProfitPerHectare = profitPerHectare;
        AreaHectares = areaHectares;
        Notes = string.IsNullOrWhiteSpace(notes) ? "" : notes.Trim();
    }

    /// <summary>Gets the display name for the hotspot.</summary>
    public string Name { get; }

    /// <summary>Gets the ISO currency code.</summary>
    public string Currency { get; }

    /// <summary>Gets the profit (or loss) per hectare.</summary>
    public decimal ProfitPerHectare { get; }

    /// <summary>Gets the area covered by the hotspot.</summary>
    public double AreaHectares { get; }

    /// <summary>Gets notes describing contributing factors.</summary>
    public string Notes { get; }

    /// <summary>Gets the formatted area display.</summary>
    public string AreaDisplay => string.Format(CultureInfo.InvariantCulture, "{0:0.0} ha", AreaHectares);

    /// <summary>Gets the formatted profit per hectare display.</summary>
    public string ProfitDisplay => FormatCurrencyWithSign(Currency, ProfitPerHectare);

    /// <summary>Gets a brush used to render profit based on sign.</summary>
    public IBrush ProfitBrush => ProfitPerHectare >= 0 ? PositiveBrush : NegativeBrush;

    /// <summary>Gets a value indicating whether additional notes are available.</summary>
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

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
