using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Aog.UI.Avalonia.Rendering;

/// <summary>
/// Inverts a boolean value for visibility bindings.
/// </summary>
public sealed class BooleanNegationConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool flag)
        {
            return !flag;
        }

        if (value is bool?)
        {
            var nullable = (bool?)value;
            return !(nullable ?? false);
        }

        return AvaloniaProperty.UnsetValue;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag ? !flag : AvaloniaProperty.UnsetValue;
    }
}
