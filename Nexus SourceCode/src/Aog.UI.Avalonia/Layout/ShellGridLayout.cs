using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Layout;

namespace Aog.UI.Avalonia.Layout;

public sealed class ShellGridLayout
{
    public double CellPx { get; set; } = 56;

    public double GutterPx { get; set; } = 8;

    public int Columns { get; set; } = 1;

    public int Rows { get; set; } = 1;

    public PaneNode? RootPane { get; set; }
        = null;

    public List<TileSpec> Tiles { get; set; } = new();

    public List<PanelSpec> Panels { get; set; } = new();

    public List<FloatingPanelSpec> FloatingPanels { get; set; } = new();

    public List<FloatingBlockSpec> FloatingBlocks { get; set; } = new();

    public Rect ToPixelRect(int column, int row, int columnSpan, int rowSpan)
    {
        if (column < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        if (row < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        if (columnSpan <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnSpan));
        }

        if (rowSpan <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowSpan));
        }

        var columns = Math.Max(1, Columns);
        var rows = Math.Max(1, Rows);
        var normalizedColumn = Math.Clamp(column, 0, Math.Max(0, columns - columnSpan));
        var normalizedRow = Math.Clamp(row, 0, Math.Max(0, rows - rowSpan));

        var cell = CellPx;
        var gutter = GutterPx;
        var step = cell + gutter;
        var x = normalizedColumn * step;
        var rowFromTop = rows - normalizedRow - rowSpan;
        if (rowFromTop < 0)
        {
            rowFromTop = 0;
        }

        var y = rowFromTop * step;
        var width = columnSpan * cell + Math.Max(0, columnSpan - 1) * gutter;
        var height = rowSpan * cell + Math.Max(0, rowSpan - 1) * gutter;
        return new Rect(x, y, width, height);
    }

    public Rect ToPixelRectFromEdges(int left, int bottom, int right, int top)
    {
        var spanColumns = Math.Max(0, right - left);
        var spanRows = Math.Max(0, top - bottom);
        if (spanColumns == 0 || spanRows == 0)
        {
            return default;
        }

        return ToPixelRect(left, bottom, spanColumns, spanRows);
    }

    public (int col, int row) SnapToGrid(Point pixel, int colSpan, int rowSpan)
    {
        var columns = Math.Max(1, Columns);
        var rows = Math.Max(1, Rows);
        var cell = CellPx;
        if (cell <= 0)
        {
            return (0, 0);
        }

        var gutter = GutterPx;
        var step = cell + gutter;
        var rawCol = (int)Math.Round(pixel.X / step, MidpointRounding.AwayFromZero);
        var rawRowFromTop = (int)Math.Round(pixel.Y / step, MidpointRounding.AwayFromZero);
        var col = Math.Clamp(rawCol, 0, Math.Max(0, columns - colSpan));
        var row = rows - rawRowFromTop - rowSpan;
        row = Math.Clamp(row, 0, Math.Max(0, rows - rowSpan));
        return (col, row);
    }
}

public abstract record PaneNode
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
}

public sealed record SplitPane : PaneNode
{
    public Orientation Orientation { get; init; } = Orientation.Horizontal;

    public double[] Ratios { get; init; } = new[] { 1.0 };

    public List<PaneNode> Children { get; init; } = new();
}

public sealed record LeafPane : PaneNode
{
    public string Kind { get; init; } = string.Empty;

    public string PluginId { get; init; } = string.Empty;
}

public enum RelativeAnchor
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}

public sealed record TileSpec
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public int Row { get; set; }
        = 0;

    public int Col { get; set; }
        = 0;

    public int RowSpan { get; set; }
        = 1;

    public int ColSpan { get; set; }
        = 1;

    public bool PaneAttached { get; set; }
        = false;

    public string? PaneId { get; set; }
        = null;

    public RelativeAnchor Anchor { get; set; }
        = RelativeAnchor.TopCenter;

    public (int dx, int dy) Offset { get; set; }
        = (0, 0);
}

public sealed class PanelSpec
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public int Left { get; set; }
        = 0;

    public int Bottom { get; set; }
        = 0;

    public int Right { get; set; }
        = 1;

    public int Top { get; set; }
        = 1;

    public bool LeftUsesGridSize { get; set; }
        = false;

    public bool BottomUsesGridSize { get; set; }
        = false;

    public bool RightUsesGridSize { get; set; }
        = false;

    public bool TopUsesGridSize { get; set; }
        = false;

    public RelativeAnchor Anchor { get; set; }
        = RelativeAnchor.Center;

    public (int dx, int dy) Offset { get; set; }
        = (0, 0);

    public Rect ToPixelRect(ShellGridLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var columns = Math.Max(1, layout.Columns);
        var rows = Math.Max(1, layout.Rows);
        var left = ResolveHorizontalEdge(Left, LeftUsesGridSize, columns);
        var right = ResolveHorizontalEdge(Right, RightUsesGridSize, columns);
        var bottom = ResolveVerticalEdge(Bottom, BottomUsesGridSize, rows);
        var top = ResolveVerticalEdge(Top, TopUsesGridSize, rows);

        if (right <= left || top <= bottom)
        {
            return default;
        }

        var baseCol = Math.Clamp(left, 0, Math.Max(0, columns - 1));
        var baseRow = Math.Clamp(bottom, 0, Math.Max(0, rows - 1));
        var colSpan = Math.Clamp(right - left, 1, columns);
        var rowSpan = Math.Clamp(top - bottom, 1, rows);
        return layout.ToPixelRect(baseCol, baseRow, colSpan, rowSpan);
    }

    private static int ResolveHorizontalEdge(int value, bool usesGridSize, int columns)
    {
        if (!usesGridSize)
        {
            return value;
        }

        return Math.Clamp(columns + value, 0, columns);
    }

    private static int ResolveVerticalEdge(int value, bool usesGridSize, int rows)
    {
        if (!usesGridSize)
        {
            return value;
        }

        return Math.Clamp(rows + value, 0, rows);
    }
}

public sealed class FloatingPanelSpec
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = string.Empty;

    public string? ContentId { get; set; }
        = null;

    public double X { get; set; }
        = 0d;

    public double Y { get; set; }
        = 0d;

    public double Width { get; set; }
        = 200d;

    public double Height { get; set; }
        = 200d;

    public bool IsLocked { get; set; }
        = false;
}

public sealed class FloatingBlockSpec
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public Guid InstanceId { get; set; }
        = Guid.Empty;

    public double X { get; set; }
        = 0d;

    public double Y { get; set; }
        = 0d;

    public double Width { get; set; }
        = 160d;

    public double Height { get; set; }
        = 160d;
}
