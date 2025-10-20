using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Aog.UI.Avalonia.Settings;

namespace Aog.UI.Avalonia.Rendering;

public sealed class SpacingToThicknessConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var spacing = value switch
        {
            double d => d,
            float f => f,
            SidebarLayoutSettings settings => settings.Spacing,
            _ => 0d
        };

        var normalized = Math.Max(0d, spacing) / 2d;
        return new Thickness(normalized);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
