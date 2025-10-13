using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia;

public partial class ScenarioEditorWindow : Window
{
    public ScenarioEditorWindow(ScenarioEditorViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnSaveScenario(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ScenarioEditorViewModel viewModel)
        {
            return;
        }

        if (!viewModel.TryExportSelectedScenario(out var json, out var errorMessage))
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                viewModel.ReportExternalError(errorMessage);
            }

            return;
        }

        var dialog = new SaveFileDialog
        {
            DefaultExtension = "json",
            Filters =
            {
                new FileDialogFilter { Name = "JSON files", Extensions = { "json" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            }
        };

        var path = await dialog.ShowAsync(this);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            await File.WriteAllTextAsync(path, json);
            viewModel.ReportExternalStatus($"Scenario saved to '{Path.GetFileName(path)}'.");
        }
        catch (IOException ex)
        {
            viewModel.ReportExternalError($"Failed to save scenario: {ex.Message}");
        }
    }

    private async void OnLoadScenario(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ScenarioEditorViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters =
            {
                new FileDialogFilter { Name = "JSON files", Extensions = { "json" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            }
        };

        var result = await dialog.ShowAsync(this);
        if (result is null || result.Length == 0 || string.IsNullOrWhiteSpace(result[0]))
        {
            return;
        }

        var path = result[0];
        try
        {
            var json = await File.ReadAllTextAsync(path);
            if (!viewModel.TryImportScenarioJson(json, out var errorMessage) && !string.IsNullOrWhiteSpace(errorMessage))
            {
                viewModel.ReportExternalError(errorMessage);
            }
        }
        catch (IOException ex)
        {
            viewModel.ReportExternalError($"Failed to load scenario: {ex.Message}");
        }
    }
}
