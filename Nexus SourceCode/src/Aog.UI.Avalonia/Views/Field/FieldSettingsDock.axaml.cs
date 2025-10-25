using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Aog.UI.Avalonia.ViewModels.Shell;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Aog.UI.Avalonia.Views.Field;

public partial class FieldSettingsDock : UserControl
{
    private const double LauncherDragThreshold = 6d;
    internal const string LauncherDragDataFormat = "Aog.Nexus.BlockLauncher";

    private Point _dragStart;
    private bool _isPressArmed;
    private bool _isDragActive;
    private BlockLauncherItemViewModel? _dragLauncher;

    public FieldSettingsDock()
    {
        InitializeComponent();
    }

    private void OnLauncherPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        if (control.DataContext is not BlockLauncherItemViewModel launcher)
        {
            return;
        }

        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (DataContext is not BlockLayoutViewModel layout || layout.IsLocked)
        {
            return;
        }

        if (IsPointerOverInteractiveChild(e.Source))
        {
            return;
        }

        _dragLauncher = launcher;
        _dragStart = e.GetPosition(control);
        _isPressArmed = true;
        _isDragActive = false;
        e.Pointer.Capture(control);
    }

    private async void OnLauncherPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPressArmed || sender is not Control control)
        {
            return;
        }

        if (_dragLauncher is null)
        {
            ResetLauncherDrag(control, e.Pointer);
            return;
        }

        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            ResetLauncherDrag(control, e.Pointer);
            return;
        }

        if (_isDragActive)
        {
            return;
        }

        var current = e.GetPosition(control);
        var delta = current - _dragStart;
        if (Math.Abs(delta.X) < LauncherDragThreshold && Math.Abs(delta.Y) < LauncherDragThreshold)
        {
            return;
        }

        _isDragActive = true;
        e.Handled = true;
        await StartLauncherDragAsync(control, e);
    }

    private void OnLauncherPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragActive)
        {
            return;
        }

        if (sender is Control control)
        {
            ResetLauncherDrag(control, e.Pointer);
        }
        else
        {
            ResetLauncherDrag(null, e.Pointer);
        }
    }

    private void OnLauncherPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isDragActive)
        {
            return;
        }

        ResetLauncherDrag(sender as Control, e.Pointer);
    }

    private static bool IsPointerOverInteractiveChild(object? source)
    {
        if (source is null)
        {
            return false;
        }

        if (source is Button)
        {
            return true;
        }

        if (source is Visual visual)
        {
            return visual.GetVisualAncestors().OfType<Button>().Any();
        }

        return false;
    }

    private async Task StartLauncherDragAsync(Control source, PointerEventArgs e)
    {
        if (_dragLauncher is null || DataContext is not BlockLayoutViewModel layout)
        {
            ResetLauncherDrag(source, e.Pointer);
            return;
        }

        if (!layout.CanLaunch(_dragLauncher))
        {
            ResetLauncherDrag(source, e.Pointer);
            return;
        }

        var data = LauncherDragData.CreatePayload(_dragLauncher, out var token);

        layout.BeginLauncherDrag();

        try
        {
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy);
        }
        finally
        {
            LauncherDragData.ReleasePayload(token);
            layout.EndLauncherDrag();
            ResetLauncherDrag(source, e.Pointer);
        }
    }

    private void ResetLauncherDrag(Control? source, IPointer? pointer)
    {
        if (source is not null && pointer is not null && pointer.Captured == source)
        {
            pointer.Capture(null);
        }

        _dragLauncher = null;
        _isPressArmed = false;
        _isDragActive = false;
    }
}

internal static class LauncherDragData
{
    private static readonly ConcurrentDictionary<string, BlockLauncherItemViewModel> ActiveLaunchers = new();
    private static readonly DataFormat<string> LauncherFormat = DataFormat.CreateStringApplicationFormat(FieldSettingsDock.LauncherDragDataFormat);

    public static DataTransfer CreatePayload(BlockLauncherItemViewModel launcher, out string token)
    {
        token = Guid.NewGuid().ToString("N");
        ActiveLaunchers[token] = launcher;

        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.Create(LauncherFormat, token));
        return transfer;
    }

    public static bool TryGetLauncher(IDataTransfer data, out BlockLauncherItemViewModel? launcher)
    {
        launcher = null;

        if (data is null)
        {
            return false;
        }

        foreach (var item in data.Items)
        {
            if (item.TryGetRaw(LauncherFormat) is string token && ActiveLaunchers.TryGetValue(token, out var candidate))
            {
                launcher = candidate;
                return true;
            }
        }

        return false;
    }

    public static void ReleasePayload(string token)
    {
        ActiveLaunchers.TryRemove(token, out _);
    }
}
