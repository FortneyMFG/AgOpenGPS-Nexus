using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Aog.UI.Avalonia.Rendering;

/// <summary>
/// Compares a string value to a configured match and returns a boolean result.
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    /// <summary>Gets or sets the string to compare against.</summary>
    public string? Match { get; set; }

    /// <summary>Gets or sets a value indicating whether the result should be negated.</summary>
    public bool Negate { get; set; }

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var candidate = value as string;
        var match = parameter as string ?? Match;
        var equals = string.Equals(candidate, match, StringComparison.OrdinalIgnoreCase);
        return Negate ? !equals : equals;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}
