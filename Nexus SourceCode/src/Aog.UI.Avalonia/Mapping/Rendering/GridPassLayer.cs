using System;
using System.Numerics;
using Aog.UI.Avalonia.Mapping.Core;
using Avalonia.OpenGL;
using OpenTK.Graphics.OpenGL;

namespace Aog.UI.Avalonia.Mapping.Rendering;

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

        GL.LineWidth(1f);
        GL.Color4(0.25f, 0.32f, 0.38f, 0.6f);
        GL.Begin(PrimitiveType.Lines);

        for (var x = startX; x <= maxX; x += spacing)
        {
            SubmitVertex(ctx, x, minY);
            SubmitVertex(ctx, x, maxY);
        }

        for (var y = startY; y <= maxY; y += spacing)
        {
            SubmitVertex(ctx, minX, y);
            SubmitVertex(ctx, maxX, y);
        }

        GL.End();
    }

    public void Dispose()
    {
        // Nothing to dispose yet.
    }

    private static void SubmitVertex(in FrameCtx ctx, double worldX, double worldY)
    {
        var local = new Vector3(
            (float)(worldX - ctx.AnchorWorld.X),
            (float)(worldY - ctx.AnchorWorld.Y),
            0);
        var vector = Vector4.Transform(new Vector4(local, 1f), ctx.ViewProjection);
        if (Math.Abs(vector.W) < float.Epsilon)
        {
            return;
        }

        var ndc = vector / vector.W;
        GL.Vertex3(ndc.X, ndc.Y, ndc.Z);
    }
}
