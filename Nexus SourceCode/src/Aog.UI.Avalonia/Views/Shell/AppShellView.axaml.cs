using System;
using Aog.UI.Avalonia.ViewModels.Shell;
using Avalonia;
using Avalonia.Controls;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class AppShellView : UserControl
{
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
}
