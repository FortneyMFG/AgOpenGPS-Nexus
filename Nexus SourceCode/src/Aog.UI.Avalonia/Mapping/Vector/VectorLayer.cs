using System;
using System.Collections.Generic;
using System.Numerics;
using Aog.UI.Avalonia.Mapping.Core;
using Avalonia.OpenGL;

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

            var fillVector = new Vector4(fillColor.Value.X, fillColor.Value.Y, fillColor.Value.Z, fillOpacity);
            var fanVertices = ToNdc(ctx, feature.Vertices);
            ctx.Batch.DrawTriangleFan(fanVertices, fillVector);
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

            var lineVector = new Vector4(lineColor.Value.X, lineColor.Value.Y, lineColor.Value.Z, opacity);
            var ndcVertices = ToNdc(ctx, feature.Vertices);

            switch (feature.Type)
            {
                case VectorFeatureType.Point:
                    ctx.Batch.DrawPoints(ndcVertices, lineVector, MathF.Max(3f, widthMeters / ctx.MetersPerPixel));
                    break;
                case VectorFeatureType.Polygon:
                    ctx.Batch.DrawLines(ndcVertices, lineVector, MathF.Max(1f, widthMeters / ctx.MetersPerPixel), loop: true);
                    break;
                default:
                    ctx.Batch.DrawLines(ndcVertices, lineVector, MathF.Max(1f, widthMeters / ctx.MetersPerPixel));
                    break;
            }
        }
    }

    public void Dispose()
    {
        // Nothing to dispose yet.
    }

    private static Vector2[] ToNdc(in FrameCtx ctx, IReadOnlyList<Double3> worldVertices)
    {
        var result = new Vector2[worldVertices.Count];
        for (var i = 0; i < worldVertices.Count; i++)
        {
            var ndc = ctx.WorldToNdc(worldVertices[i]);
            result[i] = new Vector2(ndc.X, ndc.Y);
        }

        return result;
    }
}
