using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels.Shell;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class PanelBoundsOverlay : UserControl
{
    private ResizeHandle _activeHandle = ResizeHandle.None;
    private Point _start;
    private Rect _initialBounds;

    public PanelBoundsOverlay()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginInteraction(ResizeHandle.Move, sender as IInputElement, e);
    }

    private void OnHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        if (!Enum.TryParse(control.Tag?.ToString(), ignoreCase: true, out ResizeHandle handle))
        {
            return;
        }

        BeginInteraction(handle, control, e);
    }

    private void BeginInteraction(ResizeHandle handle, IInputElement? captureTarget, PointerPressedEventArgs e)
    {
        if (DataContext is not PanelOverlayViewModel panel || panel.IsLocked)
        {
            return;
        }

        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind is not PointerUpdateKind.LeftButtonPressed)
        {
            return;
        }

        _activeHandle = handle;
        _start = e.GetPosition(this);
        _initialBounds = panel.Bounds;
        if (captureTarget is not null)
        {
            e.Pointer.Capture(captureTarget);
        }

        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_activeHandle == ResizeHandle.None || DataContext is not PanelOverlayViewModel panel)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ResetInteraction(e.Pointer);
            return;
        }

        var current = e.GetPosition(this);
        var delta = current - _start;
        if (Math.Abs(delta.X) < double.Epsilon && Math.Abs(delta.Y) < double.Epsilon)
        {
            return;
        }

        var updated = CalculateBounds(delta);
        panel.UpdateBounds(updated);
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_activeHandle != ResizeHandle.None)
        {
            ResetInteraction(e.Pointer);
            e.Handled = true;
        }
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_activeHandle != ResizeHandle.None)
        {
            ResetInteraction(e.Pointer);
        }
    }

    private void ResetInteraction(IPointer pointer)
    {
        _activeHandle = ResizeHandle.None;
        _start = default;
        _initialBounds = default;
        pointer.Capture(null);
    }

    private Rect CalculateBounds(Vector delta)
    {
        var bounds = _initialBounds;
        const double minSize = 24d;

        return _activeHandle switch
        {
            ResizeHandle.Move => new Rect(bounds.Position + delta, bounds.Size),
            ResizeHandle.Left => ResizeFromLeft(bounds, delta.X, minSize),
            ResizeHandle.Right => ResizeFromRight(bounds, delta.X, minSize),
            ResizeHandle.Top => ResizeFromTop(bounds, delta.Y, minSize),
            ResizeHandle.Bottom => ResizeFromBottom(bounds, delta.Y, minSize),
            ResizeHandle.TopLeft => ResizeFromLeft(ResizeFromTop(bounds, delta.Y, minSize), delta.X, minSize),
            ResizeHandle.TopRight => ResizeFromRight(ResizeFromTop(bounds, delta.Y, minSize), delta.X, minSize),
            ResizeHandle.BottomLeft => ResizeFromLeft(ResizeFromBottom(bounds, delta.Y, minSize), delta.X, minSize),
            ResizeHandle.BottomRight => ResizeFromRight(ResizeFromBottom(bounds, delta.Y, minSize), delta.X, minSize),
            _ => bounds,
        };
    }

    private static Rect ResizeFromLeft(Rect bounds, double deltaX, double minSize)
    {
        var newLeft = Math.Min(bounds.Right - minSize, bounds.X + deltaX);
        var width = bounds.Right - newLeft;
        if (width < minSize)
        {
            width = minSize;
            newLeft = bounds.Right - width;
        }

        return new Rect(newLeft, bounds.Y, width, bounds.Height);
    }

    private static Rect ResizeFromRight(Rect bounds, double deltaX, double minSize)
    {
        var width = Math.Max(minSize, bounds.Width + deltaX);
        return new Rect(bounds.X, bounds.Y, width, bounds.Height);
    }

    private static Rect ResizeFromTop(Rect bounds, double deltaY, double minSize)
    {
        var newTop = Math.Min(bounds.Bottom - minSize, bounds.Y + deltaY);
        var height = bounds.Bottom - newTop;
        if (height < minSize)
        {
            height = minSize;
            newTop = bounds.Bottom - height;
        }

        return new Rect(bounds.X, newTop, bounds.Width, height);
    }

    private static Rect ResizeFromBottom(Rect bounds, double deltaY, double minSize)
    {
        var height = Math.Max(minSize, bounds.Height + deltaY);
        return new Rect(bounds.X, bounds.Y, bounds.Width, height);
    }

    private enum ResizeHandle
    {
        None,
        Move,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }
}
