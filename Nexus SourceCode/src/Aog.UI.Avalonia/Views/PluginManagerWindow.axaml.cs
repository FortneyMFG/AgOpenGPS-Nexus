using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views;

public partial class PluginManagerWindow : Window
{
    public PluginManagerWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is PluginManagerViewModel manager)
        {
            manager.Refresh();
        }
    }
}
