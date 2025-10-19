using System;
using System.Numerics;
using Aog.UI.Avalonia.Mapping.Core;
using Aog.UI.Avalonia.Models;
using Avalonia.OpenGL;
using OpenTK.Graphics.OpenGL;

namespace Aog.UI.Avalonia.Mapping.Rendering;

/// <summary>
/// Renders the current vehicle pose as a simple arrow.
/// </summary>
public sealed class VehiclePassLayer : IMapLayer
{
    private readonly Func<VehiclePose> _poseProvider;
    private readonly float _bodyLength;
    private readonly float _bodyWidth;
    private bool _visible = true;

    public VehiclePassLayer(Func<VehiclePose> poseProvider, float bodyLength = 6f, float bodyWidth = 3f)
    {
        _poseProvider = poseProvider ?? throw new ArgumentNullException(nameof(poseProvider));
        _bodyLength = bodyLength;
        _bodyWidth = bodyWidth;
    }

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    public int ZIndex { get; set; }

    public void Load(GlInterface gl)
    {
        // No GPU boot required for the prototype.
    }

    public void UpdateCpu(double now)
    {
        // Pose queried during draw.
    }

    public void UploadGpu(GlInterface gl)
    {
        // No GPU uploads yet.
    }

    public void Draw(GlInterface gl, in FrameCtx ctx)
    {
        var pose = _poseProvider();
        var headingRadians = pose.HeadingDegrees * Math.PI / 180.0;
        var cos = Math.Cos(headingRadians);
        var sin = Math.Sin(headingRadians);

        var tip = new Double3(
            pose.X + (_bodyLength * 0.5 * cos),
            pose.Y + (_bodyLength * 0.5 * sin),
            0);

        var rear = new Double3(
            pose.X - (_bodyLength * 0.5 * cos),
            pose.Y - (_bodyLength * 0.5 * sin),
            0);

        var left = new Double3(
            rear.X - (_bodyWidth * 0.5 * sin),
            rear.Y + (_bodyWidth * 0.5 * cos),
            0);

        var right = new Double3(
            rear.X + (_bodyWidth * 0.5 * sin),
            rear.Y - (_bodyWidth * 0.5 * cos),
            0);

        GL.Color4(0.16f, 0.58f, 0.98f, 0.9f);
        GL.Begin(PrimitiveType.Triangles);
        SubmitVertex(ctx, tip);
        SubmitVertex(ctx, left);
        SubmitVertex(ctx, right);
        GL.End();

        GL.Color4(0.07f, 0.25f, 0.48f, 1f);
        GL.LineWidth(2f);
        GL.Begin(PrimitiveType.LineLoop);
        SubmitVertex(ctx, tip);
        SubmitVertex(ctx, left);
        SubmitVertex(ctx, right);
        GL.End();
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }

    private static void SubmitVertex(in FrameCtx ctx, Double3 world)
    {
        var local = new Vector3(
            (float)(world.X - ctx.AnchorWorld.X),
            (float)(world.Y - ctx.AnchorWorld.Y),
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
