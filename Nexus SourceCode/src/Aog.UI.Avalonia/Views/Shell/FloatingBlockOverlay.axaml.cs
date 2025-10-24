using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels.Shell;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class FloatingBlockOverlay : UserControl
{
    private bool _isDragging;
    private bool _isResizing;
    private Point _dragStart;
    private Rect _initialBounds;

    public FloatingBlockOverlay()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not FloatingBlockViewModel block || block.IsLocked)
        {
            return;
        }

        _isDragging = true;
        _dragStart = e.GetPosition(this);
        _initialBounds = block.Bounds;
        (sender as IInputElement)?.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnDragPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || DataContext is not FloatingBlockViewModel block)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ResetDrag(sender, e.Pointer);
            return;
        }

        var current = e.GetPosition(this);
        var delta = current - _dragStart;
        if (Math.Abs(delta.X) < double.Epsilon && Math.Abs(delta.Y) < double.Epsilon)
        {
            return;
        }

        var updated = new Rect(
            _initialBounds.X + delta.X,
            _initialBounds.Y + delta.Y,
            _initialBounds.Width,
            _initialBounds.Height);
        var normalized = block.Owner.ClampFloatingBounds(updated, 96, 96);
        block.Owner.UpdateFloatingBlock(block, normalized);
        e.Handled = true;
    }

    private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            ResetDrag(sender, e.Pointer);
            e.Handled = true;
        }
    }

    private void OnResizePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not FloatingBlockViewModel block || block.IsLocked)
        {
            return;
        }

        _isResizing = true;
        _dragStart = e.GetPosition(this);
        _initialBounds = block.Bounds;
        (sender as IInputElement)?.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnResizePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isResizing || DataContext is not FloatingBlockViewModel block)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ResetResize(sender, e.Pointer);
            return;
        }

        var current = e.GetPosition(this);
        var delta = current - _dragStart;
        if (Math.Abs(delta.X) < double.Epsilon && Math.Abs(delta.Y) < double.Epsilon)
        {
            return;
        }

        var width = _initialBounds.Width + delta.X;
        var height = _initialBounds.Height + delta.Y;
        var updated = new Rect(_initialBounds.Position, new Size(width, height));
        var normalized = block.Owner.ClampFloatingBounds(updated, 96, 96);
        block.Owner.UpdateFloatingBlock(block, normalized);
        e.Handled = true;
    }

    private void OnResizePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizing)
        {
            ResetResize(sender, e.Pointer);
            e.Handled = true;
        }
    }

    private void ResetDrag(object? sender, IPointer pointer)
    {
        _isDragging = false;
        _dragStart = default;
        _initialBounds = default;
        (sender as IInputElement)?.ReleasePointerCapture(pointer);
    }

    private void ResetResize(object? sender, IPointer pointer)
    {
        _isResizing = false;
        _dragStart = default;
        _initialBounds = default;
        (sender as IInputElement)?.ReleasePointerCapture(pointer);
    }
}
