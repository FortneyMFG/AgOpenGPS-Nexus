using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aog.Core.Layers;
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

        var layerEditJournal = new LayerEditEventJournalService(TimeProvider.System);
        ZoneEditorToolbar = new ZoneEditorToolbarViewModel(layerEditJournal);

        _mapLayers = BuildSampleLayers();
        _guidanceTracks = BuildSampleGuidance();
        LayerLegend = LayerLegendViewModel.FromLayers(_mapLayers);
        LayerInspector = BuildSampleInspector(_mapLayers);

        ProfitAnalytics = new ProfitAnalyticsViewModel();
        FieldHealthPanel = new FieldHealthPanelViewModel();

        ApplySamplePluginState();
        SeedDashboards();
        SeedProfitAnalytics();
        SeedFieldHealthPanel();

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

    /// <summary>Gets the zone editor toolbar view-model powering map editing affordances.</summary>
    public ZoneEditorToolbarViewModel ZoneEditorToolbar { get; }
    /// <summary>Gets the legend describing the active map layers.</summary>
    public LayerLegendViewModel LayerLegend { get; }

    /// <summary>Gets the inspector exposing the pinned layer observation.</summary>
    public LayerInspectorViewModel LayerInspector { get; }

    /// <summary>Gets the profitability analytics view-model.</summary>
    public ProfitAnalyticsViewModel ProfitAnalytics { get; }

    /// <summary>Gets the field health severity panel view-model.</summary>
    public FieldHealthPanelViewModel FieldHealthPanel { get; }

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

    private void SeedProfitAnalytics()
    {
        var snapshot = new ProfitAnalyticsSnapshot(
            "Field: North 40 • Season: Spring Wheat 2024",
            new DateTimeOffset(2025, 4, 3, 15, 45, 0, TimeSpan.Zero),
            "Profitability remains positive with strong headland performance and solid crop response on the west terrace.",
            "Loss zones exceed USD 75 per hectare near the south drainage low spots; inspect tile flow and standing water.",
            new[]
            {
                new ProfitCurrencySnapshot("USD", 48250m, 31210m, 17040m),
                new ProfitCurrencySnapshot("CAD", 52000m, 36680m, 15320m),
            },
            new[]
            {
                new ProfitHotspotSnapshot("North headland", "USD", 145m, 3.2, "High-yield passes with modest input costs."),
                new ProfitHotspotSnapshot("South drainage low", "USD", -85m, 2.1, "Waterlogged soil increased disease pressure."),
                new ProfitHotspotSnapshot("West terrace", "USD", 92m, 1.4, "Variable-rate nitrogen improved margins."),
            });

        ProfitAnalytics.ApplySnapshot(snapshot);
    }

    private void SeedFieldHealthPanel()
    {
        var snapshot = new FieldHealthPanelSnapshot(
            "Field health — risk.weeds",
            "Field: North 40 • Crop: Spring Wheat",
            12.6,
            new DateTimeOffset(2025, 4, 2, 15, 30, 0, TimeSpan.Zero),
            "Scout mapped weed pressure around drainage and terrace transitions.",
            "Critical compaction observed near south entrance; schedule remediation before planting window closes.",
            activeCount: 3,
            monitorCount: 2,
            resolvedCount: 1,
            showActive: true,
            showMonitor: true,
            showResolved: false,
            SeverityBuckets: new[]
            {
                new FieldHealthSeverityBucketSnapshot("None", 4, 2.4, "Surveyed clear of issues."),
                new FieldHealthSeverityBucketSnapshot("Low", 3, 4.1, "Light volunteer pressure along headland."),
                new FieldHealthSeverityBucketSnapshot("Moderate", 2, 3.0, "Patches respond to post-emerge chemistry."),
                new FieldHealthSeverityBucketSnapshot("High", 1, 1.7, "Canopy gaps tied to drainage rutting."),
                new FieldHealthSeverityBucketSnapshot("Critical", 1, 1.4, "Compaction hotspot with standing water."),
            },
            HistoryEntries: new[]
            {
                new FieldHealthHistoryEntrySnapshot(new DateTimeOffset(2025, 4, 1, 13, 10, 0, TimeSpan.Zero), "Moderate", "Monitor", "Initial scouting pass tagged spray follow-up."),
                new FieldHealthHistoryEntrySnapshot(new DateTimeOffset(2025, 4, 1, 16, 45, 0, TimeSpan.Zero), "High", "Active", "Compaction rut flagged for tillage crew."),
                new FieldHealthHistoryEntrySnapshot(new DateTimeOffset(2025, 4, 2, 10, 5, 0, TimeSpan.Zero), "Critical", "Active", "Waterlogged entrance; blocked drainage noted."),
                new FieldHealthHistoryEntrySnapshot(new DateTimeOffset(2025, 4, 2, 12, 20, 0, TimeSpan.Zero), "Low", "Resolved", "North headland weeds sprayed and verified."),
                new FieldHealthHistoryEntrySnapshot(new DateTimeOffset(2025, 4, 2, 15, 30, 0, TimeSpan.Zero), "Moderate", "Monitor", "Follow-up drone imagery scheduled."),
            });

        FieldHealthPanel.ApplySnapshot(snapshot);
    }

    private static IReadOnlyList<MapLayer> BuildSampleLayers()
    {
        const double spacing = 6;
        const double size = 5.5;

        var actualCells = new List<MapLayerCell>();
        var plannedCells = new List<MapLayerCell>();
        var profitCells = new List<MapLayerCell>();
        var healthCells = new List<MapLayerCell>();

        for (var x = -3; x <= 3; x++)
        {
            for (var y = -2; y <= 4; y++)
            {
                var center = new Point(10 + x * spacing, 10 + y * spacing);
                var actualCoverage = Math.Clamp(0.15 + (y + 2) * 0.18 + Math.Sin(x * 0.7) * 0.05, 0, 1);
                var plannedCoverage = Math.Clamp(0.3 + (y + 1) * 0.14 + Math.Cos(x * 0.5) * 0.07, 0, 1);
                var profitValue = Math.Clamp((Math.Cos(x * 0.45) * 180) + (Math.Sin((y + 1) * 0.55) * 120) - (y * 28), -260, 320);
                var severityScore = Math.Clamp((int)Math.Round(2 + Math.Sin(x * 0.6) + Math.Cos((y + 1) * 0.4)), 0, 4);

                actualCells.Add(new MapLayerCell(center, size, actualCoverage));
                plannedCells.Add(new MapLayerCell(center, size, plannedCoverage));
                profitCells.Add(new MapLayerCell(center, size, profitValue));
                healthCells.Add(new MapLayerCell(center, size, severityScore));
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

        var profitStyle = new LayerVisualizationStyle(
            Color.FromArgb(220, 192, 57, 43),
            Color.FromArgb(220, 34, 139, 34),
            -250,
            300,
            units: "USD/ha",
            outlineColor: Color.FromArgb(150, 46, 64, 82));

        var fieldHealthStyle = new LayerVisualizationStyle(
            Color.FromArgb(220, 46, 204, 113),
            Color.FromArgb(230, 231, 76, 60),
            0,
            4,
            units: "severity",
            outlineColor: Color.FromArgb(160, 52, 73, 94));

        return new List<MapLayer>
        {
            new("layer:coverage.actual", "Actual coverage", actualStyle, actualCells, description: "Live rate samples aggregated from the section controller."),
            new("layer:coverage.planned", "Planned rate", plannedStyle, plannedCells, isVisible: true, description: "Target metadata sourced from the prescription controller."),
            new("layer:profit.net", "Profit heatmap", profitStyle, profitCells, description: "Net profit per hectare derived from cost and yield analytics."),
            new("layer:risk.weeds", "Field health severity", fieldHealthStyle, healthCells, description: "Scouting observations colour-coded by severity."),
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

    private static LayerInspectorViewModel BuildSampleInspector(IReadOnlyList<MapLayer> layers)
    {
        if (layers is null || layers.Count == 0)
        {
            return new LayerInspectorViewModel("layer:sample", "Sample layer", isPlanned: false);
        }

        var layer = layers.FirstOrDefault(l => string.Equals(l.LayerId, "layer:profit.net", StringComparison.OrdinalIgnoreCase))
            ?? layers[0];
        var description = layer.Style.IsPlanned
            ? "Pinned observation sourced from the prescription metadata."
            : "Pinned observation using the metadata-driven inspector.";
        var inspector = new LayerInspectorViewModel(layer.LayerId, layer.DisplayName, layer.Style.IsPlanned, description);

        var cell = layer.Cells.Count > 0
            ? layer.Cells[Math.Min(3, layer.Cells.Count - 1)]
            : new MapLayerCell(new Point(0, 0), 1, layer.Style.MinimumValue);

        var valueFormat = layer.LayerId == "layer:profit.net"
            ? "{0:+$0.##;- $0.##;$0.00}"
            : string.Equals(layer.Style.Units, "fraction", StringComparison.OrdinalIgnoreCase)
                ? "{0:P1}"
                : "{0:0.##}";
        var units = layer.Style.Units;
        var targetValue = layer.Style.IsPlanned
            ? (double?)null
            : Math.Min(layer.Style.MaximumValue, cell.Value + 0.08);
        var timestamp = new DateTimeOffset(2025, 4, 2, 16, 12, 0, TimeSpan.Zero);
        var sourceDisplay = layer.LayerId == "layer:profit.net"
            ? "Profit analytics pipeline"
            : layer.Style.IsPlanned ? "Prescription catalog" : "Section Control (tractor-01)";

        static string FormatForMetadata(double value, string format, string? units)
        {
            var formatted = string.Format(CultureInfo.InvariantCulture, format, value);
            return string.IsNullOrWhiteSpace(units) ? formatted : string.Concat(formatted, " ", units);
        }

        var transportMetadata = layer.LayerId == "layer:profit.net"
            ? new[]
            {
                new KeyValuePair<string, string>("Generated by", "ProfitAnalyticsRollupService"),
                new KeyValuePair<string, string>("Yield layer", "layer:yield.normalized"),
                new KeyValuePair<string, string>("Cost scope", "session:planting-2024-04-02"),
            }
            : new[]
            {
                new KeyValuePair<string, string>("PGN", "0xEF00"),
                new KeyValuePair<string, string>("CAN ID", "0x1CEBFF02"),
                new KeyValuePair<string, string>("Rate mode", layer.Style.IsPlanned ? "Preset (open-loop)" : "Closed-loop"),
            };

        var payloadMetadata = layer.LayerId == "layer:profit.net"
            ? new[]
            {
                new KeyValuePair<string, string>("Revenue component", "$1,245.68 / ha"),
                new KeyValuePair<string, string>("Cost component", "$884.50 / ha"),
                new KeyValuePair<string, string>("Margin", "+$361.18 / ha"),
            }
            : new[]
            {
                new KeyValuePair<string, string>("Raw bytes", "2A FF 19 40 00 00 7C 3F"),
                new KeyValuePair<string, string>("Decoded rate", FormatForMetadata(cell.Value, valueFormat, units)),
                new KeyValuePair<string, string>("Section mask", "0b0011_1100"),
            };

        inspector.ApplyObservation(
            cell.Value,
            valueFormat,
            units,
            targetValue,
            layer.LayerId == "layer:profit.net" ? "Confidence 0.82 (Interpolated)" : "Quality 0.97 (Good)",
            weight: layer.LayerId == "layer:profit.net" ? 0.67 : 0.82,
            isRateUnavailable: false,
            cell.Center,
            timestamp,
            sourceDisplay,
            transportMetadata,
            payloadMetadata);

        return inspector;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
