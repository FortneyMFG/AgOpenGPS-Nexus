using System;
using Aog.UI.Avalonia.ViewModels.Shell;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class AppShellView : UserControl
{
    public AppShellView()
    {
        InitializeComponent();
    }

    private void OnFieldSettingsMenuClosed(object? sender, EventArgs e)
    {
        if (DataContext is AppShellViewModel shell)
        {
            shell.IsFieldMenuOpen = false;
        }
    }

    private void OnFieldSettingsMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AppShellViewModel shell)
        {
            shell.IsFieldMenuOpen = false;
        }
    }
}
