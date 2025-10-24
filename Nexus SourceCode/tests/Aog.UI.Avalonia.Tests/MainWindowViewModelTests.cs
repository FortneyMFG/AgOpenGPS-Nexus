using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Legacy;
using Aog.Core.Paths;
using Aog.Core.Simulation.Configuration;
using Aog.Core.V1;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.Telemetry;
using Aog.UI.Avalonia.Theming;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.ViewModels.Shell;
using Aog.UI.Avalonia.Plugins;
using Avalonia.Media;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class MainWindowViewModelTests
{
    private static readonly DateTimeOffset SeedTimestamp = new(2024, 04, 01, 12, 00, 00, TimeSpan.Zero);

    [Fact]
    public void SimulationGraphSummary_ExposesEmbeddedGraph()
    {
        var viewModel = CreateViewModel();

        viewModel.SimulationGraphSummary.Should().Contain("Simulation Provider Graph");
        viewModel.SimulationGraphSummary.Should().Contain("sim.clock.fixed");
    }

    [Fact]
    public void SimulationBar_ExposesRoutesFromConfiguration()
    {
        var viewModel = CreateViewModel();

        viewModel.SimulationBar.Should().NotBeNull();
        viewModel.SimulationBar.Routes.Should().NotBeEmpty();
        viewModel.SimulationBar.Routes.Select(route => route.Stream)
            .Should().Contain(new[] { "pose", "imu" });
    }

    [Fact]
    public void PluginPanels_ExposeSampleState()
    {
        var viewModel = CreateViewModel();

        viewModel.SteerPanel.IsEnabled.Should().BeTrue();
        viewModel.SteerPanel.TargetWheelAngleDegrees.Should().BeApproximately(2.5, 1e-3);

        viewModel.SectionsPanel.Sections.Count(section => section.IsVisible).Should().Be(8);
        viewModel.SectionsPanel.CurrentMask.Should().Be(0b0011_1100u);

        viewModel.PlanterPanel.Rows.Should().HaveCountGreaterThan(0);
        viewModel.PlanterPanel.Rows.Single(row => row.RowIndex == 2).Quality.Should().Be(PlanterRowQuality.Double);
        viewModel.PlanterPanel.Summary.Should().Contain("Rows:");

        viewModel.PresetSwitcher.Should().NotBeNull();
        viewModel.PresetSwitcher.Presets.Should().NotBeEmpty();
    }

    [Fact]
    public void LayoutDiff_SurfacesSampleChanges()
    {
        var viewModel = CreateViewModel();

        viewModel.LayoutDiff.Should().NotBeNull();
        viewModel.LayoutDiff.HasChanges.Should().BeTrue();
        viewModel.LayoutDiff.Changes.Should().HaveCountGreaterThan(0);
        viewModel.LayoutDiff.HasLinkedPreset.Should().BeTrue();
    }

    [Fact]
    public void MapOverlays_ExposeCoverageAndGuidance()
    {
        var viewModel = CreateViewModel();

        viewModel.MapLayers.Should().NotBeEmpty();
        viewModel.MapLayers.SelectMany(layer => layer.Cells).Should().NotBeEmpty();
        viewModel.GuidanceTracks.Should().NotBeEmpty();
        viewModel.GuidanceTracks.Select(track => track.Points.Count).Max().Should().BeGreaterThan(1);
    }

    [Fact]
    public void Dashboards_SurfaceSampleHistory()
    {
        var viewModel = CreateViewModel();

        viewModel.SteerDashboard.Series.Should().NotBeEmpty();
        viewModel.SteerDashboard.Series.SelectMany(series => series.Values).Should().NotBeEmpty();
        viewModel.SteerDashboard.TuningParameters.Should().NotBeEmpty();
        viewModel.SteerDashboard.TuningEvents.Should().HaveCount(3);
        viewModel.SteerDashboard.TuningEvents.Select(evt => evt.Timestamp)
            .Should()
            .Equal(
                SeedTimestamp.AddMinutes(-1),
                SeedTimestamp.AddMinutes(-2),
                SeedTimestamp.AddMinutes(-5));
        viewModel.SteerDashboard.TuningEvents.Select(evt => evt.Description)
            .Should()
            .Contain(new[]
            {
                "Adjusted P gain to 0.30 based on headland drift",
                "Applied adaptive integral clamp after curve pass",
                "Saved preset 'Spring Wheat 2024'",
            });
        viewModel.ReplayTimeline.Bookmarks.Should().NotBeEmpty();
        viewModel.ReplayTimeline.SpeedSamples.Should().HaveCountGreaterThan(10);
    }

    [Fact]
    public void InspectorAndLegend_SurfaceLayerMetadata()
    {
        var viewModel = CreateViewModel();

        viewModel.LayerLegend.Should().NotBeNull();
        viewModel.LayerLegend.HasEntries.Should().BeTrue();
        viewModel.LayerLegend.Entries.Should().HaveCount(viewModel.MapLayers.Count);
        viewModel.LayerLegend.Entries.Select(entry => entry.DisplayName)
            .Should().Contain("Actual coverage");

        viewModel.LayerInspector.Should().NotBeNull();
        viewModel.LayerInspector.LayerName.Should().NotBeNullOrWhiteSpace();
        viewModel.LayerInspector.ValueDisplay.Should().Contain("%");
        viewModel.LayerInspector.TransportMetadata.Should().NotBeEmpty();
        viewModel.LayerInspector.PayloadMetadata.Should().NotBeEmpty();
        viewModel.LayerInspector.RateAvailabilityDisplay.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FieldHealthSeverity_SurfaceSampleScale()
    {
        var viewModel = CreateViewModel();

        viewModel.FieldHealthSeverity.Should().NotBeNull();
        viewModel.FieldHealthSeverity.LayerDisplayName.Should().ContainEquivalentOf("Flood");
        viewModel.FieldHealthSeverity.Entries.Should().HaveCountGreaterThan(3);
        viewModel.FieldHealthSeverity.Entries.Select(entry => entry.Severity)
            .Should().Contain(new[] { "Critical", "High", "Moderate", "Low", "None" });
    }

    [Fact]
    public void CompanionSnapshot_MirrorsMetadataDrivenState()
    {
        var viewModel = CreateViewModel();

        var snapshot = viewModel.CreateCompanionMetadataSnapshot();

        snapshot.Legend.Entries.Should().HaveCount(viewModel.LayerLegend.Entries.Count);
        snapshot.Legend.Entries.Select(entry => entry.LayerId)
            .Should().BeEquivalentTo(viewModel.LayerLegend.Entries.Select(entry => entry.LayerId));

        var sourceLegend = viewModel.LayerLegend.Entries.First();
        var snapshotLegend = snapshot.Legend.Entries.First();
        snapshotLegend.RangeDisplay.Should().Be(sourceLegend.RangeDisplay);
        snapshotLegend.ModeDisplay.Should().Be(sourceLegend.ModeDisplay);

        snapshot.Inspector.LayerId.Should().Be(viewModel.LayerInspector.LayerId);
        snapshot.Inspector.TransportMetadata.Should().HaveCount(viewModel.LayerInspector.TransportMetadata.Count);
        snapshot.Inspector.PayloadMetadata.Should().HaveCount(viewModel.LayerInspector.PayloadMetadata.Count);
        snapshot.Inspector.RateAvailability.Should().Be(viewModel.LayerInspector.RateAvailabilityDisplay);

        snapshot.Dashboard.Series.Should().HaveCount(viewModel.SteerDashboard.Series.Count);
        var sourceSeries = viewModel.SteerDashboard.Series.Single(series => series.Id == "autosteer.crossTrack");
        snapshot.Dashboard.Series.Single(series => series.Id == "autosteer.crossTrack")
            .Values.Should().Equal(sourceSeries.Values);
        snapshot.Dashboard.Series.Single(series => series.Id == "autosteer.crossTrack")
            .StrokeColor.Should().Be(((ISolidColorBrush)sourceSeries.Stroke).Color.ToString());

        snapshot.Dashboard.TuningParameters.Select(parameter => parameter.Id)
            .Should().BeEquivalentTo(viewModel.SteerDashboard.TuningParameters.Select(parameter => parameter.Id));
        snapshot.Dashboard.GainSummary.Should().Be(viewModel.SteerDashboard.GainSummary);

        snapshot.ReplayTimeline.SpeedSamples.Should().HaveCount(viewModel.ReplayTimeline.SpeedSamples.Count);
        snapshot.ReplayTimeline.HeadingSamples.Should().HaveCount(viewModel.ReplayTimeline.HeadingSamples.Count);
        snapshot.ReplayTimeline.Bookmarks.Should().HaveCount(viewModel.ReplayTimeline.Bookmarks.Count);
        snapshot.FieldHealth.LayerDisplayName.Should().Be(viewModel.FieldHealthSeverity.LayerDisplayName);
        snapshot.FieldHealth.FilterSummary.Should().Be(viewModel.FieldHealthSeverity.FilterSummary);
        snapshot.FieldHealth.Entries.Should().HaveCount(viewModel.FieldHealthSeverity.Entries.Count);
        snapshot.FieldHealth.Entries.Select(entry => entry.Severity)
            .Should().BeEquivalentTo(viewModel.FieldHealthSeverity.Entries.Select(entry => entry.Severity));
        var sourceSeverity = viewModel.FieldHealthSeverity.Entries.First();
        var snapshotSeverity = snapshot.FieldHealth.Entries.First(entry => entry.Severity == sourceSeverity.Severity);
        snapshotSeverity.Color.Should().Be(sourceSeverity.Color.ToString());
        snapshot.Simulation.Status.Should().Be(viewModel.SimulationBar.StatusText);
        snapshot.Simulation.PlaybackRate.Should().Be(viewModel.SimulationBar.SelectedPlaybackRate);
        snapshot.Simulation.Routes.Should().HaveCount(viewModel.SimulationBar.Routes.Count);
        snapshot.Diagnostics.ConnectionSummary.Should().Be(viewModel.DiagnosticsWorkspace.ConnectionSummary);
        snapshot.Diagnostics.NetworkChannels.Should().HaveCount(viewModel.DiagnosticsWorkspace.NetworkChannels.Count);
        snapshot.Diagnostics.Loops.Should().HaveCount(viewModel.DiagnosticsWorkspace.Loops.Count);
        snapshot.Diagnostics.Events.Should().HaveCount(viewModel.DiagnosticsWorkspace.Events.Count);
        snapshot.Diagnostics.Gps.FixQuality.Should().Be(viewModel.DiagnosticsWorkspace.Gps.FixQuality);
        snapshot.Diagnostics.HasAlerts.Should().Be(viewModel.DiagnosticsWorkspace.HasAlerts);
    }

    [Fact]
    public void MeshSharePanel_SurfacesSampleDevices()
    {
        var viewModel = CreateViewModel();

        viewModel.MeshSharePanel.Should().NotBeNull();
        viewModel.MeshSharePanel.Devices.Should().NotBeEmpty();
        viewModel.MeshSharePanel.Devices.Should().AllSatisfy(device =>
        {
            device.DisplayName.Should().NotBeNullOrWhiteSpace();
            device.DeviceId.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void RadioProvisioningPanel_SurfacesProvisioningState()
    {
        var viewModel = CreateViewModel();

        viewModel.RadioProvisioningPanel.Should().NotBeNull();
        viewModel.RadioProvisioningPanel.Devices.Should().NotBeEmpty();
        viewModel.RadioProvisioningPanel.Profiles.Should().NotBeEmpty();
        viewModel.RadioProvisioningPanel.AuditTrail.Should().NotBeEmpty();
        viewModel.RadioProvisioningPanel.Devices.Should().AllSatisfy(device =>
        {
            device.Steps.Should().NotBeEmpty();
        });
    }

    [Fact]
    public void DiagnosticsWorkspace_SurfacesSampleData()
    {
        var viewModel = CreateViewModel();

        viewModel.DiagnosticsWorkspace.Should().NotBeNull();
        viewModel.DiagnosticsWorkspace.Gps.FixQuality.Should().Be("RTK Fixed");
        viewModel.DiagnosticsWorkspace.NetworkChannels.Should().NotBeEmpty();
        viewModel.DiagnosticsWorkspace.Loops.Should().NotBeEmpty();
        viewModel.DiagnosticsWorkspace.Events.Should().NotBeEmpty();
        viewModel.DiagnosticsWorkspace.TelemetrySummary.Should().Contain("No crash reports");
    }

    [Fact]
    public void ShellMenuBar_DispatchesCommands()
    {
        var viewModel = CreateViewModel(out var dispatcher);

        var profileItem = viewModel.ShellMenuBar.FileMenu.Items.First();
        profileItem.Command.Should().NotBeNull();
        profileItem.Command!.Execute(null);

        dispatcher.Invocations.Should().ContainSingle(invocation =>
            invocation.InjectionPoint == "menu.file" && invocation.CommandId == "core.profile.manage");
        profileItem.LastInvocationHandled.Should().BeTrue();
        profileItem.LastInvocationTimestamp.Should().NotBeNull();
    }

    [Fact]
    public void ShellMenuItemTooltip_IncludesLocalOffset()
    {
        var originalTz = Environment.GetEnvironmentVariable("TZ");
        try
        {
            Environment.SetEnvironmentVariable("TZ", "Asia/Seoul");
            TimeZoneInfo.ClearCachedData();

            var viewModel = CreateViewModel(out _);
            var profileItem = viewModel.ShellMenuBar.FileMenu.Items.First();

            profileItem.Command!.Execute(null);

            var tooltip = profileItem.Tooltip;
            tooltip.Should().Contain("Last invoked");

            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.UtcNow);
            var expectedOffset = offset.ToString(@"\+hh\:mm;-hh\:mm", CultureInfo.InvariantCulture);
            tooltip.Should().Contain(expectedOffset);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TZ", originalTz);
            TimeZoneInfo.ClearCachedData();
        }
    }

    [Fact]
    public void TopToolbar_TogglesUpdateState()
    {
        var viewModel = CreateViewModel(out var dispatcher);

        var autoSteer = viewModel.TopToolbar.Items.First(item => item.Id == "toolbar.autosteer");
        autoSteer.IsChecked.Should().BeTrue();

        autoSteer.Command.Execute(null);

        autoSteer.IsChecked.Should().BeFalse();
        dispatcher.Invocations.Should().Contain(invocation => invocation.CommandId == "core.toolbar.autoSteer");
    }

    [Fact]
    public void StatusStrip_ExposesIndicators()
    {
        var viewModel = CreateViewModel();

        viewModel.StatusStrip.Indicators.Should().NotBeEmpty();
        viewModel.StatusStrip.Indicators.Select(indicator => indicator.Label)
            .Should().Contain("GPS");
    }

    [Fact]
    public void LegacyImportScenario_UpdatesScenarioCollection()
    {
        var viewModel = CreateViewModel();

        var baselineCount = viewModel.CreateScenarioEditorViewModel().Scenarios.Count;
        var importResult = CreateSampleLegacyImportResult();
        var wizard = viewModel.CreateLegacyImportWizardViewModel();

        SetImportResult(wizard, importResult);

        wizard.TryApplyRoutes().Should().BeTrue();

        var updatedEditor = viewModel.CreateScenarioEditorViewModel();
        updatedEditor.Scenarios.Select(scenario => scenario.ScenarioId)
            .Should().Contain(importResult.Scenario.ScenarioId);
        updatedEditor.Scenarios.Should().HaveCount(baselineCount + 1);

        SetImportResult(wizard, importResult);
        wizard.TryApplyRoutes().Should().BeTrue();

        var dedupedEditor = viewModel.CreateScenarioEditorViewModel();
        dedupedEditor.Scenarios.Select(scenario => scenario.ScenarioId)
            .Count(id => id == importResult.Scenario.ScenarioId)
            .Should().Be(1);
    }

    private static MainWindowViewModel CreateViewModel() 
    {
        var connectionStore = new InMemoryConnectionSettingsStore();
        var runModeService = new TestRunModeService();
        var connection = new ConnectionSettingsViewModel(connectionStore, runModeService);
        var preferencesStore = new InMemoryUiPreferencesStore();
        var preferencesService = new UiPreferencesService(preferencesStore);
        var catalog = new BlockCatalog(new IBlockProvider[] { new CoreBlockProvider() });
        var layoutStore = new BlockLayoutStore(preferencesService, catalog);
        var themeManager = new TestThemeManager();
        var telemetryService = new TestCrashTelemetryService();
        var telemetryViewModel = new TelemetryPrivacyViewModel(telemetryService);
        var dispatcher = new RecordingShellCommandDispatcher();
        var layout = new BlockLayoutViewModel(layoutStore, catalog, dispatcher, preferencesService);
        var shell = new AppShellViewModel(layout);
        var pluginRegistry = new PluginRegistry();
        var pluginHost = new PluginHost(new NullServiceProvider(), NullLogger<PluginHost>.Instance);
        var timeProvider = new FixedTimeProvider(SeedTimestamp);

        return new MainWindowViewModel(
            connection,
            null,
            preferencesService,
            themeManager,
            dispatcher,
            pluginRegistry,
            pluginHost,
            shell,
            telemetryViewModel,
            timeProvider,
            NullLogger<MainWindowViewModel>.Instance);
    }

    private static MainWindowViewModel CreateViewModel(out RecordingShellCommandDispatcher dispatcher)
    {
        var connectionStore = new InMemoryConnectionSettingsStore();
        var runModeService = new TestRunModeService();
        var connection = new ConnectionSettingsViewModel(connectionStore, runModeService);
        var preferencesStore = new InMemoryUiPreferencesStore();
        var preferencesService = new UiPreferencesService(preferencesStore);
        var catalog = new BlockCatalog(new IBlockProvider[] { new CoreBlockProvider() });
        var layoutStore = new BlockLayoutStore(preferencesService, catalog);
        var themeManager = new TestThemeManager();
        var telemetryService = new TestCrashTelemetryService();
        var telemetryViewModel = new TelemetryPrivacyViewModel(telemetryService);
        dispatcher = new RecordingShellCommandDispatcher();
        var layout = new BlockLayoutViewModel(layoutStore, catalog, dispatcher, preferencesService);
        var shell = new AppShellViewModel(layout);
        var pluginRegistry = new PluginRegistry();
        var pluginHost = new PluginHost(new NullServiceProvider(), NullLogger<PluginHost>.Instance);

        // Deterministic time for tests that assert relative timestamps.
        var timeProvider = new FixedTimeProvider(SeedTimestamp);

        return new MainWindowViewModel(
            connection,
            null,
            preferencesService,
            themeManager,
            dispatcher,
            pluginRegistry,
            pluginHost,
            shell,
            telemetryViewModel,
            timeProvider,
            NullLogger<MainWindowViewModel>.Instance);
    }

    // Deterministic TimeProvider for stable tests.
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    private sealed class InMemoryConnectionSettingsStore : IConnectionSettingsStore
    {
        private ConnectionSettings _settings = new();

        public ConnectionSettings Load() => _settings.Clone();

        public void Save(ConnectionSettings settings)
        {
            _settings = settings.Clone();
        }
    }

    private class FakePreferencesService : IUiPreferencesService
    {
        private readonly UiPreferences _preferences;

        public FakePreferencesService(UiPreferences preferences)
        {
            _preferences = preferences;
        }

        public UiPreferences GetPreferences() => _preferences.Clone();

        public void UpdateTheme(UiTheme theme) { }

        public void UpdateWindowPlacement(WindowPlacement placement) { }

        public void UpdateTelemetryOptIn(bool isOptedIn) { }

        public void UpdateRunMode(AvaloniaRunMode mode) { }

        public void UpdateShellLayout(ShellLayoutPreferences layout)
        {
            _preferences.ShellLayout = layout.Clone();
        }
    }

    private static void SetImportResult(LegacyImportWizardViewModel wizard, LegacyGuidanceImportResult result)
    {
        var field = typeof(LegacyImportWizardViewModel).GetField("_result", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is null)
        {
            throw new InvalidOperationException("LegacyImportWizardViewModel._result field not found.");
        }

        field.SetValue(wizard, result);
    }

    private static LegacyGuidanceImportResult CreateSampleLegacyImportResult()
    {
        var abLines = new[]
        {
            new LegacyAbLinePlanar(
                "Alpha",
                new GeographicCoordinate(51.0, -114.0),
                new GeographicCoordinate(51.001, -114.0),
                new PlanarPoint(0, 0),
                new PlanarPoint(10, 0),
                0,
                10),
        };

        var boundary = new[]
        {
            new PlanarPoint(0, 0),
            new PlanarPoint(10, 0),
            new PlanarPoint(10, 5),
            new PlanarPoint(0, 5),
        };

        var scenario = new SimulationScenarioConfiguration(
            "legacy:sample",
            "Imported guidance",
            new[] { new SimulationRouteConfiguration("pose", "legacy/udp/main_gps", "hardware") },
            options: null);

        return new LegacyGuidanceImportResult(
            "SampleField",
            new GeographicCoordinate(51.0, -114.0),
            abLines,
            boundary,
            scenario);
    }

    private sealed class InMemoryUiPreferencesStore : IUiPreferencesStore
    {
        private UiPreferences _preferences = new();

        public UiPreferences Load() => _preferences.Clone();

        public void Save(UiPreferences preferences)
        {
            _preferences = preferences.Clone();
        }
    }

    private sealed class TestRunModeService : IAvaloniaRunModeService
    {
        private AvaloniaRunMode _mode = AvaloniaRunMode.CompanionRemote;

        public event EventHandler<AvaloniaRunModeChangedEventArgs>? ModeChanged;

        public AvaloniaRunMode CurrentMode => _mode;

        public IReadOnlyList<AvaloniaRunMode> SupportedModes { get; } = Enum.GetValues<AvaloniaRunMode>();

        public Task<RunModeChangeResult> SetModeAsync(AvaloniaRunMode mode, CancellationToken cancellationToken = default)
        {
            _mode = mode;
            ModeChanged?.Invoke(this, new AvaloniaRunModeChangedEventArgs(mode));
            return Task.FromResult(new RunModeChangeResult(mode, false));
        }
    }

    private sealed class TestThemeManager : IThemeManager
    {
        public UiTheme CurrentTheme { get; private set; } = UiTheme.Light;

        public event EventHandler<UiTheme>? ThemeChanged;

        public void ApplyTheme(UiTheme theme)
        {
            if (CurrentTheme != theme)
            {
                CurrentTheme = theme;
                ThemeChanged?.Invoke(this, theme);
            }
        }
    }

    private sealed class RecordingShellCommandDispatcher : IShellCommandDispatcher
    {
        public List<(string InjectionPoint, string CommandId)> Invocations { get; } = new();

        public ValueTask<bool> DispatchAsync(string injectionPoint, string commandId, CancellationToken cancellationToken = default)
        {
            Invocations.Add((injectionPoint, commandId));
            return ValueTask.FromResult(true);
        }
    }

    private sealed class TestCrashTelemetryService : ICrashTelemetryService
    {
        private CrashTelemetryState _state = new()
        {
            IsTelemetryOptedIn = false,
            PendingReports = Array.Empty<CrashReportSummary>(),
        };

        public CrashTelemetryState GetState() => _state;

        public void SetTelemetryOptIn(bool isOptedIn)
        {
            _state = new CrashTelemetryState
            {
                IsTelemetryOptedIn = isOptedIn,
                PendingReports = _state.PendingReports,
            };
        }

        public void ClearPendingReports()
        {
            _state = new CrashTelemetryState
            {
                IsTelemetryOptedIn = _state.IsTelemetryOptedIn,
                PendingReports = Array.Empty<CrashReportSummary>(),
            };
        }

        public IReadOnlyList<CrashReportSummary> UploadPendingReports() => Array.Empty<CrashReportSummary>();
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
