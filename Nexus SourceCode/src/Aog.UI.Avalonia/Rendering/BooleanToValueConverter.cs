using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Aog.UI.Avalonia.Rendering;

public sealed class BooleanToValueConverter : IValueConverter
{
    public object? TrueValue { get; set; } = AvaloniaProperty.UnsetValue;
    public object? FalseValue { get; set; } = AvaloniaProperty.UnsetValue;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is bool b && b;
        return flag ? TrueValue : FalseValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
