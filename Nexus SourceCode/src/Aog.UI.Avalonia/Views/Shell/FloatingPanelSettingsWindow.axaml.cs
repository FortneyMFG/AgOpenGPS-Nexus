using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels.Shell;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class FloatingPanelSettingsWindow : Window
{
    public FloatingPanelSettingsWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FloatingPanelSettingsDialogViewModel viewModel)
        {
            viewModel.Apply();
            Close(true);
        }
        else
        {
            Close(false);
        }
    }
}
