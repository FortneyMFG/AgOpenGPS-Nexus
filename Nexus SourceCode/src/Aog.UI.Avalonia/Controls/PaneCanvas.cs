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

    public static readonly StyledProperty<bool> ShowMinorGridProperty =
        AvaloniaProperty.Register<PaneCanvas, bool>(nameof(ShowMinorGrid));

    private static readonly IBrush GridBackgroundBrush = new ImmutableSolidColorBrush(Color.FromArgb(255, 16, 16, 16));
    private static readonly IBrush GridCellBrush = new ImmutableSolidColorBrush(Color.FromArgb(28, 255, 255, 255));
    private static readonly IBrush GridMajorCellBrush = new ImmutableSolidColorBrush(Color.FromArgb(45, 255, 255, 255));
    private static readonly Pen MinorGridPen = new(new ImmutableSolidColorBrush(Color.FromArgb(36, 255, 255, 255)), 1);
    private static readonly Pen MajorGridPen = new(new ImmutableSolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 2);
    private static readonly IBrush GridDotBrush = new ImmutableSolidColorBrush(Color.FromArgb(160, 255, 255, 255));
    private static readonly IBrush PaneFillBrush = new ImmutableSolidColorBrush(Color.FromArgb(232, 40, 40, 40));
    private static readonly Pen PaneBorderPen = new(new ImmutableSolidColorBrush(Color.FromArgb(196, 86, 86, 86)), 2);
    private static readonly IBrush PaneDividerBrush = new ImmutableSolidColorBrush(Color.FromArgb(120, 180, 180, 180));

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

    public bool ShowMinorGrid
    {
        get => GetValue(ShowMinorGridProperty);
        set => SetValue(ShowMinorGridProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Layout is not { } layout)
        {
            return;
        }

        DrawGrid(context, layout, ShowMinorGrid);
        if (LayoutResult is { } paneLayout)
        {
            DrawPanels(context, paneLayout);
        }
    }

    private static void DrawGrid(DrawingContext context, ShellGridLayout layout, bool showMinorGrid)
    {
        var cell = layout.CellPx;
        if (layout.Columns <= 0 || layout.Rows <= 0 || cell <= 0)
        {
            return;
        }

        var gutter = layout.GutterPx;
        var step = cell + gutter;
        var width = layout.Columns * cell + Math.Max(0, layout.Columns - 1) * gutter;
        var height = layout.Rows * cell + Math.Max(0, layout.Rows - 1) * gutter;

        var majorStep = Math.Max(1, layout.Columns / MajorGridColumns);
        var rowMajorStep = Math.Max(1, layout.Rows / MajorGridColumns);

        context.DrawRectangle(GridBackgroundBrush, null, new Rect(0, 0, width, height));

        if (showMinorGrid)
        {
            for (var col = 0; col < layout.Columns; col++)
            {
                var x = col * step;
                if (x >= width)
                {
                    continue;
                }

                var rectWidth = Math.Min(cell, width - x);
                if (rectWidth <= 0)
                {
                    continue;
                }

                var isMajorCol = col % majorStep == 0;
                for (var row = 0; row < layout.Rows; row++)
                {
                    var y = row * step;
                    if (y >= height)
                    {
                        continue;
                    }

                    var rectHeight = Math.Min(cell, height - y);
                    if (rectHeight <= 0)
                    {
                        continue;
                    }

                    var isMajorRow = row % rowMajorStep == 0;
                    var brush = isMajorCol || isMajorRow ? GridMajorCellBrush : GridCellBrush;
                    context.DrawRectangle(brush, null, new Rect(x, y, rectWidth, rectHeight));
                }
            }
        }

        for (var col = 0; col <= layout.Columns; col++)
        {
            var x = col * step;
            var isMajor = col % majorStep == 0;
            if (!isMajor && !showMinorGrid)
            {
                continue;
            }

            var pen = isMajor ? MajorGridPen : MinorGridPen;
            context.DrawLine(pen, new Point(x, 0), new Point(x, height));
        }

        for (var row = 0; row <= layout.Rows; row++)
        {
            var y = row * step;
            var isMajor = row % rowMajorStep == 0;
            if (!isMajor && !showMinorGrid)
            {
                continue;
            }

            var pen = isMajor ? MajorGridPen : MinorGridPen;
            context.DrawLine(pen, new Point(0, y), new Point(width, y));
        }

        if (showMinorGrid)
        {
            var dotRadius = Math.Max(1d, cell * 0.06d);
            for (var col = 0; col <= layout.Columns; col += majorStep)
            {
                var x = col * step;
                if (x < 0 || x > width)
                {
                    continue;
                }

                for (var row = 0; row <= layout.Rows; row += rowMajorStep)
                {
                    var y = row * step;
                    if (y < 0 || y > height)
                    {
                        continue;
                    }

                    context.DrawEllipse(GridDotBrush, null, new Point(x, y), dotRadius, dotRadius);
                }
            }
        }
    }

    private static void DrawPanels(DrawingContext context, PaneLayoutResult layout)
    {
        foreach (var pane in layout.Panels)
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
