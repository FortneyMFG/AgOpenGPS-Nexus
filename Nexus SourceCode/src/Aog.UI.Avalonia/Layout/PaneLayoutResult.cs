using System.Collections.Generic;
using AvaloniaRect = global::Avalonia.Rect;

namespace Aog.UI.Avalonia.Layout;

public sealed class PaneLayoutResult
{
    public PaneLayoutResult(IReadOnlyList<PanelVisual> panels, IReadOnlyList<PanelDividerVisual> dividers)
    {
        Panels = panels;
        Dividers = dividers;
    }

    public IReadOnlyList<PanelVisual> Panels { get; }

    public IReadOnlyList<PanelDividerVisual> Dividers { get; }
}

public sealed record PanelVisual(string Id, AvaloniaRect Bounds);

public enum PanelDividerOrientation
{
    Horizontal,
    Vertical,
}

public sealed record PanelDividerVisual(PanelDividerOrientation Orientation, AvaloniaRect Bounds, string PanelId);
