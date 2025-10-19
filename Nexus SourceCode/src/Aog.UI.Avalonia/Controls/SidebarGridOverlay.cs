using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Aog.UI.Avalonia.Settings;

namespace Aog.UI.Avalonia.Controls;

/// <summary>
/// Renders a lightweight grid overlay to visualize available block slots while editing.
/// </summary>
public sealed class SidebarGridOverlay : Control
{
    /// <summary>
    /// Identifies the <see cref="LayoutSettings"/> styled property.
    /// </summary>
    public static readonly StyledProperty<SidebarLayoutSettings?> LayoutSettingsProperty =
        AvaloniaProperty.Register<SidebarGridOverlay, SidebarLayoutSettings?>(nameof(LayoutSettings));

    private static readonly ImmutableSolidColorBrush BackgroundBrush =
        new(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));

    private static readonly Pen GridPen = new(
        new ImmutableSolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
        thickness: 1,
        lineCap: PenLineCap.Flat,
        lineJoin: PenLineJoin.Miter,
        miterLimit: 10,
        dashStyle: new DashStyle(new[] { 4d, 4d }, 0));

    static SidebarGridOverlay()
    {
        LayoutSettingsProperty.Changed.AddClassHandler<SidebarGridOverlay>((overlay, _) => overlay.InvalidateVisual());
    }

    /// <summary>Gets or sets the layout settings that control block sizing.</summary>
    public SidebarLayoutSettings? LayoutSettings
    {
        get => GetValue(LayoutSettingsProperty);
        set => SetValue(LayoutSettingsProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!IsVisible)
        {
            return;
        }

        var settings = LayoutSettings;
        if (settings is null)
        {
            return;
        }

        var blockSize = settings.BlockSize;
        if (blockSize <= 0)
        {
            return;
        }

        var spacing = Math.Max(0, settings.Spacing);
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        context.DrawRectangle(BackgroundBrush, null, bounds);

        var (columnCount, cellWidth) = CalculateSlots(
            bounds.Width,
            blockSize,
            spacing,
            settings.WidthMode,
            settings.BlockColumns);

        var (rowCount, cellHeight) = CalculateSlots(
            bounds.Height,
            blockSize,
            spacing,
            settings.HeightMode,
            settings.BlockRows);

        if (columnCount <= 0 || rowCount <= 0 || cellWidth <= 0 || cellHeight <= 0)
        {
            return;
        }

        var totalWidth = (columnCount * cellWidth) + Math.Max(0, columnCount - 1) * spacing;
        var totalHeight = (rowCount * cellHeight) + Math.Max(0, rowCount - 1) * spacing;

        var startX = Math.Max(0, (bounds.Width - totalWidth) / 2d);
        var startY = Math.Max(0, (bounds.Height - totalHeight) / 2d);

        for (var column = 0; column < columnCount; column++)
        {
            var x = startX + column * (cellWidth + spacing);
            if (x >= bounds.Width)
            {
                break;
            }

            var width = Math.Min(cellWidth, bounds.Width - x);
            if (width <= 1)
            {
                continue;
            }

            for (var row = 0; row < rowCount; row++)
            {
                var y = startY + row * (cellHeight + spacing);
                if (y >= bounds.Height)
                {
                    break;
                }

                var height = Math.Min(cellHeight, bounds.Height - y);
                if (height <= 1)
                {
                    continue;
                }

                var rect = new Rect(x, y, width, height);
                context.DrawRectangle(null, GridPen, rect);
            }
        }
    }

    private static (int Count, double Size) CalculateSlots(
        double available,
        double baseSize,
        double spacing,
        LayoutDimensionMode mode,
        double configuredUnits)
    {
        if (baseSize <= 0)
        {
            return (0, 0);
        }

        spacing = Math.Max(0, spacing);

        if (mode == LayoutDimensionMode.Fixed && configuredUnits > 0)
        {
            var units = Math.Max(configuredUnits, 0.25d);
            var count = Math.Max(1, (int)Math.Ceiling(units));
            var slot = (baseSize * units) / count;
            return (count, Math.Max(2d, slot));
        }

        var step = baseSize + spacing;
        if (step <= double.Epsilon)
        {
            return (0, 0);
        }

        var countDynamic = Math.Max(1, (int)Math.Floor((available + spacing) / step));
        var totalSpacing = Math.Max(0, countDynamic - 1) * spacing;
        var usable = Math.Max(0, available - totalSpacing);
        var slotDynamic = countDynamic > 0 ? usable / countDynamic : 0;
        if (slotDynamic <= 0)
        {
            slotDynamic = available > 0 ? Math.Min(baseSize, available / countDynamic) : 0;
        }

        slotDynamic = Math.Min(baseSize, slotDynamic);
        return (countDynamic, Math.Max(2d, slotDynamic));
    }
}
