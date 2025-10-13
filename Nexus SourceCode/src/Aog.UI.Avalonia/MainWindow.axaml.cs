using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
