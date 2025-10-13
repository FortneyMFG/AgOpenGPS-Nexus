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
    private static readonly string SimulationSummary = BuildSimulationGraphSummary();

    /// <summary>
    /// Gets the title displayed in the main window.
    /// </summary>
    public string Title => "AgOpenGPS Nexus";

    /// <summary>
    /// Gets a description of the platform the application is currently running on.
    /// </summary>
    public string PlatformDescription =>
        $"Running on {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture}) with {RuntimeInformation.FrameworkDescription}";

    /// <summary>
    /// Gets a summary of the embedded simulation configuration.
    /// </summary>
    public string SimulationGraphSummary => SimulationSummary;

    private static string BuildSimulationGraphSummary()
    {
        const string resourceName = "Aog.UI.Avalonia.Resources.SimulationSample.json";
        var assembly = typeof(MainWindowViewModel).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return "Simulation sample resource not found.";
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

            return catalog.BuildGraph().FormatSummary().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Failed to load simulation sample: {ex.Message}";
        }
    }
}
