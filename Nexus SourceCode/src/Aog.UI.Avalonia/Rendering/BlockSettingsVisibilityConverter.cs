using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Aog.UI.Avalonia.Rendering;

/// <summary>
/// Determines whether block settings affordances should be visible based on layout state.
/// </summary>
public sealed class BlockSettingsVisibilityConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Count < 2)
        {
            return AvaloniaProperty.UnsetValue;
        }

        var hasSettings = values[0] as bool? ?? false;
        var isLocked = values[1] as bool? ?? false;

        return hasSettings && !isLocked;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
