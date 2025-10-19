using System.Numerics;

namespace Aog.UI.Avalonia.Mapping.Core;

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
    Double3 AnchorWorld)
{
    public Matrix4x4 ViewProjection => Matrix4x4.Multiply(View, Projection);
}
