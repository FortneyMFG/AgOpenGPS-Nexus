using System;
using Avalonia.OpenGL;

namespace Aog.UI.Avalonia.Mapping.Core;

/// <summary>
/// Represents a logical drawable element within the map scene.
/// </summary>
public interface IMapLayer : IDisposable
{
    /// <summary>
    /// Gets or sets a value indicating whether the layer is visible for rendering.
    /// </summary>
    bool Visible { get; set; }

    /// <summary>
    /// Gets or sets the draw order; higher values render later.
    /// </summary>
    int ZIndex { get; set; }

    /// <summary>
    /// Performs GL resource allocation.
    /// </summary>
    void Load(GlInterface gl);

    /// <summary>
    /// Executes CPU-side maintenance for the layer (tile scheduling, geometry baking, etc.).
    /// </summary>
    void UpdateCpu(double now);

    /// <summary>
    /// Uploads any pending CPU state to GPU resources.
    /// </summary>
    void UploadGpu(GlInterface gl);

    /// <summary>
    /// Draws the layer using the provided GL context and frame data.
    /// </summary>
    void Draw(GlInterface gl, in FrameCtx ctx);
}
