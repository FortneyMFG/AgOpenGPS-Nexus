using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels.Shell;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class BlockSettingsWindow : Window
{
    public BlockSettingsWindow()
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

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BlockSettingsDialogViewModel viewModel)
        {
            viewModel.Apply();
        }

        Close(true);
    }
}
