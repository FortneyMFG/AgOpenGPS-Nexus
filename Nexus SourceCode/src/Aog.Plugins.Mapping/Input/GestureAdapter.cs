using System;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Aog.Plugins.Mapping.Input;

/// <summary>
/// Translates Avalonia pointer gestures into camera operations.
/// </summary>
public sealed class GestureAdapter : IDisposable
{
    private readonly Control _control;
    private readonly CameraRig _camera;
    private bool _isPanning;
    private IPointer? _activePointer;
    private Vector2 _lastPosition;

    public GestureAdapter(Control control, CameraRig camera)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));

        _control.PointerPressed += OnPointerPressed;
        _control.PointerMoved += OnPointerMoved;
        _control.PointerReleased += OnPointerReleased;
        _control.PointerWheelChanged += OnPointerWheelChanged;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_control);
        if (_isPanning || !point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isPanning = true;
        _activePointer = e.Pointer;
        _lastPosition = point.Position.ToVector2();
        e.Pointer.Capture(_control);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || _activePointer is null || e.Pointer != _activePointer)
        {
            return;
        }

        var current = e.GetPosition(_control).ToVector2();
        var delta = current - _lastPosition;
        _camera.PanScreenDelta(delta);
        _lastPosition = current;
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_activePointer is null || e.Pointer != _activePointer)
        {
            return;
        }

        e.Pointer.Capture(null);
        _isPanning = false;
        _activePointer = null;
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var position = e.GetPosition(_control).ToVector2();
        _camera.ZoomAt(position, (float)e.Delta.Y);
        e.Handled = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _control.PointerPressed -= OnPointerPressed;
        _control.PointerMoved -= OnPointerMoved;
        _control.PointerReleased -= OnPointerReleased;
        _control.PointerWheelChanged -= OnPointerWheelChanged;
    }
}

internal static class PointExtensions
{
    public static Vector2 ToVector2(this global::Avalonia.Point point) => new((float)point.X, (float)point.Y);
}
