using System;
using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
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

        if (StorageProvider is not { } storageProvider)
        {
            return;
        }

        try
        {
            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*.*" } },
                },
            }).ConfigureAwait(true);

            if (file is null)
            {
                return;
            }

            await using var stream = await file.OpenWriteAsync().ConfigureAwait(true);
            await using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
            await writer.WriteAsync(json).ConfigureAwait(true);
            await writer.FlushAsync().ConfigureAwait(true);

            var savedPath = file.Path?.LocalPath;
            var displayName = !string.IsNullOrWhiteSpace(savedPath)
                ? Path.GetFileName(savedPath)
                : file.Name;
            viewModel.ReportExternalStatus($"Scenario saved to '{displayName}'.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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

        if (StorageProvider is not { } storageProvider)
        {
            return;
        }

        try
        {
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*.*" } },
                },
            }).ConfigureAwait(true);

            if (files is null || files.Count == 0)
            {
                return;
            }

            var file = files[0];

            await using var stream = await file.OpenReadAsync().ConfigureAwait(true);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            var json = await reader.ReadToEndAsync().ConfigureAwait(true);
            if (!viewModel.TryImportScenarioJson(json, out var errorMessage) && !string.IsNullOrWhiteSpace(errorMessage))
            {
                viewModel.ReportExternalError(errorMessage);
            }
        }
        catch (IOException ex)
        {
            viewModel.ReportExternalError($"Failed to load scenario: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            viewModel.ReportExternalError($"Failed to load scenario: {ex.Message}");
        }
    }
}
