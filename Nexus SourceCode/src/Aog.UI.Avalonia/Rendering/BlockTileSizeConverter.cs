using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.ViewModels.Shell;

namespace Aog.UI.Avalonia.Rendering;

/// <summary>
/// Converts block tile sizing metadata into pixel dimensions.
/// </summary>
public sealed class BlockTileSizeConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Count < 2)
        {
            return AvaloniaProperty.UnsetValue;
        }

        if (values[0] is not BlockItemViewModel item || values[1] is not SidebarLayoutSettings settings)
        {
            return AvaloniaProperty.UnsetValue;
        }

        var dimension = parameter as string ?? "Width";
        var span = dimension.Equals("Height", StringComparison.OrdinalIgnoreCase)
            ? item.HeightUnits
            : item.WidthUnits;

        if (span <= 0)
        {
            return 0d;
        }

        var blockSize = settings.BlockSize;
        var spacing = settings.Spacing;
        var total = (blockSize * span) + (Math.Max(0, span - 1) * spacing);
        return total;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
