using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Aog.UI.Avalonia.Settings;

namespace Aog.UI.Avalonia.Rendering;

/// <summary>
/// Converts sidebar layout settings into a concrete dimension for container sizing.
/// </summary>
public sealed class SidebarDimensionConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SidebarLayoutSettings settings || parameter is not string dimension)
        {
            return AvaloniaProperty.UnsetValue;
        }

        return dimension.Equals("Height", StringComparison.OrdinalIgnoreCase)
            ? Calculate(settings.HeightMode, settings.BlockRows, settings.BlockSize, settings.Spacing)
            : Calculate(settings.WidthMode, settings.BlockColumns, settings.BlockSize, settings.Spacing);
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static double Calculate(LayoutDimensionMode mode, double blocks, double blockSize, double spacing)
    {
        if (mode == LayoutDimensionMode.Dynamic || blocks <= 0)
        {
            return double.NaN;
        }

        var span = Math.Max(1d, blocks);
        var spacingSegments = Math.Max(0d, span - 1d);
        return (blockSize * span) + (spacingSegments * spacing);
    }
}
