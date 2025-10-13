using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia;

public partial class LegacyImportWizardWindow : Window
{
    public LegacyImportWizardWindow(LegacyImportWizardViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnBrowseAbLines(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LegacyImportWizardViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters =
            {
                new FileDialogFilter { Name = "CSV files", Extensions = { "csv" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } },
            },
        };

        var result = await dialog.ShowAsync(this);
        if (result is { Length: > 0 })
        {
            viewModel.SetAbLineCsvPath(result[0]);
        }
    }

    private async void OnBrowseBoundary(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LegacyImportWizardViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters =
            {
                new FileDialogFilter { Name = "Shapefiles", Extensions = { "shp" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } },
            },
        };

        var result = await dialog.ShowAsync(this);
        if (result is { Length: > 0 })
        {
            viewModel.SetBoundaryShapePath(result[0]);
        }
    }

    private async void OnImport(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LegacyImportWizardViewModel viewModel)
        {
            return;
        }

        await viewModel.ImportAsync().ConfigureAwait(true);
    }

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LegacyImportWizardViewModel viewModel)
        {
            return;
        }

        if (viewModel.TryApplyRoutes())
        {
            Close();
        }
    }
}
