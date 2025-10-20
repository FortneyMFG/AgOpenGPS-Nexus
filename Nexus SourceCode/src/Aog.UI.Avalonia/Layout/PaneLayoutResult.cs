using System.Collections.Generic;
using Avalonia;

namespace Aog.UI.Avalonia.Layout;

public sealed class PaneLayoutResult
{
    public PaneLayoutResult(IReadOnlyList<PaneVisual> panes, IReadOnlyList<PaneDividerVisual> dividers)
    {
        Panes = panes;
        Dividers = dividers;
    }

    public IReadOnlyList<PaneVisual> Panes { get; }

    public IReadOnlyList<PaneDividerVisual> Dividers { get; }
}

public sealed record PaneVisual(LeafPane Pane, Rect Bounds);

public enum PaneDividerOrientation
{
    Horizontal,
    Vertical,
}

public sealed record PaneDividerVisual(PaneDividerOrientation Orientation, Rect Bounds);
