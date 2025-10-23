using System;
using System.Globalization;
using System.Text;
using Aog.UI.Avalonia.Mapping.Core;
using Avalonia.OpenGL;

namespace Aog.UI.Avalonia.Mapping.HUD;

/// <summary>
/// Placeholder HUD layer that surfaces debug information through GL overlays (text rendering TBD).
/// </summary>
public sealed class DebugHudLayer : IMapLayer
{
    private bool _visible = true;
    private string _frameInfo = string.Empty;

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    public int ZIndex { get; set; } = int.MaxValue;

    public void Load(GlInterface gl)
    {
        // Text rendering pipeline will be wired in later.
    }

    public void UpdateCpu(double now)
    {
        _frameInfo = BuildFrameInfo(now);
    }

    public void UploadGpu(GlInterface gl)
    {
        // No GPU resources yet.
    }

    public void Draw(GlInterface gl, in FrameCtx ctx)
    {
        // Stub: actual HUD rendering pending bitmap font integration.
    }

    public void Dispose()
    {
        // Nothing to dispose yet.
    }

    private static string BuildFrameInfo(double now)
    {
        var builder = new StringBuilder();
        builder.Append("t=");
        builder.Append(now.ToString("F2", CultureInfo.InvariantCulture));
        builder.Append("s");
        return builder.ToString();
    }
}
