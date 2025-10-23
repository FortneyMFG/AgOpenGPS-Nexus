using System;
using Aog.UI.Avalonia.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Aog.UI.Avalonia.Controls;

public sealed class PaneCanvas : Control
{
    public static readonly StyledProperty<ShellGridLayout?> LayoutProperty =
        AvaloniaProperty.Register<PaneCanvas, ShellGridLayout?>(nameof(Layout));

    public static readonly StyledProperty<PaneLayoutResult?> LayoutResultProperty =
        AvaloniaProperty.Register<PaneCanvas, PaneLayoutResult?>(nameof(LayoutResult));

    public ShellGridLayout? Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public PaneLayoutResult? LayoutResult
    {
        get => GetValue(LayoutResultProperty);
        set => SetValue(LayoutResultProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Layout is not { } layout)
        {
            return;
        }

        DrawGrid(context, layout);
    }

    private static void DrawGrid(DrawingContext context, ShellGridLayout layout)
    {
        var cell = layout.CellPx;
        if (layout.Columns <= 0 || layout.Rows <= 0 || cell <= 0)
        {
            return;
        }

        var width = cell * layout.Columns;
        var height = cell * layout.Rows;

        var minorPen = new Pen(new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)), 1);
        var majorPen = new Pen(new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)), 2);
        var minorPerMajor = Math.Max(1, layout.Columns / 10);

        for (var col = 0; col <= layout.Columns; col++)
        {
            var x = col * cell;
            var pen = col % minorPerMajor == 0 ? majorPen : minorPen;
            context.DrawLine(pen, new Point(x, 0), new Point(x, height));
        }

        for (var row = 0; row <= layout.Rows; row++)
        {
            var y = row * cell;
            var pen = row % minorPerMajor == 0 ? majorPen : minorPen;
            context.DrawLine(pen, new Point(0, y), new Point(width, y));
        }
    }
}
