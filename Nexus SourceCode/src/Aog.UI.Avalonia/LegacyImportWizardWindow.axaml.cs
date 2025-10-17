using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
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

        if (StorageProvider is not { } storageProvider)
        {
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("CSV files") { Patterns = new[] { "*.csv" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*.*" } },
            },
        }).ConfigureAwait(true);

        if (files is { Count: > 0 } && files[0].Path?.LocalPath is { } path && !string.IsNullOrWhiteSpace(path))
        {
            viewModel.SetAbLineCsvPath(path);
        }
    }

    private async void OnBrowseBoundary(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LegacyImportWizardViewModel viewModel)
        {
            return;
        }

        if (StorageProvider is not { } storageProvider)
        {
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Shapefiles") { Patterns = new[] { "*.shp" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*.*" } },
            },
        }).ConfigureAwait(true);

        if (files is { Count: > 0 } && files[0].Path?.LocalPath is { } path && !string.IsNullOrWhiteSpace(path))
        {
            viewModel.SetBoundaryShapePath(path);
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
