using System;
using System.Collections.Generic;
using System.Numerics;
using Aog.Plugins.Mapping.Core;
using Avalonia.OpenGL;

namespace Aog.Plugins.Mapping.Rendering;

/// <summary>
/// Renders a simple background grid aligned to whole metres. Currently stubbed until shader infrastructure lands.
/// </summary>
public sealed class GridPassLayer : IMapLayer
{
    private bool _visible = true;

    public float SpacingMeters { get; set; } = 10f;

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    public int ZIndex { get; set; }

    public void Load(GlInterface gl)
    {
        // Shader setup will be added later.
    }

    public void UpdateCpu(double now)
    {
        // Nothing to update yet.
    }

    public void UploadGpu(GlInterface gl)
    {
        // Nothing to upload for the stubbed grid.
    }

    public void Draw(GlInterface gl, in FrameCtx ctx)
    {
        var spacing = Math.Max(SpacingMeters, 0.1f);
        var halfWidth = ctx.MetersPerPixel * ctx.ViewportSizePixels.X * 0.5f;
        var halfHeight = ctx.MetersPerPixel * ctx.ViewportSizePixels.Y * 0.5f;

        var cameraWorld = ctx.AnchorWorld + new Double3(ctx.CameraPosLocal.X, ctx.CameraPosLocal.Y, ctx.CameraPosLocal.Z);
        var minX = cameraWorld.X - halfWidth;
        var maxX = cameraWorld.X + halfWidth;
        var minY = cameraWorld.Y - halfHeight;
        var maxY = cameraWorld.Y + halfHeight;

        var startX = Math.Floor(minX / spacing) * spacing;
        var startY = Math.Floor(minY / spacing) * spacing;

        var vertices = new List<Vector2>();

        for (var x = startX; x <= maxX; x += spacing)
        {
            var start = ctx.WorldToNdc(x, minY);
            var end = ctx.WorldToNdc(x, maxY);
            vertices.Add(new Vector2(start.X, start.Y));
            vertices.Add(new Vector2(end.X, end.Y));
        }

        for (var y = startY; y <= maxY; y += spacing)
        {
            var start = ctx.WorldToNdc(minX, y);
            var end = ctx.WorldToNdc(maxX, y);
            vertices.Add(new Vector2(start.X, start.Y));
            vertices.Add(new Vector2(end.X, end.Y));
        }

        var color = new Vector4(0.25f, 0.32f, 0.38f, 0.6f);
        ctx.Batch.DrawLineSegments(vertices.ToArray(), color, 1f);
    }

    public void Dispose()
    {
        // Nothing to dispose yet.
    }

}
