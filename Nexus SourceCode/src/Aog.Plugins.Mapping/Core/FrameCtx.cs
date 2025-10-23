using System.Numerics;
using Aog.Plugins.Mapping.Rendering;

namespace Aog.Plugins.Mapping.Core;

/// <summary>
/// Encapsulates render-time state shared across the active map layers for the current frame.
/// </summary>
public readonly record struct FrameCtx(
    Matrix4x4 View,
    Matrix4x4 Projection,
    Vector3 CameraPosLocal,
    double TimeSeconds,
    float MetersPerPixel,
    bool IsGles,
    Vector2 ViewportSizePixels,
    Double3 AnchorWorld,
    PrimitiveBatch2D Batch)
{
    public Matrix4x4 ViewProjection => Matrix4x4.Multiply(View, Projection);

    public Vector3 WorldToNdc(in Double3 world)
    {
        if (ViewportSizePixels.X <= 0 || ViewportSizePixels.Y <= 0 || MetersPerPixel <= 0)
        {
            return Vector3.Zero;
        }

        var localX = (float)(world.X - AnchorWorld.X);
        var localY = (float)(world.Y - AnchorWorld.Y);

        var relX = localX - CameraPosLocal.X;
        var relY = localY - CameraPosLocal.Y;

        var pixelX = (relX / MetersPerPixel) + (ViewportSizePixels.X * 0.5f);
        var pixelY = (ViewportSizePixels.Y * 0.5f) - (relY / MetersPerPixel);

        var ndcX = (pixelX / ViewportSizePixels.X * 2f) - 1f;
        var ndcY = (pixelY / ViewportSizePixels.Y * 2f) - 1f;

        return new Vector3(ndcX, ndcY, 0);
    }

    public Vector3 WorldToNdc(double x, double y) => WorldToNdc(new Double3(x, y, 0));
}
