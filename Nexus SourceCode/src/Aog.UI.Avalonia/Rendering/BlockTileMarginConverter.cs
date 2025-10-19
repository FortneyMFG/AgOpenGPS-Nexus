using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Aog.UI.Avalonia.Settings;

namespace Aog.UI.Avalonia.Rendering;

/// <summary>
/// Produces margin thickness values for block tiles based on sidebar spacing.
/// </summary>
public sealed class BlockTileMarginConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Count < 1)
        {
            return AvaloniaProperty.UnsetValue;
        }

        var settings = values[^1] as SidebarLayoutSettings;
        if (settings is null)
        {
            return new Thickness(4);
        }

        var spacing = Math.Max(0, settings.Spacing);
        var margin = spacing / 2;
        return new Thickness(margin);
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
