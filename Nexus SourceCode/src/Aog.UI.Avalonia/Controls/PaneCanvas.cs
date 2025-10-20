using System.Collections.Generic;
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

        if (Layout is not { } layout || LayoutResult is not { } result)
        {
            return;
        }

        DrawGrid(context, layout);
        DrawPanes(context, result.Panes);
        DrawDividers(context, result.Dividers);
    }

    private static void DrawGrid(DrawingContext context, ShellGridLayout layout)
    {
        var cell = layout.CellPx;
        var gutter = layout.GutterPx;
        if (layout.Columns <= 0 || layout.Rows <= 0)
        {
            return;
        }

        var width = layout.ToPixelRect(0, 0, layout.Columns, 1).Width;
        var height = layout.ToPixelRect(0, 0, 1, layout.Rows).Height;

        var pen = new Pen(new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)), 1);

        for (var col = 0; col <= layout.Columns; col++)
        {
            var x = col * (cell + gutter) - gutter / 2;
            context.DrawLine(pen, new Point(x, 0), new Point(x, height));
        }

        for (var row = 0; row <= layout.Rows; row++)
        {
            var y = row * (cell + gutter) - gutter / 2;
            context.DrawLine(pen, new Point(0, y), new Point(width, y));
        }
    }

    private static void DrawPanes(DrawingContext context, IReadOnlyList<PaneVisual> panes)
    {
        var fill = new SolidColorBrush(Color.FromArgb(24, 0, 128, 255));
        var stroke = new Pen(new SolidColorBrush(Color.FromArgb(128, 0, 128, 255)), 1);

        foreach (var pane in panes)
        {
            context.FillRectangle(fill, pane.Bounds);
            context.DrawRectangle(stroke, pane.Bounds);
        }
    }

    private static void DrawDividers(DrawingContext context, IReadOnlyList<PaneDividerVisual> dividers)
    {
        var stroke = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 2);
        foreach (var divider in dividers)
        {
            context.DrawRectangle(stroke, divider.Bounds);
        }
    }
}
