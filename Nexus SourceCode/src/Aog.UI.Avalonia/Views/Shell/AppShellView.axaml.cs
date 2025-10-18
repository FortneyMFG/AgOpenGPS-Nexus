using System;
using System.Collections;
using System.Linq;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.ViewModels.Shell;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class AppShellView : UserControl
{
    private const string DragDataFormat = "Aog.UI.Avalonia.BlockItem";

    public AppShellView()
    {
        InitializeComponent();
        AttachBlockHost(nameof(TopBlocksControl), BlockRegion.Top, Orientation.Horizontal);
        AttachBlockHost(nameof(BottomBlocksControl), BlockRegion.Bottom, Orientation.Horizontal);
        AttachBlockHost(nameof(LeftBlocksControl), BlockRegion.Left, Orientation.Vertical);
        AttachBlockHost(nameof(RightBlocksControl), BlockRegion.Right, Orientation.Vertical);
    }

    private void AttachBlockHost(string controlName, BlockRegion region, Orientation orientation)
    {
        if (this.FindControl<ItemsControl>(controlName) is not { } itemsControl)
        {
            return;
        }

        itemsControl.AddHandler(DragDrop.DragOverEvent, (sender, e) => OnDragOver(region, sender, e));
        itemsControl.AddHandler(DragDrop.DropEvent, (sender, e) => OnDrop(region, orientation, sender, e));
        itemsControl.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not AppShellViewModel shell || sender is not ItemsControl)
        {
            return;
        }

        if (e.Source is Control { DataContext: BlockItemViewModel item } control)
        {
            var point = e.GetCurrentPoint(control);
            if (!point.Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (!shell.Layout.CanDrag(item))
            {
                return;
            }

#pragma warning disable CS0618
            var data = new DataObject();
            data.Set(DragDataFormat, item);
            _ = DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
#pragma warning restore CS0618
            e.Handled = true;
        }
    }

    private void OnDragOver(BlockRegion region, object? sender, DragEventArgs e)
    {
        if (!TryGetDraggedItem(e, out var item) || DataContext is not AppShellViewModel shell)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        if (shell.Layout.CanDrop(item, region))
        {
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDrop(BlockRegion region, Orientation orientation, object? sender, DragEventArgs e)
    {
        if (!TryGetDraggedItem(e, out var item) || DataContext is not AppShellViewModel shell)
        {
            return;
        }

        var itemsControl = sender as ItemsControl ?? FindItemsControl(e.Source);
        if (itemsControl is null)
        {
            return;
        }

        var insertIndex = GetInsertIndex(itemsControl, orientation, e);
        if (shell.Layout.HandleDrop(item, region, insertIndex))
        {
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private bool TryGetDraggedItem(DragEventArgs e, out BlockItemViewModel item)
    {
#pragma warning disable CS0618
        if (e.Data.Get(DragDataFormat) is BlockItemViewModel blockItem)
#pragma warning restore CS0618
        {
            item = blockItem;
            return true;
        }

        item = default!;
        return false;
    }

    private static ItemsControl? FindItemsControl(object? source)
    {
        return (source as Control)?.FindAncestorOfType<ItemsControl>();
    }

    private static int GetInsertIndex(ItemsControl control, Orientation orientation, DragEventArgs e)
    {
        if (e.Source is Control sourceControl)
        {
            var container = sourceControl.FindAncestorOfType<ContentPresenter>() ??
                            (sourceControl as ContentPresenter);
            if (container is not null)
            {
                var index = control.IndexFromContainer(container);
                if (index >= 0)
                {
                    var position = e.GetPosition(container);
                    var bounds = container.Bounds;
                    var insertAfter = orientation switch
                    {
                        Orientation.Horizontal => position.X > bounds.Width / 2,
                        Orientation.Vertical => position.Y > bounds.Height / 2,
                        _ => false,
                    };

                    return insertAfter ? index + 1 : index;
                }
            }
        }

        if (control.Items is IList list)
        {
            return list.Count;
        }

        if (control.Items is IEnumerable enumerable)
        {
            return enumerable.Cast<object>().Count();
        }

        return 0;
    }
}

