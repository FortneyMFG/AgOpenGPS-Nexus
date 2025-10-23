using System;
using System.Linq;
using Aog.UI.Avalonia.Layout;
using Aog.UI.Avalonia.ViewModels.Shell;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Aog.UI.Avalonia.Controls;

public sealed class BlockTileButton : Button
{
    private const double DragThreshold = 6;
    private bool _isDragging;
    private bool _suppressClick;
    private Point _start;
    private TiledPanel? _panel;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (DataContext is not BlockItemViewModel block || block.Owner.IsLocked)
        {
            return;
        }

        if (e.Source is Visual visual && visual.GetVisualAncestors().OfType<Menu>().Any())
        {
            return;
        }

        _panel = this.GetVisualAncestors().OfType<TiledPanel>().FirstOrDefault();
        if (_panel?.Layout is null)
        {
            _panel = null;
            return;
        }

        _isDragging = true;
        _suppressClick = false;
        _start = e.GetPosition(_panel);
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isDragging || _panel is null)
        {
            return;
        }

        var current = e.GetPosition(_panel);
        if (!_suppressClick)
        {
            var delta = current - _start;
            if (Math.Abs(delta.X) > DragThreshold || Math.Abs(delta.Y) > DragThreshold)
            {
                _suppressClick = true;
                PseudoClasses.Set(":pressed", false);
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isDragging && _panel is not null && DataContext is BlockItemViewModel block)
        {
            var position = e.GetPosition(_panel);
            if (_panel.Layout is { } layout)
            {
                var (col, row) = layout.SnapToGrid(position, block.Tile.ColSpan, block.Tile.RowSpan);
                block.Owner.MoveBlock(block, col, row);
            }

            e.Pointer.Capture(null);
            e.Handled = _suppressClick;
        }

        base.OnPointerReleased(e);
        ResetDragState();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        ResetDragState();
    }

    private void ResetDragState()
    {
        _isDragging = false;
        _suppressClick = false;
        _panel = null;
    }
}
