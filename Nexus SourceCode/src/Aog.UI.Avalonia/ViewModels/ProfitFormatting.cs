using System;
using System.Globalization;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Shared formatting helpers for profit analytics view-models.
/// </summary>
internal static class ProfitFormatting
{
    /// <summary>
    /// Formats a currency value using the supplied symbol and options.
    /// </summary>
    /// <param name="value">Currency amount to format.</param>
    /// <param name="currencySymbol">Symbol to prefix.</param>
    /// <param name="showSign">Whether to prefix a + sign for positive values.</param>
    /// <param name="format">Numeric format string applied to the absolute value.</param>
    /// <param name="suffix">Optional suffix appended to the formatted value.</param>
    public static string FormatCurrency(
        decimal value,
        string currencySymbol,
        bool showSign = false,
        string format = "N0",
        string? suffix = null)
    {
        if (currencySymbol is null)
        {
            throw new ArgumentNullException(nameof(currencySymbol));
        }

        var absolute = Math.Abs(value);
        var numericFormat = string.Format(CultureInfo.InvariantCulture, "{{0:{0}}}", format);
        var formatted = string.Format(CultureInfo.InvariantCulture, numericFormat, absolute);

        if (showSign)
        {
            formatted = value >= 0
                ? string.Concat('+', currencySymbol, formatted)
                : string.Concat('-', currencySymbol, formatted);
        }
        else if (value < 0)
        {
            formatted = string.Concat('-', currencySymbol, formatted);
        }
        else
        {
            formatted = string.Concat(currencySymbol, formatted);
        }

        if (!string.IsNullOrWhiteSpace(suffix))
        {
            formatted = string.Concat(formatted, suffix);
        }

        return formatted;
    }

    /// <summary>
    /// Formats an area value expressed in hectares.
    /// </summary>
    public static string FormatHectares(double hectares)
    {
        if (double.IsNaN(hectares) || double.IsInfinity(hectares))
        {
            throw new ArgumentOutOfRangeException(nameof(hectares));
        }

        return string.Format(CultureInfo.InvariantCulture, "{0:0.##} ha", hectares);
    }

    /// <summary>
    /// Formats a decimal ratio as a percentage or returns an em dash when undefined.
    /// </summary>
    public static string FormatPercentage(decimal numerator, decimal denominator)
    {
        if (denominator == 0)
        {
            return "—";
        }

        var ratio = numerator / denominator;
        return string.Format(CultureInfo.InvariantCulture, "{0:P1}", ratio);
    }
}
