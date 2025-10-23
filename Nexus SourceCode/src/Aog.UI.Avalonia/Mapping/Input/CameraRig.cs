using System;
using System.Numerics;
using Aog.UI.Avalonia.Mapping.Core;

namespace Aog.UI.Avalonia.Mapping.Input;

/// <summary>
/// Maintains camera state for the map view and exposes helpers for screen/world conversions.
/// </summary>
public sealed class CameraRig
{
    private readonly float _minZoom;
    private readonly float _maxZoom;
    private Vector2 _viewportPixels = new(1, 1);
    private Vector2 _centerMeters = Vector2.Zero;
    private float _zoom;
    private readonly float _heightMeters;

    public CameraRig(float minZoom = 0.5f, float maxZoom = 64f, float initialZoom = 4f, float heightMeters = 120f)
    {
        if (minZoom <= 0 || maxZoom <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minZoom));
        }

        if (maxZoom < minZoom)
        {
            throw new ArgumentException("Max zoom must be greater than min zoom.", nameof(maxZoom));
        }

        if (initialZoom < minZoom || initialZoom > maxZoom)
        {
            initialZoom = Math.Clamp(initialZoom, minZoom, maxZoom);
        }

        _minZoom = minZoom;
        _maxZoom = maxZoom;
        _zoom = initialZoom;
        _heightMeters = heightMeters;
    }

    public Matrix4x4 View => Matrix4x4.CreateTranslation(-_centerMeters.X, -_centerMeters.Y, 0);

    public Matrix4x4 Projection => Matrix4x4.CreateOrthographic(
        _viewportPixels.X * MetersPerPixel,
        _viewportPixels.Y * MetersPerPixel,
        -_heightMeters,
        _heightMeters);

    public float MetersPerPixel => 1f / _zoom;

    public Vector2 ViewportPixels => _viewportPixels;

    public Double3 PositionWorld => new(_centerMeters.X, _centerMeters.Y, _heightMeters);

    public Vector2 Center
    {
        get => _centerMeters;
        set => _centerMeters = value;
    }

    public void Resize(float widthPixels, float heightPixels)
    {
        _viewportPixels = new Vector2(Math.Max(widthPixels, 1), Math.Max(heightPixels, 1));
    }

    public void PanScreenDelta(Vector2 screenDelta)
    {
        var worldDelta = new Vector2(
            screenDelta.X * MetersPerPixel,
            -screenDelta.Y * MetersPerPixel);
        _centerMeters -= worldDelta;
    }

    public void ZoomAt(Vector2 screenPoint, float zoomDelta)
    {
        var zoomFactor = MathF.Pow(1.2f, zoomDelta);
        var targetZoom = Math.Clamp(_zoom * zoomFactor, _minZoom, _maxZoom);
        if (Math.Abs(targetZoom - _zoom) < float.Epsilon)
        {
            return;
        }

        var worldBefore = ScreenToWorld(screenPoint);
        _zoom = targetZoom;
        var worldAfter = ScreenToWorld(screenPoint);
        var correction = worldAfter - worldBefore;
        _centerMeters -= correction;
    }

    public Vector2 ScreenToWorld(Vector2 screenPoint)
    {
        var offset = ScreenToWorldOffset(screenPoint);
        return _centerMeters + offset;
    }

    public Vector2 WorldToScreen(Vector2 worldPoint)
    {
        var offset = worldPoint - _centerMeters;
        var pixels = new Vector2(
            (offset.X / MetersPerPixel) + (_viewportPixels.X * 0.5f),
            (_viewportPixels.Y * 0.5f) - (offset.Y / MetersPerPixel));
        return pixels;
    }

    public void FrameBounds(Double3 min, Double3 max, float paddingMeters = 10f)
    {
        var width = (float)(max.X - min.X + (paddingMeters * 2));
        var height = (float)(max.Y - min.Y + (paddingMeters * 2));
        var center = new Vector2(
            (float)((min.X + max.X) / 2.0),
            (float)((min.Y + max.Y) / 2.0));

        FrameBounds(width, height, center);
    }

    public void FrameBounds(float widthMeters, float heightMeters, Vector2 center)
    {
        _centerMeters = center;

        if (_viewportPixels.X <= 0 || _viewportPixels.Y <= 0)
        {
            return;
        }

        var zoomX = widthMeters <= 0 ? _zoom : _viewportPixels.X / widthMeters;
        var zoomY = heightMeters <= 0 ? _zoom : _viewportPixels.Y / heightMeters;
        var targetZoom = Math.Clamp(Math.Min(zoomX, zoomY), _minZoom, _maxZoom);
        _zoom = targetZoom;
    }

    private Vector2 ScreenToWorldOffset(Vector2 screenPoint)
    {
        var normalized = new Vector2(
            (screenPoint.X / _viewportPixels.X) - 0.5f,
            0.5f - (screenPoint.Y / _viewportPixels.Y));

        return new Vector2(
            normalized.X * _viewportPixels.X * MetersPerPixel,
            normalized.Y * _viewportPixels.Y * MetersPerPixel);
    }
}
