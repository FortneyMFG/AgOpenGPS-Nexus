using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.UI.Avalonia.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Coordinates run mode selection, persistence, and notifications.
/// </summary>
public sealed class AvaloniaRunModeService : IAvaloniaRunModeService
{
    private readonly ILogger<AvaloniaRunModeService> _logger;
    private readonly IUiPreferencesService _preferencesService;
    private readonly IRunModePlatform _platform;
    private readonly IReadOnlyList<AvaloniaRunMode> _modes = Enum.GetValues<AvaloniaRunMode>();
    private AvaloniaRunMode _currentMode;

    public AvaloniaRunModeService(
        IOptions<AvaloniaShellOptions> options,
        IUiPreferencesService preferencesService,
        IRunModePlatform platform,
        ILogger<AvaloniaRunModeService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(preferencesService);
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(logger);

        _preferencesService = preferencesService;
        _platform = platform;
        _logger = logger;

        var configuredMode = options.Value.RunMode;
        var persisted = preferencesService.GetPreferences().RunMode;
        _currentMode = ResolveInitialMode(configuredMode, persisted);

        if (_currentMode != persisted)
        {
            _preferencesService.UpdateRunMode(_currentMode);
        }

        _logger.LogInformation("Avalonia run mode initialised to {Mode}.", _currentMode);
    }

    public event EventHandler<AvaloniaRunModeChangedEventArgs>? ModeChanged;

    public AvaloniaRunMode CurrentMode => _currentMode;

    public IReadOnlyList<AvaloniaRunMode> SupportedModes => _modes;

    public Task<RunModeChangeResult> SetModeAsync(AvaloniaRunMode mode, CancellationToken cancellationToken = default)
    {
        if (!_modes.Contains(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        if (mode == _currentMode)
        {
            return Task.FromResult(new RunModeChangeResult(mode, RequiresRestart(_currentMode, mode)));
        }

        var previous = _currentMode;
        _currentMode = mode;
        _preferencesService.UpdateRunMode(mode);
        _logger.LogInformation("Run mode switched to {Mode}.", mode);
        ModeChanged?.Invoke(this, new AvaloniaRunModeChangedEventArgs(mode));

        var requiresRestart = RequiresRestart(previous, mode);
        var reason = requiresRestart
            ? "Switching between in-proc and out-of-proc hosting requires a restart."
            : null;

        return Task.FromResult(new RunModeChangeResult(mode, requiresRestart, reason));
    }

    private AvaloniaRunMode ResolveInitialMode(AvaloniaRunMode configured, AvaloniaRunMode persisted)
    {
        if (_modes.Contains(configured))
        {
            return configured;
        }

        if (_modes.Contains(persisted))
        {
            return persisted;
        }

        return _platform.GetDefaultMode();
    }

    private static bool RequiresRestart(AvaloniaRunMode current, AvaloniaRunMode next)
    {
        return IsOutOfProc(current) != IsOutOfProc(next);
    }

    private static bool IsOutOfProc(AvaloniaRunMode mode)
    {
        return mode == AvaloniaRunMode.LocalOutOfProc;
    }
}
