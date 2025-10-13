using System;
using System.Linq;
using Aog.Core.V1;
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

    private static MainWindowViewModel CreateViewModel()
    {
        var connectionStore = new InMemoryConnectionSettingsStore();
        var connection = new ConnectionSettingsViewModel(connectionStore);
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
