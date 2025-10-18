using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.Telemetry;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class DiagnosticsWorkspaceViewModelTests
{
    [Fact]
    public void CreateSample_PopulatesDiagnosticsMetadata()
    {
        var telemetry = new TelemetryPrivacyViewModel(new TestCrashTelemetryService());
        var deviceManager = DeviceManagerCompatibilityViewModel.CreateSample();
        var connection = new ConnectionSettingsViewModel(new InMemoryConnectionSettingsStore(), new TestRunModeService());

        var workspace = DiagnosticsWorkspaceViewModel.CreateSample(telemetry, deviceManager, connection);

        workspace.Telemetry.Should().Be(telemetry);
        workspace.DeviceManager.Should().Be(deviceManager);
        workspace.Gps.FixQuality.Should().Be("RTK Fixed");
        workspace.Gps.SatelliteCount.Should().BeGreaterThanOrEqualTo(10);
        workspace.NetworkChannels.Should().NotBeEmpty();
        workspace.SerialProfiles.Should().NotBeEmpty();
        workspace.Loops.Should().NotBeEmpty();
        workspace.Events.Should().NotBeEmpty();
        workspace.ConnectionSummary.Should().Contain(connection.AgioEndpoint);
        workspace.TelemetrySummary.Should().Contain("No crash reports");
        workspace.DeviceHealthSummary.Should().Contain(deviceManager.SummaryTitle);
        workspace.LastUpdatedUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void CreateSample_ProducesCompanionFriendlySummaries()
    {
        var telemetry = new TelemetryPrivacyViewModel(new TestCrashTelemetryService());
        var deviceManager = DeviceManagerCompatibilityViewModel.CreateSample();
        var connection = new ConnectionSettingsViewModel(new InMemoryConnectionSettingsStore(), new TestRunModeService());

        var workspace = DiagnosticsWorkspaceViewModel.CreateSample(telemetry, deviceManager, connection);

        workspace.GpsSummary.Should().Contain("RTK Fixed");
        workspace.OverallSummary.Should().Contain("pose.loop");
        workspace.HasAlerts.Should().BeTrue();
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
