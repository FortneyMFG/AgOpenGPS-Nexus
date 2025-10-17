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
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Models;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.Theming;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides presentation data for the bootstrap shell window.
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private const string SimulationResourceName = "Aog.UI.Avalonia.Resources.SimulationSample.json";

    private readonly ConnectionSettingsViewModel _connectionSettings;
    private readonly List<SimulationScenarioConfiguration> _scenarioDefinitions = new();
    private readonly SimulationConfiguration? _simulationConfiguration;
    private readonly IUiPreferencesService _preferencesService;
    private readonly IThemeManager _themeManager;
    private readonly ShellLayoutPreferences _shellLayout;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    private UiTheme _selectedTheme;

    private readonly IReadOnlyList<MapLayer> _mapLayers;
    private readonly IReadOnlyList<GuidanceTrack> _guidanceTracks;
    private bool _isTopToolbarVisible;
    private bool _isRightSidebarVisible;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="connectionSettings">Connection settings view-model injected from DI.</param>
    /// <param name="replayController">Optional replay controller for transport control.</param>
    /// <param name="preferencesService">Service for persisting UI preferences.</param>
    /// <param name="themeManager">The theme manager used to apply theme changes.</param>
    /// <param name="telemetryPrivacy">Telemetry opt-in view-model.</param>
    /// <param name="timeProvider">Provider used to generate deterministic timestamps.</param>
    public MainWindowViewModel(
        ConnectionSettingsViewModel connectionSettings,
        IReplayController? replayController,
        IUiPreferencesService preferencesService,
        IThemeManager themeManager,
        IShellCommandDispatcher commandDispatcher,
        TelemetryPrivacyViewModel telemetryPrivacy,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(connectionSettings);
        ArgumentNullException.ThrowIfNull(preferencesService);
        ArgumentNullException.ThrowIfNull(themeManager);
        ArgumentNullException.ThrowIfNull(commandDispatcher);
        ArgumentNullException.ThrowIfNull(telemetryPrivacy);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _connectionSettings = connectionSettings;
        _preferencesService = preferencesService;
        _themeManager = themeManager;
        _timeProvider = timeProvider;

        TelemetryPrivacy = telemetryPrivacy;

        Title = "AgOpenGPS Nexus";
        PlatformDescription =
            $"Running on {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture}) with {RuntimeInformation.FrameworkDescription}";

        SeasonNavigator = SeasonNavigatorViewModel.CreateSample();
        CropQuickSelect = CropQuickSelectViewModel.CreateSample();
        PresetSwitcher = PresetSwitcherViewModel.CreateSample();
        LayoutDiff = LayoutDiffViewModel.CreateSample();

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

        var layerEditJournal = new LayerEditEventJournalService(_timeProvider);
        ZoneEditorToolbar = new ZoneEditorToolbarViewModel(layerEditJournal);
        ZonePolicyPanel = new ZoneConstraintPolicyViewModel();
        ZoneImportExportPanel = new ZoneImportExportPanelViewModel();

        _mapLayers = BuildSampleLayers();
        _guidanceTracks = BuildSampleGuidance();
        LayerLegend = LayerLegendViewModel.FromLayers(_mapLayers);
        LayerInspector = BuildSampleInspector(_mapLayers);
        MeshSharePanel = MeshSharePanelViewModel.CreateSample();
        FieldHealthSeverity = FieldHealthSeverityPanelViewModel.CreateSample();
        ProfitAnalytics = ProfitAnalyticsViewModel.CreateSample();
        RadioProvisioningPanel = RadioProvisioningPanelViewModel.CreateSample();
        RadioProvisioning = RadioProvisioningFlowViewModel.CreateSample();

        ApplySamplePluginState();
        SeedDashboards();

        DeviceManagerCompatibility = DeviceManagerCompatibilityViewModel.CreateSample();
        DiagnosticsWorkspace = DiagnosticsWorkspaceViewModel.CreateSample(TelemetryPrivacy, DeviceManagerCompatibility, connectionSettings);

        if (configuration?.Scenarios is not null)
        {
            _scenarioDefinitions.AddRange(configuration.Scenarios);
        }

        // Theme bootstrapping
        AvailableThemes = Enum.GetValues<UiTheme>();
        var preferences = _preferencesService.GetPreferences();
        _shellLayout = preferences.ShellLayout.Clone();
        _isTopToolbarVisible = _shellLayout.IsTopToolbarVisible;
        _isRightSidebarVisible = _shellLayout.IsRightSidebarVisible;
        _selectedTheme = preferences.Theme;
        _themeManager.ApplyTheme(_selectedTheme);

        ShellMenuBar = new ShellMenuBarViewModel(commandDispatcher);
        TopToolbar = new TopToolbarViewModel(commandDispatcher);
        StatusStrip = BuildStatusStrip();
    }

    /// <summary>Raised when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the title displayed in the main window.</summary>
    public string Title { get; }

    /// <summary>Gets a description of the runtime platform.</summary>
    public string PlatformDescription { get; }

    /// <summary>Gets the view-model describing the shell menu bar.</summary>
    public ShellMenuBarViewModel ShellMenuBar { get; }

    /// <summary>Gets the view-model backing the top toolbar.</summary>
    public TopToolbarViewModel TopToolbar { get; }

    /// <summary>Gets the status strip view-model providing runtime indicators.</summary>
    public StatusStripViewModel StatusStrip { get; }

    /// <summary>Gets a sample vehicle pose used to seed the map view.</summary>
    public VehiclePose VehiclePose { get; } = new(10, 15, 45);

    /// <summary>Gets the connection settings view-model.</summary>
    public ConnectionSettingsViewModel Connection => _connectionSettings;

    /// <summary>Releases resources held by the view-model.</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets or sets whether the top toolbar is visible.</summary>
    public bool IsTopToolbarVisible
    {
        get => _isTopToolbarVisible;
        set
        {
            if (_isTopToolbarVisible == value)
            {
                return;
            }

            _isTopToolbarVisible = value;
            OnPropertyChanged();
            _shellLayout.IsTopToolbarVisible = value;
            PersistShellLayout();
        }
    }

    /// <summary>Gets or sets whether the right sidebar panels are visible.</summary>
    public bool IsRightSidebarVisible
    {
        get => _isRightSidebarVisible;
        set
        {
            if (_isRightSidebarVisible == value)
            {
                return;
            }

            _isRightSidebarVisible = value;
            OnPropertyChanged();
            _shellLayout.IsRightSidebarVisible = value;
            PersistShellLayout();
            OnPropertyChanged(nameof(MainWorkspaceColumnWidth));
            OnPropertyChanged(nameof(RightSidebarColumnWidth));
        }
    }

    /// <summary>Gets the grid length applied to the main workspace column.</summary>
    public GridLength MainWorkspaceColumnWidth =>
        IsRightSidebarVisible ? new GridLength(3, GridUnitType.Star) : new GridLength(1, GridUnitType.Star);

    /// <summary>Gets the grid length applied to the right sidebar column.</summary>
    public GridLength RightSidebarColumnWidth =>
        IsRightSidebarVisible ? new GridLength(2, GridUnitType.Star) : new GridLength(0);

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

    /// <summary>Gets the crop quick-select view-model surfaced in the field navigator.</summary>
    public CropQuickSelectViewModel CropQuickSelect { get; }

    /// <summary>Gets the preset switcher view-model that surfaces orchestration status.</summary>
    public PresetSwitcherViewModel PresetSwitcher { get; }

    /// <summary>Gets the layout diff view-model surfaced for linked layouts.</summary>
    public LayoutDiffViewModel LayoutDiff { get; }

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

    /// <summary>Gets the zone policy panel view-model that surfaces override toggles.</summary>
    public ZoneConstraintPolicyViewModel ZonePolicyPanel { get; }

    /// <summary>Gets the zone import/export panel view-model describing transfer workflows.</summary>
    public ZoneImportExportPanelViewModel ZoneImportExportPanel { get; }
    /// <summary>Gets the legend describing the active map layers.</summary>
    public LayerLegendViewModel LayerLegend { get; }

    /// <summary>Gets the inspector exposing the pinned layer observation.</summary>
    public LayerInspectorViewModel LayerInspector { get; }
    /// <summary>Gets the panel surfacing field health severity guidance.</summary>
    public FieldHealthSeverityPanelViewModel FieldHealthSeverity { get; }

    /// <summary>Gets the profitability analytics view-model powering the profit card.</summary>
    public ProfitAnalyticsViewModel ProfitAnalytics { get; }

    /// <summary>Gets the compatibility dashboard view-model consumed by the Device Manager card.</summary>
    public DeviceManagerCompatibilityViewModel DeviceManagerCompatibility { get; }
    /// <summary>Gets the mesh share/subscribe panel view-model.</summary>
    public MeshSharePanelViewModel MeshSharePanel { get; }
    /// <summary>Gets the radio provisioning panel view-model.</summary>
    public RadioProvisioningPanelViewModel RadioProvisioningPanel { get; }
    /// <summary>Gets the RadioBridge provisioning workflow view-model.</summary>
    public RadioProvisioningFlowViewModel RadioProvisioning { get; }
    /// <summary>Gets the diagnostics workspace view-model surfaced in the sidebar.</summary>
    public DiagnosticsWorkspaceViewModel DiagnosticsWorkspace { get; }

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
            var scenario = result.Scenario;
            var existingIndex = _scenarioDefinitions.FindIndex(
                definition => string.Equals(definition.ScenarioId, scenario.ScenarioId, StringComparison.Ordinal));

            if (existingIndex >= 0)
            {
                _scenarioDefinitions[existingIndex] = scenario;
            }
            else
            {
                _scenarioDefinitions.Add(scenario);
            }

            return true;
        });
    }

    /// <summary>
    /// Creates a boundary tool view-model populated with sample geometry.
    /// </summary>
    public BoundaryToolViewModel CreateBoundaryToolViewModel() => BoundaryToolViewModel.CreateSample();

    /// <summary>
    /// Creates a flag manager view-model populated with sample flags.
    /// </summary>
    public FlagManagerDialogViewModel CreateFlagManagerDialogViewModel() => FlagManagerDialogViewModel.CreateSample();

    /// <summary>
    /// Creates a shift position dialog view-model populated with sample offsets.
    /// </summary>
    public ShiftPositionDialogViewModel CreateShiftPositionDialogViewModel() => ShiftPositionDialogViewModel.CreateSample();

    /// <summary>
    /// Creates a metadata snapshot that companion clients can consume to mirror the desktop layout.
    /// </summary>
    public CompanionMetadataSnapshot CreateCompanionMetadataSnapshot()
    {
        return CompanionMetadataSnapshot.From(
            LayerLegend,
            LayerInspector,
            SteerDashboard,
            ReplayTimeline,
            FieldHealthSeverity,
            SimulationBar,
            DiagnosticsWorkspace);
    }

    private void PersistShellLayout()
    {
        _preferencesService.UpdateShellLayout(_shellLayout);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            return;
        }

        _disposed = true;
        _connectionSettings.Dispose();
        SimulationBar.Dispose();
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

    private StatusStripViewModel BuildStatusStrip()
    {
        var indicators = new[]
        {
            new ShellStatusIndicatorViewModel(
                "GPS",
                "RTK FIX",
                StatusIndicatorLevel.Normal,
                "GNSS corrections locked with centimeter accuracy."),
            new ShellStatusIndicatorViewModel(
                "Speed",
                "6.2 mph",
                StatusIndicatorLevel.Normal,
                "Ground speed reported by vehicle bus."),
            new ShellStatusIndicatorViewModel(
                "Sections",
                "6 / 8",
                StatusIndicatorLevel.Warning,
                "Two sections masked due to obstacle avoidance."),
            new ShellStatusIndicatorViewModel(
                "Radio",
                "Mesh linked",
                StatusIndicatorLevel.Normal,
                "RadioBridge mesh network connected."),
        };

        return new StatusStripViewModel(indicators);
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
        var seedTimestamp = _timeProvider.GetUtcNow();
        SteerDashboard.RecordTuningEvent(seedTimestamp.AddMinutes(-5), "Adjusted P gain to 0.30 based on headland drift");
        SteerDashboard.RecordTuningEvent(seedTimestamp.AddMinutes(-2), "Applied adaptive integral clamp after curve pass");
        SteerDashboard.RecordTuningEvent(seedTimestamp.AddMinutes(-1), "Saved preset 'Spring Wheat 2024'");

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
        var profitCells = new List<MapLayerCell>();

        for (var x = -3; x <= 3; x++)
        {
            for (var y = -2; y <= 4; y++)
            {
                var center = new Point(10 + x * spacing, 10 + y * spacing);
                var actualCoverage = Math.Clamp(0.15 + (y + 2) * 0.18 + Math.Sin(x * 0.7) * 0.05, 0, 1);
                var plannedCoverage = Math.Clamp(0.3 + (y + 1) * 0.14 + Math.Cos(x * 0.5) * 0.07, 0, 1);
                var profitValue = ((Math.Sin(x * 0.8) * 120) + (Math.Cos((y + 1) * 0.55) * 95)) + ((y - 1) * 42) - 65;

                actualCells.Add(new MapLayerCell(center, size, actualCoverage));
                plannedCells.Add(new MapLayerCell(center, size, plannedCoverage));
                profitCells.Add(new MapLayerCell(center, size, profitValue));
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
            Color.FromArgb(230, 178, 62, 94),
            Color.FromArgb(230, 16, 110, 85),
            -180,
            420,
            units: "USD/ha",
            outlineColor: Color.FromArgb(140, 33, 46, 51));

        return new List<MapLayer>
        {
            new("layer:coverage.actual", "Actual coverage", actualStyle, actualCells),
            new("layer:coverage.planned", "Planned rate", plannedStyle, plannedCells),
            new("layer:profit.net", "Profit per hectare", profitStyle, profitCells),
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

        var layer = layers[0];
        var description = layer.Style.IsPlanned
            ? "Pinned observation sourced from the prescription metadata."
            : "Pinned observation using the metadata-driven coverage inspector.";
        var inspector = new LayerInspectorViewModel(layer.LayerId, layer.DisplayName, layer.Style.IsPlanned, description);

        var cell = layer.Cells.Count > 0
            ? layer.Cells[Math.Min(3, layer.Cells.Count - 1)]
            : new MapLayerCell(new Point(0, 0), 1, layer.Style.MinimumValue);

        var valueFormat = string.Equals(layer.Style.Units, "fraction", StringComparison.OrdinalIgnoreCase)
            ? "{0:P1}"
            : "{0:0.##}";
        var units = layer.Style.Units;
        var targetValue = layer.Style.IsPlanned
            ? (double?)null
            : Math.Min(layer.Style.MaximumValue, cell.Value + 0.08);
        var timestamp = new DateTimeOffset(2024, 4, 11, 14, 32, 0, TimeSpan.Zero);
        var sourceDisplay = layer.Style.IsPlanned ? "Prescription catalog" : "Section Control (tractor-01)";

        static string FormatForMetadata(double value, string format, string? units)
        {
            var formatted = string.Format(CultureInfo.InvariantCulture, format, value);
            return string.IsNullOrWhiteSpace(units) ? formatted : string.Concat(formatted, " ", units);
        }

        var transportMetadata = new[]
        {
            new KeyValuePair<string, string>("PGN", "0xEF00"),
            new KeyValuePair<string, string>("CAN ID", "0x1CEBFF02"),
            new KeyValuePair<string, string>("Rate mode", layer.Style.IsPlanned ? "Preset (open-loop)" : "Closed-loop"),
        };

        var payloadMetadata = new[]
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
            "Quality 0.97 (Good)",
            weight: 0.82,
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
