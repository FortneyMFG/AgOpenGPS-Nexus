using System;
using System.IO;
using System.Runtime.InteropServices;
using Aog.Core.Simulation;
using Aog.Core.Simulation.Configuration;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides presentation data for the bootstrap shell window.
/// </summary>
public class MainWindowViewModel
{
    private const string SimulationResourceName = "Aog.UI.Avalonia.Resources.SimulationSample.json";

    public MainWindowViewModel()
    {
        Title = "AgOpenGPS Nexus";
        PlatformDescription =
            $"Running on {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture}) with {RuntimeInformation.FrameworkDescription}";

        var configuration = TryLoadSimulationConfiguration(out var summary);
        SimulationGraphSummary = summary;
        SimulationBar = new SimulationBarViewModel(configuration);
    }

    /// <summary>
    /// Gets the title displayed in the main window.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets a description of the platform the application is currently running on.
    /// </summary>
    public string PlatformDescription { get; }

    /// <summary>
    /// Gets a summary of the embedded simulation configuration.
    /// </summary>
    public string SimulationGraphSummary { get; }

    /// <summary>
    /// Gets the simulation bar view-model bound to the UI.
    /// </summary>
    public SimulationBarViewModel SimulationBar { get; }

    private static SimulationConfiguration? TryLoadSimulationConfiguration(out string summary)
    {
        var assembly = typeof(MainWindowViewModel).Assembly;

        using var stream = assembly.GetManifestResourceStream(SimulationResourceName);
        if (stream is null)
        {
            summary = "Simulation sample resource not found.";
            return null;
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        try
        {
            var configuration = SimulationConfigurationLoader.Load(json);
            var catalog = new SimulationCatalog();
            foreach (var descriptor in configuration.CreateProviderDescriptors())
            {
                catalog.Register(descriptor);
            }

            summary = catalog.BuildGraph().FormatSummary().TrimEnd();
            return configuration;
        }
        catch (Exception ex)
        {
            summary = $"Failed to load simulation sample: {ex.Message}";
            return null;
        }
    }
}
