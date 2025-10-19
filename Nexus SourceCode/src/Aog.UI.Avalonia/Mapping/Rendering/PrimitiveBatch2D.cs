using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace Aog.UI.Avalonia.Mapping.Rendering;

/// <summary>
/// Minimal shader-backed helper for drawing 2D primitives in NDC space, compatible with GL and GLES.
/// </summary>
public sealed class PrimitiveBatch2D : IDisposable
{
    private readonly bool _isGles;
    private readonly int _program;
    private readonly int _vbo;
    private readonly int _vao;
    private readonly int _colorLocation;
    private bool _disposed;
    private readonly List<Vector2> _scratch = new();
    private readonly List<float> _floatScratch = new();

    public PrimitiveBatch2D(bool isGles)
    {
        _isGles = isGles;
        _program = CompileProgram(isGles);
        _colorLocation = GL.GetUniformLocation(_program, "uColor");

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 2 * 0, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void DrawLines(ReadOnlySpan<Vector2> vertices, in Vector4 color, float lineWidth = 1f, bool loop = false)
    {
        if (vertices.Length < 2)
        {
            return;
        }

        if (loop)
        {
            _scratch.Clear();
            _scratch.AddRange(vertices.ToArray());
            _scratch.Add(vertices[0]);
            Draw(PrimitiveType.LineStrip, CollectionsMarshal.AsSpan(_scratch), color, lineWidth);
        }
        else
        {
        Draw(PrimitiveType.LineStrip, vertices, color, lineWidth);
        }
    }

    public void DrawLineSegments(ReadOnlySpan<Vector2> vertices, in Vector4 color, float lineWidth = 1f)
    {
        if (vertices.Length < 2)
        {
            return;
        }

        Draw(PrimitiveType.Lines, vertices, color, lineWidth);
    }

    public void DrawPoints(ReadOnlySpan<Vector2> vertices, in Vector4 color, float pointSize = 4f)
    {
        if (vertices.Length == 0)
        {
            return;
        }

        GL.PointSize(pointSize);
        Draw(PrimitiveType.Points, vertices, color, 1f);
    }

    public void DrawTriangleFan(ReadOnlySpan<Vector2> vertices, in Vector4 color)
    {
        if (vertices.Length < 3)
        {
            return;
        }

        Draw(PrimitiveType.TriangleFan, vertices, color, 1f);
    }

    private void Draw(PrimitiveType primitive, ReadOnlySpan<Vector2> vertices, in Vector4 color, float lineWidth)
    {
        GL.UseProgram(_program);
        GL.Uniform4(_colorLocation, color.X, color.Y, color.Z, color.W);
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        _floatScratch.Clear();
        foreach (var vertex in vertices)
        {
            _floatScratch.Add(vertex.X);
            _floatScratch.Add(vertex.Y);
        }

        var bufferSpan = CollectionsMarshal.AsSpan(_floatScratch);
        var bufferArray = bufferSpan.ToArray();
        if (bufferArray.Length == 0)
        {
            return;
        }

        var sizeInBytes = bufferArray.Length * sizeof(float);
        unsafe
        {
            fixed (float* ptr = bufferArray)
            {
                GL.BufferData(BufferTarget.ArrayBuffer, sizeInBytes, (IntPtr)ptr, BufferUsageHint.DynamicDraw);
            }
        }
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, 0);

        if (primitive is PrimitiveType.Lines or PrimitiveType.LineStrip)
        {
            GL.LineWidth(Math.Clamp(lineWidth, 1f, 10f));
        }

        GL.DrawArrays(primitive, 0, vertices.Length);

        GL.DisableVertexAttribArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    private static int CompileProgram(bool isGles)
    {
        var vertexSrc = isGles
            ? @"#version 300 es
layout(location = 0) in vec2 aPosition;
void main()
{
    gl_Position = vec4(aPosition, 0.0, 1.0);
}"
            : @"#version 330 core
layout(location = 0) in vec2 aPosition;
void main()
{
    gl_Position = vec4(aPosition, 0.0, 1.0);
}";

        var fragmentSrc = isGles
            ? @"#version 300 es
precision mediump float;
uniform vec4 uColor;
out vec4 FragColor;
void main()
{
    FragColor = uColor;
}"
            : @"#version 330 core
out vec4 FragColor;
uniform vec4 uColor;
void main()
{
    FragColor = uColor;
}";

        var vertexShader = CompileShader(ShaderType.VertexShader, vertexSrc);
        var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSrc);

        var program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var status);
        if (status == 0)
        {
            var info = GL.GetProgramInfoLog(program);
            GL.DeleteProgram(program);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            throw new InvalidOperationException($"Failed to link primitive shader program: {info}");
        }

        GL.DetachShader(program, vertexShader);
        GL.DetachShader(program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        return program;
    }

    private static int CompileShader(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out var status);
        if (status == 0)
        {
            var info = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"Failed to compile shader ({type}): {info}");
        }

        return shader;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_program);
    }
}
