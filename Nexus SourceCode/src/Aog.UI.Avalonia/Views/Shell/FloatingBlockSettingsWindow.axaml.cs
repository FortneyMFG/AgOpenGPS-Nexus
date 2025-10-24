using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels.Shell;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class FloatingBlockSettingsWindow : Window
{
    public FloatingBlockSettingsWindow()
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
        if (DataContext is FloatingBlockSettingsDialogViewModel viewModel)
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
