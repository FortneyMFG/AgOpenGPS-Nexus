using System;
using System.IO;
using System.Runtime.InteropServices;
using Aog.UI.Avalonia.Models;
using Aog.Core.Simulation;
using Aog.Core.Simulation.Configuration;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides presentation data for the bootstrap shell window.
/// </summary>
public class MainWindowViewModel
{
    private readonly ConnectionSettingsViewModel _connectionSettings;
    private static readonly string SimulationSummary = BuildSimulationGraphSummary();

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="connectionSettings">The connection settings view-model to expose to the view.</param>
    public MainWindowViewModel(ConnectionSettingsViewModel connectionSettings)
    {
        ArgumentNullException.ThrowIfNull(connectionSettings);
        _connectionSettings = connectionSettings;
    }

    /// <summary>Gets the title displayed in the main window.</summary>
    public string Title => "AgOpenGPS Nexus";

    /// <summary>Gets a description of the runtime platform.</summary>
    public string PlatformDescription =>
        $"Running on {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture}) with {RuntimeInformation.FrameworkDescription}";

    /// <summary>Gets a sample vehicle pose used to seed the map view.</summary>
    public VehiclePose VehiclePose { get; } = new(10, 15, 45);

    /// <summary>Gets the connection settings view-model.</summary>
    public ConnectionSettingsViewModel Connection => _connectionSettings;

    /// <summary>Gets a summary of the embedded simulation configuration.</summary>
    public string SimulationGraphSummary => SimulationSummary;

    private static string BuildSimulationGraphSummary()
    {
        const string resourceName = "Aog.UI.Avalonia.Resources.SimulationSample.json";
        var assembly = typeof(MainWindowViewModel).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return "Simulation sample resource not found.";

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        try
        {
            var configuration = SimulationConfigurationLoader.Load(json);
            var catalog = new SimulationCatalog();
            foreach (var descriptor in configuration.CreateProviderDescriptors())
                catalog.Register(descriptor);

            return catalog.BuildGraph().FormatSummary().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Failed to load simulation sample: {ex.Message}";
        }
    }
}
