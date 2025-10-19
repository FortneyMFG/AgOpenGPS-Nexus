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
        var step = blockSize + spacing;
        if (step <= 0.01)
        {
            return;
        }

        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        context.DrawRectangle(BackgroundBrush, null, bounds);

        var offset = spacing / 2d;
        var maxX = bounds.Width - offset;
        var maxY = bounds.Height - offset;
        if (maxX <= offset || maxY <= offset)
        {
            return;
        }

        var minCell = Math.Max(2, blockSize / 2d);

        for (var x = offset; x <= maxX - minCell; x += step)
        {
            var cellWidth = Math.Min(blockSize, maxX - x);
            if (cellWidth < minCell)
            {
                continue;
            }

            for (var y = offset; y <= maxY - minCell; y += step)
            {
                var cellHeight = Math.Min(blockSize, maxY - y);
                if (cellHeight < minCell)
                {
                    continue;
                }

                var rect = new Rect(x, y, cellWidth, cellHeight);
                context.DrawRectangle(null, GridPen, rect);
            }
        }
    }
}
