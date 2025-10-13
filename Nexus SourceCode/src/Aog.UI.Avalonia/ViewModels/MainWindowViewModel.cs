using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aog.Core.Replay;
using Aog.Core.Simulation;
using Aog.Core.Simulation.Configuration;
using Aog.Core.V1;
using Aog.UI.Avalonia.Models;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.Theming;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides presentation data for the bootstrap shell window.
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private const string SimulationResourceName = "Aog.UI.Avalonia.Resources.SimulationSample.json";

    private readonly ConnectionSettingsViewModel _connectionSettings;
    private readonly List<SimulationScenarioConfiguration> _scenarioDefinitions = new();
    private readonly SimulationConfiguration? _simulationConfiguration;
    private readonly IUiPreferencesService _preferencesService;
    private readonly IThemeManager _themeManager;

    private UiTheme _selectedTheme;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="connectionSettings">Connection settings view-model injected from DI.</param>
    /// <param name="replayController">Optional replay controller for transport control.</param>
    /// <param name="preferencesService">Service for persisting UI preferences.</param>
    /// <param name="themeManager">The theme manager used to apply theme changes.</param>
    /// <param name="telemetryPrivacy">Telemetry opt-in view-model.</param>
    public MainWindowViewModel(
        ConnectionSettingsViewModel connectionSettings,
        IReplayController? replayController,
        IUiPreferencesService preferencesService,
        IThemeManager themeManager,
        TelemetryPrivacyViewModel telemetryPrivacy)
    {
        ArgumentNullException.ThrowIfNull(connectionSettings);
        ArgumentNullException.ThrowIfNull(preferencesService);
        ArgumentNullException.ThrowIfNull(themeManager);
        ArgumentNullException.ThrowIfNull(telemetryPrivacy);
        _connectionSettings = connectionSettings;
        _preferencesService = preferencesService;
        _themeManager = themeManager;

        TelemetryPrivacy = telemetryPrivacy;

        Title = "AgOpenGPS Nexus";
        PlatformDescription =
            $"Running on {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture}) with {RuntimeInformation.FrameworkDescription}";

        // Load simulation configuration + summary and create the bar VM.
        var configuration = TryLoadSimulationConfiguration(out var summary);
        _simulationConfiguration = configuration;
        SimulationGraphSummary = summary;
        SimulationBar = new SimulationBarViewModel(configuration, replayController);

        SteerPanel = new SteerPanelViewModel();
        SectionsPanel = new SectionsPanelViewModel();
        PlanterPanel = new PlanterPanelViewModel();
        ApplySamplePluginState();

        if (configuration?.Scenarios is not null)
        {
            _scenarioDefinitions.AddRange(configuration.Scenarios);
        }

        AvailableThemes = Enum.GetValues<UiTheme>();
        var preferences = _preferencesService.GetPreferences();
        _selectedTheme = preferences.Theme;
        _themeManager.ApplyTheme(_selectedTheme);
    }

    /// <summary>Raised when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the title displayed in the main window.</summary>
    public string Title { get; }

    /// <summary>Gets a description of the runtime platform.</summary>
    public string PlatformDescription { get; }

    /// <summary>Gets a sample vehicle pose used to seed the map view.</summary>
    public VehiclePose VehiclePose { get; } = new(10, 15, 45);

    /// <summary>Gets the connection settings view-model.</summary>
    public ConnectionSettingsViewModel Connection => _connectionSettings;

    /// <summary>Gets a summary of the embedded simulation configuration.</summary>
    public string SimulationGraphSummary { get; }

    /// <summary>Gets the simulation bar view-model bound to the UI.</summary>
    public SimulationBarViewModel SimulationBar { get; }

    /// <summary>Gets the view-model describing the steer panel.</summary>
    public SteerPanelViewModel SteerPanel { get; }

    /// <summary>Gets the view-model describing the sections panel.</summary>
    public SectionsPanelViewModel SectionsPanel { get; }

    /// <summary>Gets the view-model describing the planter panel.</summary>
    public PlanterPanelViewModel PlanterPanel { get; }

    /// <summary>Gets the telemetry privacy view-model.</summary>
    public TelemetryPrivacyViewModel TelemetryPrivacy { get; }

    /// <summary>Gets the available UI themes.</summary>
    public IReadOnlyList<UiTheme> AvailableThemes { get; }

    /// <summary>Gets or sets the currently selected UI theme.</summary>
    public UiTheme SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (value == _selectedTheme)
            {
                return;
            }

            _selectedTheme = value;
            OnPropertyChanged();
            _preferencesService.UpdateTheme(value);
            _themeManager.ApplyTheme(value);
        }
    }

    /// <summary>
    /// Creates a scenario editor view-model that can update the simulation routes.
    /// </summary>
    public ScenarioEditorViewModel CreateScenarioEditorViewModel()
    {
        return new ScenarioEditorViewModel(
            _simulationConfiguration,
            _scenarioDefinitions,
            scenario => SimulationBar.ApplyScenario(scenario),
            () => SimulationBar.ResetToConfigurationRoutes());
    }

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

    private void ApplySamplePluginState()
    {
        var steerSample = new SteerCmd
        {
            Enable = true,
            TargetWheelAngleDeg = 2.5,
            FeedForward = 0.18,
            ControllerOutput = 0.42,
        };
        SteerPanel.ApplySteerCommand(steerSample);

        var sectionSample = new SectionMask
        {
            SectionCount = 8,
            Mask = 0b0011_1100,
        };
        SectionsPanel.ApplySectionMask(sectionSample);

        var planterSamples = new[]
        {
            new PlanterRowStatus
            {
                RowIndex = 0,
                TargetPopulationPerMeter = 10.0,
                ActualPopulationPerMeter = 10.1,
                SkipRate = 0.0,
                DoubleRate = 0.0,
                Quality = PlanterRowQuality.Ok,
            },
            new PlanterRowStatus
            {
                RowIndex = 1,
                TargetPopulationPerMeter = 10.0,
                ActualPopulationPerMeter = 9.1,
                SkipRate = 0.15,
                DoubleRate = 0.0,
                Quality = PlanterRowQuality.Skip,
            },
            new PlanterRowStatus
            {
                RowIndex = 2,
                TargetPopulationPerMeter = 10.0,
                ActualPopulationPerMeter = 10.8,
                SkipRate = 0.0,
                DoubleRate = 0.2,
                Quality = PlanterRowQuality.Double,
            },
            new PlanterRowStatus
            {
                RowIndex = 3,
                TargetPopulationPerMeter = 10.0,
                ActualPopulationPerMeter = 10.0,
                SkipRate = 0.0,
                DoubleRate = 0.0,
                Quality = PlanterRowQuality.Ok,
            },
        };
        PlanterPanel.ApplyRowStatuses(planterSamples);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
