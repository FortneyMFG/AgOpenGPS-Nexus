using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Aog.UI.Avalonia.Rendering;

/// <summary>
/// Converts a block icon key into a bitmap sourced from the Avalonia asset pipeline.
/// </summary>
public sealed class IconKeyToBitmapConverter : IValueConverter
{
    public static IconKeyToBitmapConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var normalized = key.Trim().Replace('\\', '/').ToLowerInvariant();
        var uri = new Uri($"avares://Aog.UI.Avalonia/Resources/Icons/{normalized}.png");
        if (!AssetLoader.Exists(uri))
        {
            return null;
        }

        using var stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Converts a boolean lock state to the corresponding lock or unlock bitmap.
/// </summary>
public sealed class LockStateToBitmapConverter : IValueConverter
{
    public static LockStateToBitmapConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is bool locked && !locked ? "unlock" : "lock";
        return IconKeyToBitmapConverter.Instance.Convert(key, targetType, parameter, culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Converts a boolean lock state into the tooltip text describing the next action.
/// </summary>
public sealed class LockStateToTextConverter : IValueConverter
{
    public static LockStateToTextConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool locked && !locked
            ? "Lock layout editing"
            : "Unlock layout editing";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
