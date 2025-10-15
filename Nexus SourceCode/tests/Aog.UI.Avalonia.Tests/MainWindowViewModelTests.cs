using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.Telemetry;
using Aog.UI.Avalonia.Theming;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class MainWindowViewModelTests
{
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

        viewModel.SectionsPanel.Sections.Should().HaveCount(8);
        viewModel.SectionsPanel.CurrentMask.Should().Be(0b0011_1100u);

        viewModel.PlanterPanel.Rows.Should().HaveCountGreaterThan(0);
        viewModel.PlanterPanel.Rows.Single(row => row.RowIndex == 2).Quality.Should().Be(PlanterRowQuality.Double);
        viewModel.PlanterPanel.Summary.Should().Contain("Rows:");
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
    public void PresetSwitcher_SurfaceSamplePresets()
    {
        var viewModel = CreateViewModel();

        viewModel.PresetSwitcher.Should().NotBeNull();
        viewModel.PresetSwitcher.Presets.Should().HaveCountGreaterThan(1);
        viewModel.PresetSwitcher.ActivePresetDisplay.Should().NotBeNullOrWhiteSpace();
        viewModel.PresetSwitcher.Presets.Any(preset => preset.OrchestrationState == PresetOrchestrationState.Blocked)
            .Should().BeTrue();
    }

    private static MainWindowViewModel CreateViewModel()
    {
        var connectionStore = new InMemoryConnectionSettingsStore();
        var runModeService = new TestRunModeService();
        var connection = new ConnectionSettingsViewModel(connectionStore, runModeService);
        var preferencesStore = new InMemoryUiPreferencesStore();
        var preferencesService = new UiPreferencesService(preferencesStore);
        var themeManager = new TestThemeManager();
        var telemetryService = new TestCrashTelemetryService();
        var telemetryViewModel = new TelemetryPrivacyViewModel(telemetryService);
        return new MainWindowViewModel(connection, null, preferencesService, themeManager, telemetryViewModel);
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
            return Task.FromResult(new RunModeChangeResult(mode, requiresRestart: false));
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
}
