using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Aog.UI.Avalonia.Controls;
using Aog.UI.Avalonia.ViewModels.Shell;
using Aog.UI.Avalonia.Views.Field;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class AppShellView : UserControl
{
    private TiledPanel? _tilePanel;

    public AppShellView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is AppShellViewModel shell)
        {
            shell.Layout.UpdateViewport(Bounds.Size);
        }
    }

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is AppShellViewModel shell)
        {
            shell.Layout.UpdateViewport(e.NewSize);
        }
    }

    private void OnWorkspaceDragOver(object? sender, DragEventArgs e)
    {
        if (!TryGetShell(out var shell) || !TryGetLauncher(e.DataTransfer, out var launcher) || !shell.Layout.CanLaunch(launcher))
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnWorkspaceDrop(object? sender, DragEventArgs e)
    {
        if (!TryGetShell(out var shell) || !TryGetLauncher(e.DataTransfer, out var launcher))
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var layout = shell.Layout;
        if (!layout.CanLaunch(launcher))
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var panel = GetTilePanel();
        if (panel?.Layout is not { } grid)
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var position = e.GetPosition(panel);
        var (colSpan, rowSpan) = layout.GetGridSpan(launcher);
        var (column, row) = grid.SnapToGrid(position, colSpan, rowSpan);
        layout.Launch(launcher, column, row);

        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private bool TryGetShell(out AppShellViewModel shell)
    {
        if (DataContext is AppShellViewModel model)
        {
            shell = model;
            return true;
        }

        shell = default!;
        return false;
    }

    private static bool TryGetLauncher(IDataTransfer? data, [NotNullWhen(true)] out BlockLauncherItemViewModel? launcher)
    {
        if (data is null)
        {
            launcher = null;
            return false;
        }

        return LauncherDragData.TryGetLauncher(data, out launcher) && launcher is not null;
    }

    private TiledPanel? GetTilePanel()
    {
        if (_tilePanel is { } panel && panel.IsAttachedToVisualTree())
        {
            return panel;
        }

        var tiles = this.FindControl<ItemsControl>("BlockTiles");
        panel = tiles?.GetVisualDescendants().OfType<TiledPanel>().FirstOrDefault();
        _tilePanel = panel;
        return panel;
    }
}
