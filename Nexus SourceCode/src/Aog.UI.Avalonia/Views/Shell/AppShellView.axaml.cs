using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.Views;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class AppShellView : UserControl
{
    public AppShellView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnOpenScenarioEditor(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (this.FindAncestorOfType<Window>() is not { } owner)
        {
            owner = TopLevel.GetTopLevel(this) as Window;
        }

        if (owner is null)
        {
            return;
        }

        var window = new ScenarioEditorWindow(viewModel.CreateScenarioEditorViewModel());
        await window.ShowDialog(owner);
    }

    private async void OnOpenLegacyImportWizard(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (this.FindAncestorOfType<Window>() is not { } owner)
        {
            owner = TopLevel.GetTopLevel(this) as Window;
        }

        if (owner is null)
        {
            return;
        }

        var window = new LegacyImportWizardWindow(viewModel.CreateLegacyImportWizardViewModel());
        await window.ShowDialog(owner);
    }
}
