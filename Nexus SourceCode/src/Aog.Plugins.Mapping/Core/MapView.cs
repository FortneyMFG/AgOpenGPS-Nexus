using System;
using System.Diagnostics;
using System.Numerics;
using Aog.Plugins.Mapping.Input;
using Aog.Plugins.Mapping.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Aog.Plugins.Mapping.Core;

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
    private bool _isGles;
    private PrimitiveBatch2D? _primitiveBatch;

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
        }

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.ScissorTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        if (_primitiveBatch is null)
        {
            var version = GL.GetString(StringName.Version) ?? string.Empty;
            _isGles = version.Contains("OpenGL ES", StringComparison.OrdinalIgnoreCase);
            _primitiveBatch = new PrimitiveBatch2D(_isGles);
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

        _primitiveBatch?.Dispose();
        _primitiveBatch = null;
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        var renderScale = (VisualRoot as IRenderRoot)?.RenderScaling ?? 1.0;
        var pixelWidth = Math.Max(1, (int)Math.Round(Bounds.Width * renderScale));
        var pixelHeight = Math.Max(1, (int)Math.Round(Bounds.Height * renderScale));

        GL.Viewport(0, 0, pixelWidth, pixelHeight);
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadIdentity();
        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadIdentity();

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
            _isGles,
            _camera.ViewportPixels,
            anchor,
            _primitiveBatch ?? throw new InvalidOperationException("Primitive batch not initialised"));

        _scene.Render(gl, ctx);
        RequestNextFrameRendering();
        GL.Flush();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _gestureAdapter?.Dispose();
        _gestureAdapter = null;
    }

    private sealed class AvaloniaBindingsContext : IBindingsContext
    {
        private readonly GlInterface _gl;

        public AvaloniaBindingsContext(GlInterface gl)
        {
            _gl = gl;
        }

        public IntPtr GetProcAddress(string procName) => _gl.GetProcAddress(procName);
    }
}
