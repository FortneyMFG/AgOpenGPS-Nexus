using System;
using System.Collections.Generic;
using System.Numerics;
using Aog.UI.Avalonia.Mapping.Core;
using Avalonia.OpenGL;
using OpenTK.Graphics.OpenGL;

namespace Aog.UI.Avalonia.Mapping.Vector;

/// <summary>
/// Vector layer stub; CPU batching and GL upload hooks will be populated alongside data sources.
/// </summary>
public sealed class VectorLayer : IMapLayer
{
    private readonly IVectorSource _source;
    private IReadOnlyList<VectorFeature> _cache = Array.Empty<VectorFeature>();
    private bool _visible = true;

    public VectorLayer(IVectorSource source, Styles.FillSymbolizer? fill = null, Styles.LineSymbolizer? line = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        Fill = fill;
        Line = line;
    }

    public Styles.FillSymbolizer? Fill { get; }

    public Styles.LineSymbolizer? Line { get; }

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    public int ZIndex { get; set; }

    public void Load(GlInterface gl)
    {
        // Shader compilation and VAO setup will be wired in during rendering implementation.
    }

    public void UpdateCpu(double now)
    {
        _cache = _source.GetSnapshot();
    }

    public void UploadGpu(GlInterface gl)
    {
        // No GPU upload yet for immediate-mode prototype.
    }

    public void Draw(GlInterface gl, in FrameCtx ctx)
    {
        if (_cache.Count == 0)
        {
            return;
        }

        foreach (var feature in _cache)
        {
            if (feature.Type != VectorFeatureType.Polygon)
            {
                continue;
            }

            var fillColor = feature.FillColor ?? Fill?.Rgba;
            if (fillColor is null)
            {
                continue;
            }

            var fillOpacity = feature.FillColor.HasValue
                ? fillColor.Value.W
                : Fill is null ? 1f : Fill.Opacity * fillColor.Value.W;

            GL.Color4(fillColor.Value.X, fillColor.Value.Y, fillColor.Value.Z, fillOpacity);
            GL.Begin(PrimitiveType.TriangleFan);
            foreach (var vertex in feature.Vertices)
            {
                SubmitVertex(ctx, vertex);
            }
            GL.End();
        }

        foreach (var feature in _cache)
        {
            var lineColor = feature.LineColor ?? Line?.Rgba;
            if (lineColor is null)
            {
                continue;
            }

            var widthMeters = feature.LineWidthMeters ?? Line?.WidthMeters ?? 1f;
            var opacity = feature.LineColor.HasValue
                ? lineColor.Value.W
                : Line is null ? 1f : Line.Opacity * lineColor.Value.W;

            GL.Color4(lineColor.Value.X, lineColor.Value.Y, lineColor.Value.Z, opacity);
            GL.LineWidth(MathF.Max(1f, widthMeters / ctx.MetersPerPixel));

            var mode = feature.Type switch
            {
                VectorFeatureType.Point => PrimitiveType.Points,
                _ => PrimitiveType.LineStrip
            };

            GL.Begin(mode);
            foreach (var vertex in feature.Vertices)
            {
                SubmitVertex(ctx, vertex);
            }

            if (feature.Type == VectorFeatureType.Polygon && feature.Vertices.Count > 0)
            {
                SubmitVertex(ctx, feature.Vertices[0]);
            }

            GL.End();
        }
    }

    public void Dispose()
    {
        // Nothing to dispose yet.
    }

    private static void SubmitVertex(in FrameCtx ctx, Double3 world)
    {
        var local = new Vector3(
            (float)(world.X - ctx.AnchorWorld.X),
            (float)(world.Y - ctx.AnchorWorld.Y),
            (float)(world.Z - ctx.AnchorWorld.Z));

        var vector = Vector4.Transform(new Vector4(local, 1f), ctx.ViewProjection);
        if (Math.Abs(vector.W) < float.Epsilon)
        {
            return;
        }

        var ndc = vector / vector.W;
        GL.Vertex3(ndc.X, ndc.Y, ndc.Z);
    }
}
