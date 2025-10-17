using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Aog.UI.Avalonia.Controls;

/// <summary>
/// Lightweight sparkline control for visualising historical telemetry samples.
/// </summary>
public sealed class Sparkline : Control
{
    /// <summary>Identifies the <see cref="Values"/> property.</summary>
    public static readonly StyledProperty<IReadOnlyList<double>> ValuesProperty =
        AvaloniaProperty.Register<Sparkline, IReadOnlyList<double>>(nameof(Values), Array.Empty<double>());

    /// <summary>Identifies the <see cref="Stroke"/> property.</summary>
    public static readonly StyledProperty<IBrush> StrokeProperty =
        AvaloniaProperty.Register<Sparkline, IBrush>(nameof(Stroke), Brushes.LimeGreen);

    /// <summary>Identifies the <see cref="Fill"/> property.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<Sparkline, IBrush?>(nameof(Fill),
            new SolidColorBrush(Color.FromArgb(64, 50, 205, 50)));

    /// <summary>Identifies the <see cref="StrokeThickness"/> property.</summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(StrokeThickness), 2.0);

    /// <summary>Gets or sets the values rendered by the sparkline.</summary>
    public IReadOnlyList<double> Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Gets or sets the stroke brush.</summary>
    public IBrush Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>Gets or sets the optional fill brush below the sparkline.</summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Gets or sets the stroke thickness.</summary>
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValuesProperty ||
            change.Property == StrokeProperty ||
            change.Property == FillProperty ||
            change.Property == StrokeThicknessProperty)
        {
            InvalidateVisual();
        }
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var samples = Values ?? Array.Empty<double>();
        if (samples.Count < 2 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        double min = samples.Min();
        double max = samples.Max();
        if (Math.Abs(max - min) < 1e-9)
        {
            max = min + 1;
        }

        var geometry = new StreamGeometry();
        using (var contextImpl = geometry.Open())
        {
            for (var index = 0; index < samples.Count; index++)
            {
                var x = index / (double)(samples.Count - 1) * Bounds.Width;
                var normalized = (samples[index] - min) / (max - min);
                var y = Bounds.Height - normalized * Bounds.Height;
                var point = new Point(x, y);
                if (index == 0)
                {
                    contextImpl.BeginFigure(point, isFilled: Fill is not null);
                }
                else
                {
                    contextImpl.LineTo(point);
                }
            }
            contextImpl.EndFigure(isClosed: false);
        }

        if (Fill is not null)
        {
            context.DrawGeometry(Fill, null, geometry);
        }

        var pen = new Pen(Stroke, StrokeThickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        context.DrawGeometry(null, pen, geometry);
    }
}
