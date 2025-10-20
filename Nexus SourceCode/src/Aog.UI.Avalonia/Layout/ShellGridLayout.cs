using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Layout;

namespace Aog.UI.Avalonia.Layout;

public sealed class ShellGridLayout
{
    public double CellPx { get; init; } = 56;

    public double GutterPx { get; init; } = 8;

    public int Columns { get; set; } = 1;

    public int Rows { get; set; } = 1;

    public PaneNode? RootPane { get; set; }
        = null;

    public List<TileSpec> Tiles { get; init; } = new();

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

        var cell = CellPx;
        var gutter = GutterPx;
        var x = column * (cell + gutter);
        var y = row * (cell + gutter);
        var width = columnSpan * cell + Math.Max(0, columnSpan - 1) * gutter;
        var height = rowSpan * cell + Math.Max(0, rowSpan - 1) * gutter;
        return new Rect(x, y, width, height);
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
        = 2;

    public int ColSpan { get; set; }
        = 2;

    public bool PaneAttached { get; set; }
        = false;

    public string? PaneId { get; set; }
        = null;

    public RelativeAnchor Anchor { get; set; }
        = RelativeAnchor.TopCenter;

    public (int dx, int dy) Offset { get; set; }
        = (0, 0);
}
