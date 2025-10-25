using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Aog.UI.Avalonia.Rendering;

/// <summary>
/// Converts string values to a boolean indicating whether content should be visible.
/// </summary>
public sealed class StringHasValueConverter : IValueConverter
{
    public static StringHasValueConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text && !string.IsNullOrWhiteSpace(text);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Converts string values to a boolean indicating when no content is present.
/// </summary>
public sealed class StringIsNullOrWhiteSpaceConverter : IValueConverter
{
    public static StringIsNullOrWhiteSpaceConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not string text || string.IsNullOrWhiteSpace(text);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
