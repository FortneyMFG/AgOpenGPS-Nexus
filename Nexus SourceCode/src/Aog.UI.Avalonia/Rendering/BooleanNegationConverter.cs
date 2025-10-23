using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Aog.UI.Avalonia.Rendering;

/// <summary>
/// Inverts boolean values for binding scenarios.
/// </summary>
public sealed class BooleanNegationConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag ? !flag : AvaloniaProperty.UnsetValue;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool flag)
        {
            return !flag;
        }

        throw new NotSupportedException("BooleanNegationConverter only supports boolean values.");
    }
}
