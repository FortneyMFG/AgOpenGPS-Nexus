using System;
using System.Linq;
using Aog.UI.Avalonia.Layout;
using Aog.UI.Avalonia.ViewModels.Shell;
using Aog.UI.Avalonia.Views.Field;
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
    private bool _tileDragActive;
    private BlockItemViewModel? _dragBlock;

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

        _dragBlock = block;
        _tileDragActive = false;
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
                EnsureTileDragStarted();
                _suppressClick = true;
                PseudoClasses.Set(":pressed", false);
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isDragging && _panel is not null && _dragBlock is { } block)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (_tileDragActive && topLevel is not null)
            {
                var pointerPosition = e.GetPosition(topLevel);
                if (TryDropIntoFieldDock(topLevel, pointerPosition, block))
                {
                    e.Pointer.Capture(null);
                    e.Handled = true;
                    base.OnPointerReleased(e);
                    ResetDragState();
                    return;
                }
            }

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

    private void EnsureTileDragStarted()
    {
        if (_tileDragActive)
        {
            return;
        }

        if (_dragBlock is { } block)
        {
            block.Owner.BeginTileDrag();
            _tileDragActive = true;
        }
    }

    private bool TryDropIntoFieldDock(TopLevel topLevel, Point pointerPosition, BlockItemViewModel block)
    {
        foreach (var dock in topLevel.GetVisualDescendants().OfType<FieldSettingsDock>())
        {
            if (!dock.IsEffectivelyVisible || !dock.IsHitTestVisible)
            {
                continue;
            }

            var origin = dock.TranslatePoint(default, topLevel);
            if (origin is null)
            {
                continue;
            }

            var bounds = new Rect(origin.Value, dock.Bounds.Size);
            if (bounds.Contains(pointerPosition) && block.Owner.CanDelete(block))
            {
                block.Owner.Delete(block);
                return true;
            }
        }

        return false;
    }

    private void ResetDragState()
    {
        if (_tileDragActive && _dragBlock is { } block)
        {
            block.Owner.EndTileDrag();
        }

        _isDragging = false;
        _suppressClick = false;
        _tileDragActive = false;
        _dragBlock = null;
        _panel = null;
    }
}
