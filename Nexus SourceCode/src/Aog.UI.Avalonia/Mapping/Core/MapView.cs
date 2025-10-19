using System;
using System.Diagnostics;
using System.Numerics;
using Aog.UI.Avalonia.Mapping.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Aog.UI.Avalonia.Mapping.Core;

/// <summary>
/// OpenGL-backed control that hosts the mapping scene.
/// </summary>
public sealed class MapView : OpenGlControlBase
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private MapScene _scene = new();
    private Localizer _localizer = new();
    private CameraRig _camera = new();
    private GestureAdapter? _gestureAdapter;
    private GlInterface? _currentGl;
    private bool _glReady;
    private bool _bindingsLoaded;

    public MapView()
    {
        Focusable = true;
    }

    /// <summary>
    /// Gets the active map scene.
    /// </summary>
    public MapScene Scene => _scene;

    /// <summary>
    /// Replaces the active scene.
    /// </summary>
    public void Attach(MapScene scene, CameraRig camera, Localizer localizer)
    {
        _scene?.Dispose();
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

        _gestureAdapter?.Dispose();
        _gestureAdapter = new GestureAdapter(this, _camera);

        if (_glReady && _currentGl is not null)
        {
            foreach (var layer in _scene.Layers)
            {
                layer.Load(_currentGl);
            }
        }
    }

    /// <summary>
    /// Adds the supplied layer to the scene and loads it immediately when GL is ready.
    /// </summary>
    public void AddLayer(IMapLayer layer)
    {
        _scene.Add(layer);
        if (_glReady && _currentGl is not null)
        {
            layer.Load(_currentGl);
        }
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);
        if (!_bindingsLoaded)
        {
            GL.LoadBindings(new AvaloniaBindingsContext(gl));
            _bindingsLoaded = true;
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
        }

        _currentGl = gl;
        _glReady = true;

        foreach (var layer in _scene.Layers)
        {
            layer.Load(gl);
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        base.OnOpenGlDeinit(gl);
        _glReady = false;
        _currentGl = null;

        foreach (var layer in _scene.Layers)
        {
            layer.Dispose();
        }
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        var renderScale = (VisualRoot as IRenderRoot)?.RenderScaling ?? 1.0;
        var pixelWidth = Math.Max(1, (int)Math.Round(Bounds.Width * renderScale));
        var pixelHeight = Math.Max(1, (int)Math.Round(Bounds.Height * renderScale));

        GL.Viewport(0, 0, pixelWidth, pixelHeight);
        GL.ClearColor(0.08f, 0.1f, 0.13f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _camera.Resize(pixelWidth, pixelHeight);

        var cameraWorld = _camera.PositionWorld;
        _localizer.TryRecenter(cameraWorld);
        var cameraLocal = _localizer.ToLocal(cameraWorld);

        var anchor = _localizer.Anchor;
        var ctx = new FrameCtx(
            _camera.View,
            _camera.Projection,
            cameraLocal,
            _clock.Elapsed.TotalSeconds,
            _camera.MetersPerPixel,
            IsGles(gl),
            _camera.ViewportPixels,
            anchor);

        _scene.Render(gl, ctx);
        RequestNextFrameRendering();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _gestureAdapter?.Dispose();
        _gestureAdapter = null;
    }

    private static bool IsGles(GlInterface gl) => false;
}

file sealed class AvaloniaBindingsContext : IBindingsContext
{
    private readonly GlInterface _gl;

    public AvaloniaBindingsContext(GlInterface gl)
    {
        _gl = gl;
    }

    public IntPtr GetProcAddress(string procName) => _gl.GetProcAddress(procName);
}
