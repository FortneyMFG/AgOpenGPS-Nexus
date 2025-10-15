using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aog.Core.Legacy;
using Aog.Core.Replay;
using Aog.Core.Simulation;
using Aog.Core.Simulation.Configuration;
using Aog.Core.V1;
using Aog.UI.Avalonia.Models;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.Theming;
using Avalonia;
using Avalonia.Media;

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

    private readonly IReadOnlyList<MapLayer> _mapLayers;
    private readonly IReadOnlyList<GuidanceTrack> _guidanceTracks;

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

        SeasonNavigator = SeasonNavigatorViewModel.CreateSample();

        // Load simulation configuration + summary and create the bar VM.
        var configuration = TryLoadSimulationConfiguration(out var summary);
        _simulationConfiguration = configuration;
        SimulationGraphSummary = summary;
        SimulationBar = new SimulationBarViewModel(configuration, replayController);

        SteerPanel = new SteerPanelViewModel();
        SteerDashboard = new SteerDashboardViewModel();
        SectionsPanel = new SectionsPanelViewModel();
        PlanterPanel = new PlanterPanelViewModel();
        ReplayTimeline = new ReplayTimelineViewModel();

        _mapLayers = BuildSampleLayers();
        _guidanceTracks = BuildSampleGuidance();

        ApplySamplePluginState();
        SeedDashboards();

        if (configuration?.Scenarios is not null)
        {
            _scenarioDefinitions.AddRange(configuration.Scenarios);
        }

        // Theme bootstrapping
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

    /// <summary>Gets the dashboard view-model that surfaces steering history.</summary>
    public SteerDashboardViewModel SteerDashboard { get; }

    /// <summary>Gets the view-model describing the sections panel.</summary>
    public SectionsPanelViewModel SectionsPanel { get; }

    /// <summary>Gets the view-model describing the planter panel.</summary>
    public PlanterPanelViewModel PlanterPanel { get; }

    /// <summary>Gets the telemetry privacy view-model.</summary>
    public TelemetryPrivacyViewModel TelemetryPrivacy { get; }

    /// <summary>Gets the season navigator view-model.</summary>
    public SeasonNavigatorViewModel SeasonNavigator { get; }

    /// <summary>Gets the available UI themes.</summary>
    public IReadOnlyList<UiTheme> AvailableThemes { get; }

    /// <summary>Gets or sets the currently selected UI theme.</summary>
    public UiTheme SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (value == _selectedTheme) return;
            _selectedTheme = value;
            OnPropertyChanged();
            _preferencesService.UpdateTheme(value);
            _themeManager.ApplyTheme(value);
        }
    }

    /// <summary>Gets the replay timeline analytics view-model.</summary>
    public ReplayTimelineViewModel ReplayTimeline { get; }

    /// <summary>Gets the map layers rendered on the map.</summary>
    public IReadOnlyList<MapLayer> MapLayers => _mapLayers;

    /// <summary>Gets the guidance tracks rendered on the map.</summary>
    public IReadOnlyList<GuidanceTrack> GuidanceTracks => _guidanceTracks;

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

    /// <summary>
    /// Creates a legacy import wizard view-model wired to update the simulation routes.
    /// </summary>
    public LegacyImportWizardViewModel CreateLegacyImportWizardViewModel()
    {
        var service = new LegacyGuidanceImportService();
        return new LegacyImportWizardViewModel(service, result =>
        {
            SimulationBar.ApplyLegacyImport(result);
            return true;
        });
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
        SteerDashboard.UpdateCommandedAngle(steerSample.TargetWheelAngleDeg);

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

    private void SeedDashboards()
    {
        var crossTrack = Enumerable.Range(0, 60)
            .Select(index => Math.Sin(index / 8.0) * 0.12 + (index / 60.0 - 0.5) * 0.02)
            .ToArray();
        var wheelAngles = Enumerable.Range(0, crossTrack.Length)
            .Select(index => Math.Sin(index / 5.0) * 7.5)
            .ToArray();
        var controllerOutputs = Enumerable.Range(0, crossTrack.Length)
            .Select(index => Math.Clamp(Math.Sin(index / 6.0) * 0.5 + 0.5, -1, 1))
            .ToArray();

        SteerDashboard.ApplyHistoricalSamples(crossTrack, wheelAngles, controllerOutputs);
        SteerDashboard.RecordTuningEvent(DateTimeOffset.Now.AddMinutes(-5), "Adjusted P gain to 0.30 based on headland drift");
        SteerDashboard.RecordTuningEvent(DateTimeOffset.Now.AddMinutes(-2), "Applied adaptive integral clamp after curve pass");
        SteerDashboard.RecordTuningEvent(DateTimeOffset.Now.AddMinutes(-1), "Saved preset 'Spring Wheat 2024'");

        var bookmarks = new[]
        {
            new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(45), "Headland turn", "Operator nudged wheel to re-align"),
            new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(120), "Section skip", "Section 3 masked due to obstacle"),
            new ReplayTimelineBookmarkViewModel(TimeSpan.FromSeconds(210), "AB reacquire", "Auto steer re-locked onto AB line"),
        };

        var speeds = Enumerable.Range(0, 120).Select(i => 6 + Math.Sin(i / 9.0) * 0.8).ToArray();
        var headings = Enumerable.Range(0, 120).Select(i => Math.Sin(i / 15.0) * 15).ToArray();
        ReplayTimeline.ApplySampleData(speeds, headings, bookmarks);
    }

    private static IReadOnlyList<MapLayer> BuildSampleLayers()
    {
        const double spacing = 6;
        const double size = 5.5;

        var actualCells = new List<MapLayerCell>();
        var plannedCells = new List<MapLayerCell>();

        for (var x = -3; x <= 3; x++)
        {
            for (var y = -2; y <= 4; y++)
            {
                var center = new Point(10 + x * spacing, 10 + y * spacing);
                var actualCoverage = Math.Clamp(0.15 + (y + 2) * 0.18 + Math.Sin(x * 0.7) * 0.05, 0, 1);
                var plannedCoverage = Math.Clamp(0.3 + (y + 1) * 0.14 + Math.Cos(x * 0.5) * 0.07, 0, 1);

                actualCells.Add(new MapLayerCell(center, size, actualCoverage));
                plannedCells.Add(new MapLayerCell(center, size, plannedCoverage));
            }
        }

        var actualStyle = new LayerVisualizationStyle(
            Color.FromArgb(220, 34, 139, 34),
            Color.FromArgb(230, 17, 201, 141),
            0,
            1,
            units: "fraction",
            outlineColor: Color.FromArgb(160, 33, 46, 51));

        var plannedStyle = new LayerVisualizationStyle(
            Color.FromArgb(160, 30, 64, 174),
            Color.FromArgb(200, 255, 215, 0),
            0,
            1,
            units: "fraction",
            isPlanned: true,
            outlineColor: Color.FromArgb(120, 24, 34, 84));

        return new List<MapLayer>
        {
            new("layer:coverage.actual", "Actual coverage", actualStyle, actualCells),
            new("layer:coverage.planned", "Planned rate", plannedStyle, plannedCells),
        };
    }

    private static IReadOnlyList<GuidanceTrack> BuildSampleGuidance()
    {
        var abLinePoints = Enumerable.Range(-10, 25)
            .Select(i => new Point(10 + i * 2.5, 40))
            .ToList();
        var currentPassPoints = Enumerable.Range(0, 18)
            .Select(i => new Point(10 + i * 2.4, 12 + Math.Sin(i / 3.0) * 1.4))
            .ToList();
        var boundaryPoints = new List<Point>
        {
            new(-10, -10),
            new(50, -8),
            new(52, 42),
            new(-12, 40),
            new(-10, -10),
        };

        return new List<GuidanceTrack>
        {
            new("AB Reference", abLinePoints, Color.FromArgb(200, 55, 161, 255), 2.5),
            new("Current pass", currentPassPoints, Color.FromArgb(230, 255, 215, 0), 3),
            new("Boundary", boundaryPoints, Color.FromArgb(180, 255, 86, 48), 2),
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
