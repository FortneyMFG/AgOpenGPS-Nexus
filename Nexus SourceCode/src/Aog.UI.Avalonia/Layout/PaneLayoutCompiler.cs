using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Layout;
using AvaloniaRect = global::Avalonia.Rect;

namespace Aog.UI.Avalonia.Layout;

public static class PaneLayoutCompiler
{
    public static PaneLayoutResult Compile(ShellGridLayout layout)
    {
        if (layout is null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        if (layout.Panels is { Count: > 0 })
        {
            return CompilePanels(layout);
        }

        var panels = new List<PanelVisual>();
        var dividers = new List<PanelDividerVisual>();

        if (layout.RootPane is null)
        {
            return new PaneLayoutResult(panels, dividers);
        }

        var rootRect = new TileSpec
        {
            Row = 0,
            Col = 0,
            RowSpan = Math.Max(1, layout.Rows),
            ColSpan = Math.Max(1, layout.Columns),
        };

        CompileNode(layout, layout.RootPane, rootRect, panels, dividers);
        return new PaneLayoutResult(panels, dividers);
    }

    private static void CompileNode(
        ShellGridLayout layout,
        PaneNode node,
        TileSpec bounds,
        List<PanelVisual> panels,
        List<PanelDividerVisual> dividers)
    {
        switch (node)
        {
            case LeafPane leaf:
                var rect = layout.ToPixelRect(bounds.Col, bounds.Row, bounds.ColSpan, bounds.RowSpan);
                panels.Add(new PanelVisual(leaf.Id, rect));
                break;
            case SplitPane split:
                CompileSplit(layout, split, bounds, panes, dividers);
                break;
        }
    }

    private static void CompileSplit(
        ShellGridLayout layout,
        SplitPane split,
        TileSpec bounds,
        List<PanelVisual> panes,
        List<PanelDividerVisual> dividers)
    {
        if (split.Children.Count == 0)
        {
            return;
        }

        var ratios = split.Ratios.Length == split.Children.Count
            ? split.Ratios
            : CreateUniformRatios(split.Children.Count);

        var total = split.Orientation == Orientation.Horizontal
            ? bounds.ColSpan
            : bounds.RowSpan;

        if (total <= 0)
        {
            total = 1;
        }

        var spans = AllocateSpans(total, ratios, split.Children.Count);

        var cursorRow = bounds.Row;
        var cursorCol = bounds.Col;

        for (var i = 0; i < split.Children.Count; i++)
        {
            var span = spans[i];
            var childBounds = new TileSpec
            {
                Row = cursorRow,
                Col = cursorCol,
                RowSpan = split.Orientation == Orientation.Horizontal ? bounds.RowSpan : span,
                ColSpan = split.Orientation == Orientation.Horizontal ? span : bounds.ColSpan,
            };

            CompileNode(layout, split.Children[i], childBounds, panes, dividers);

            if (split.Orientation == Orientation.Horizontal)
            {
                cursorCol += span;
                var rect = CreateVerticalDivider(layout, cursorCol, bounds.Row, bounds.RowSpan);
                dividers.Add(new PanelDividerVisual(PanelDividerOrientation.Vertical, rect, split.Id));
            }
            else
            {
                cursorRow += span;
                var rect = CreateHorizontalDivider(layout, bounds.Col, cursorRow, bounds.ColSpan);
                dividers.Add(new PanelDividerVisual(PanelDividerOrientation.Horizontal, rect, split.Id));
            }
        }

        if (dividers.Count > 0)
        {
            dividers.RemoveAt(dividers.Count - 1);
        }
    }

    private static PaneLayoutResult CompilePanels(ShellGridLayout layout)
    {
        var panels = new List<PanelVisual>();
        var dividers = new List<PanelDividerVisual>();

        foreach (var panel in layout.Panels)
        {
            if (panel is null)
            {
                continue;
            }

            var bounds = panel.ToPixelRect(layout);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                continue;
            }

            panels.Add(new PanelVisual(panel.Id, bounds));
            dividers.AddRange(CreatePanelDividers(layout, panel, bounds));
        }

        return new PaneLayoutResult(panels, dividers);
    }

    private static IEnumerable<PanelDividerVisual> CreatePanelDividers(
        ShellGridLayout layout,
        PanelSpec panel,
        AvaloniaRect bounds)
    {
        var thickness = Math.Max(2, (int)Math.Round(layout.GutterPx <= 0 ? layout.CellPx * 0.05 : layout.GutterPx));
        var verticalThickness = Math.Min(thickness, Math.Max(1, (int)Math.Round(bounds.Width)));
        var horizontalThickness = Math.Min(thickness, Math.Max(1, (int)Math.Round(bounds.Height)));
        var dividers = new List<PanelDividerVisual>
        {
            new(PanelDividerOrientation.Vertical, new AvaloniaRect(bounds.X, bounds.Y, verticalThickness, bounds.Height), panel.Id),
            new(PanelDividerOrientation.Vertical, new AvaloniaRect(bounds.Right - verticalThickness, bounds.Y, verticalThickness, bounds.Height), panel.Id),
            new(PanelDividerOrientation.Horizontal, new AvaloniaRect(bounds.X, bounds.Y, bounds.Width, horizontalThickness), panel.Id),
            new(PanelDividerOrientation.Horizontal, new AvaloniaRect(bounds.X, bounds.Bottom - horizontalThickness, bounds.Width, horizontalThickness), panel.Id),
        };

        return dividers;
    }

    private static int[] AllocateSpans(int total, double[] ratios, int count)
    {
        var spans = new int[count];
        var remaining = total;

        for (var i = 0; i < count; i++)
        {
            var ratio = ratios[i];
            var span = (int)Math.Max(1, Math.Round(total * ratio));
            if (span > remaining)
            {
                span = remaining;
            }

            spans[i] = span;
            remaining -= span;
        }

        if (remaining > 0)
        {
            spans[^1] += remaining;
        }

        return spans;
    }

    private static double[] CreateUniformRatios(int count)
    {
        var ratios = new double[count];
        for (var i = 0; i < count; i++)
        {
            ratios[i] = 1.0 / count;
        }

        return ratios;
    }

    private static AvaloniaRect CreateVerticalDivider(ShellGridLayout layout, int column, int row, int rowSpan)
    {
        var cell = layout.CellPx;
        var gutter = layout.GutterPx;
        var x = column * (cell + gutter) - gutter / 2;
        var y = row * (cell + gutter);
        var height = rowSpan * cell + Math.Max(0, rowSpan - 1) * gutter;
        return new AvaloniaRect(x, y, gutter, height);
    }

    private static AvaloniaRect CreateHorizontalDivider(ShellGridLayout layout, int column, int row, int colSpan)
    {
        var cell = layout.CellPx;
        var gutter = layout.GutterPx;
        var x = column * (cell + gutter);
        var y = row * (cell + gutter) - gutter / 2;
        var width = colSpan * cell + Math.Max(0, colSpan - 1) * gutter;
        return new AvaloniaRect(x, y, width, gutter);
    }
}
