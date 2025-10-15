using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Hosting;

public sealed class AvaloniaRunModeServiceTests
{
    [Fact]
    public async Task SetModeAsync_RaisesEvent_AndPersistsPreference()
    {
        var preferences = new TestPreferencesService();
        var service = CreateService(preferences, platformDefault: AvaloniaRunMode.LocalInProc);

        var observedModes = new List<AvaloniaRunMode>();
        service.ModeChanged += (_, args) => observedModes.Add(args.Mode);

        var result = await service.SetModeAsync(AvaloniaRunMode.LocalOutOfProc);

        result.RequiresRestart.Should().BeTrue();
        observedModes.Should().ContainSingle().Which.Should().Be(AvaloniaRunMode.LocalOutOfProc);
        preferences.LastSavedRunMode.Should().Be(AvaloniaRunMode.LocalOutOfProc);
    }

    [Fact]
    public async Task SetModeAsync_DoesNotRequireRestart_WhenStayingInProc()
    {
        var preferences = new TestPreferencesService();
        var service = CreateService(preferences, platformDefault: AvaloniaRunMode.LocalInProc);

        var result = await service.SetModeAsync(AvaloniaRunMode.LocalInProc);

        result.RequiresRestart.Should().BeFalse();
        preferences.LastSavedRunMode.Should().Be(AvaloniaRunMode.LocalInProc);
    }

    private static AvaloniaRunModeService CreateService(TestPreferencesService preferences, AvaloniaRunMode platformDefault)
    {
        var options = Options.Create(new AvaloniaShellOptions());
        return new AvaloniaRunModeService(options, preferences, new TestPlatform(platformDefault), NullLogger<AvaloniaRunModeService>.Instance);
    }

    private sealed class TestPreferencesService : IUiPreferencesService
    {
        private UiPreferences _preferences = new();

        public AvaloniaRunMode LastSavedRunMode { get; private set; } = AvaloniaRunMode.CompanionRemote;

        public UiPreferences GetPreferences() => _preferences.Clone();

        public void UpdateTheme(UiTheme theme)
        {
            _preferences.Theme = theme;
        }

        public void UpdateWindowPlacement(WindowPlacement placement)
        {
            _preferences.Window = placement.Clone();
        }

        public void UpdateTelemetryOptIn(bool isOptedIn)
        {
            _preferences.TelemetryOptIn = isOptedIn;
        }

        public void UpdateRunMode(AvaloniaRunMode mode)
        {
            LastSavedRunMode = mode;
            _preferences.RunMode = mode;
        }
    }

    private sealed class TestPlatform : IRunModePlatform
    {
        private readonly AvaloniaRunMode _defaultMode;

        public TestPlatform(AvaloniaRunMode defaultMode)
        {
            _defaultMode = defaultMode;
        }

        public AvaloniaRunMode GetDefaultMode() => _defaultMode;
    }
}
