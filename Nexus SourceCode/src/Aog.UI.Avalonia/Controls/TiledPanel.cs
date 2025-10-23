using System;
using System.Threading;
using Aog.UI.Avalonia.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Aog.UI.Avalonia.Controls;

public class TiledPanel : Panel
{
    private static int _activeInstances;

    public static readonly StyledProperty<ShellGridLayout?> LayoutProperty =
        AvaloniaProperty.Register<TiledPanel, ShellGridLayout?>(nameof(Layout));

    public static readonly AttachedProperty<int> RowProperty =
        AvaloniaProperty.RegisterAttached<TiledPanel, Control, int>("Row", 0, false, BindingMode.TwoWay);

    public static readonly AttachedProperty<int> ColumnProperty =
        AvaloniaProperty.RegisterAttached<TiledPanel, Control, int>("Column", 0, false, BindingMode.TwoWay);

    public static readonly AttachedProperty<int> RowSpanProperty =
        AvaloniaProperty.RegisterAttached<TiledPanel, Control, int>("RowSpan", 1, false, BindingMode.TwoWay);

    public static readonly AttachedProperty<int> ColumnSpanProperty =
        AvaloniaProperty.RegisterAttached<TiledPanel, Control, int>("ColumnSpan", 1, false, BindingMode.TwoWay);

    public TiledPanel()
    {
#if DEBUG
        var active = Interlocked.Increment(ref _activeInstances);
        if (active > 1)
        {
            throw new InvalidOperationException("Only one TiledPanel instance may exist in the visual tree.");
        }
#endif
    }

    public ShellGridLayout? Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public static int GetRow(AvaloniaObject obj) => obj.GetValue(RowProperty);

    public static void SetRow(AvaloniaObject obj, int value) => obj.SetValue(RowProperty, value);

    public static int GetColumn(AvaloniaObject obj) => obj.GetValue(ColumnProperty);

    public static void SetColumn(AvaloniaObject obj, int value) => obj.SetValue(ColumnProperty, value);

    public static int GetRowSpan(AvaloniaObject obj) => obj.GetValue(RowSpanProperty);

    public static void SetRowSpan(AvaloniaObject obj, int value) => obj.SetValue(RowSpanProperty, value);

    public static int GetColumnSpan(AvaloniaObject obj) => obj.GetValue(ColumnSpanProperty);

    public static void SetColumnSpan(AvaloniaObject obj, int value) => obj.SetValue(ColumnSpanProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        var layout = Layout;
        if (layout is null)
        {
            return base.MeasureOverride(availableSize);
        }

        var columns = Math.Max(1, layout.Columns);
        var rows = Math.Max(1, layout.Rows);

        foreach (var child in Children)
        {
            if (child is Control control)
            {
                var rect = layout.ToPixelRect(
                    Math.Clamp(GetColumn(control), 0, columns - 1),
                    Math.Clamp(GetRow(control), 0, rows - 1),
                    Math.Max(1, GetColumnSpan(control)),
                    Math.Max(1, GetRowSpan(control)));
                control.Measure(new Size(rect.Width, rect.Height));
            }
            else
            {
                child.Measure(availableSize);
            }
        }

        var width = layout.ToPixelRect(0, 0, columns, 1).Width;
        var height = layout.ToPixelRect(0, 0, 1, rows).Height;
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var layout = Layout;
        if (layout is null)
        {
            return base.ArrangeOverride(finalSize);
        }

        var columns = Math.Max(1, layout.Columns);
        var rows = Math.Max(1, layout.Rows);

        foreach (var child in Children)
        {
            if (child is Control control)
            {
                var rect = layout.ToPixelRect(
                    Math.Clamp(GetColumn(control), 0, columns - 1),
                    Math.Clamp(GetRow(control), 0, rows - 1),
                    Math.Max(1, GetColumnSpan(control)),
                    Math.Max(1, GetRowSpan(control)));
                control.Arrange(rect);
            }
            else
            {
                child.Arrange(new Rect(finalSize));
            }
        }

        return finalSize;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
#if DEBUG
        Interlocked.Exchange(ref _activeInstances, 0);
#endif
    }
}
