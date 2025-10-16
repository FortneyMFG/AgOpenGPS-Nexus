using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.Views;
using Aog.UI.Avalonia.Views.FieldOperations;

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

        if (ResolveOwner() is not { } owner)
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

        if (ResolveOwner() is not { } owner)
        {
            return;
        }

        var window = new LegacyImportWizardWindow(viewModel.CreateLegacyImportWizardViewModel());
        await window.ShowDialog(owner);
    }

    private async void OnOpenBoundaryTool(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (ResolveOwner() is not { } owner)
        {
            return;
        }

        var window = new BoundaryToolDialog(viewModel.CreateBoundaryToolViewModel());
        await window.ShowDialog(owner);
    }

    private async void OnOpenFlagManager(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (ResolveOwner() is not { } owner)
        {
            return;
        }

        var window = new FlagManagerDialog(viewModel.CreateFlagManagerDialogViewModel());
        await window.ShowDialog(owner);
    }

    private async void OnOpenShiftPosition(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (ResolveOwner() is not { } owner)
        {
            return;
        }

        var window = new ShiftPositionDialog(viewModel.CreateShiftPositionDialogViewModel());
        await window.ShowDialog(owner);
    }

    private Window? ResolveOwner()
    {
        if (this.FindAncestorOfType<Window>() is { } owner)
        {
            return owner;
        }

        return TopLevel.GetTopLevel(this) as Window;
    }
}
