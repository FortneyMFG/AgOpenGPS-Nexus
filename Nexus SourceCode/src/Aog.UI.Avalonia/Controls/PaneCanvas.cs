using System;
using Aog.UI.Avalonia.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Aog.UI.Avalonia.Controls;

public sealed class PaneCanvas : Control
{
    private const int MajorGridColumns = 10;

    public static readonly StyledProperty<ShellGridLayout?> LayoutProperty =
        AvaloniaProperty.Register<PaneCanvas, ShellGridLayout?>(nameof(Layout));

    public static readonly StyledProperty<PaneLayoutResult?> LayoutResultProperty =
        AvaloniaProperty.Register<PaneCanvas, PaneLayoutResult?>(nameof(LayoutResult));

    private static readonly Pen MinorGridPen = new(new ImmutableSolidColorBrush(Color.FromArgb(32, 255, 255, 255)), 1);
    private static readonly Pen MajorGridPen = new(new ImmutableSolidColorBrush(Color.FromArgb(128, 255, 255, 255)), 2);
    private static readonly IBrush PaneFillBrush = new ImmutableSolidColorBrush(Color.FromArgb(26, 15, 23, 42));
    private static readonly Pen PaneBorderPen = new(new ImmutableSolidColorBrush(Color.FromArgb(144, 15, 23, 42)), 1.5);
    private static readonly IBrush PaneDividerBrush = new ImmutableSolidColorBrush(Color.FromArgb(96, 255, 255, 255));

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
        if (LayoutResult is { } paneLayout)
        {
            DrawPanels(context, paneLayout);
        }
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

        var majorStep = Math.Max(1, layout.Columns / MajorGridColumns);
        var rowMajorStep = Math.Max(1, layout.Rows / MajorGridColumns);

        for (var col = 0; col <= layout.Columns; col++)
        {
            var x = col * cell;
            var pen = col % majorStep == 0 ? MajorGridPen : MinorGridPen;
            context.DrawLine(pen, new Point(x, 0), new Point(x, height));
        }

        for (var row = 0; row <= layout.Rows; row++)
        {
            var y = row * cell;
            var pen = row % rowMajorStep == 0 ? MajorGridPen : MinorGridPen;
            context.DrawLine(pen, new Point(0, y), new Point(width, y));
        }
    }

    private static void DrawPanels(DrawingContext context, PaneLayoutResult layout)
    {
        foreach (var pane in layout.Panes)
        {
            if (pane.Bounds.Width <= 0 || pane.Bounds.Height <= 0)
            {
                continue;
            }

            context.DrawRectangle(PaneFillBrush, PaneBorderPen, pane.Bounds);
        }

        foreach (var divider in layout.Dividers)
        {
            if (divider.Bounds.Width <= 0 || divider.Bounds.Height <= 0)
            {
                continue;
            }

            context.DrawRectangle(PaneDividerBrush, null, divider.Bounds);
        }
    }
}
