using System;
using Aog.Agio.Safety;
using Aog.Core.V1;
using Microsoft.Extensions.Options;

namespace Aog.Agio;

/// <summary>
/// Default implementation of <see cref="IActuatorFailsafeService"/> that enforces watchdog-based safety policies.
/// </summary>
public sealed class ActuatorFailsafeService : IActuatorFailsafeService, IDisposable
{
    private readonly object _syncRoot = new();
    private readonly TimeProvider _timeProvider;
    private readonly IDisposable? _optionsSubscription;
    private readonly ISafetyLog _safetyLog;

    private OptionsSnapshot _options;
    private DateTimeOffset? _lastHeartbeatUtc;
    private SteerCmd? _lastSteerCommand;
    private SectionMask? _lastSectionMask;
    private bool _heartbeatActive;

    public ActuatorFailsafeService(
        IOptionsMonitor<AgioSafetyOptions> optionsMonitor,
        TimeProvider? timeProvider = null,
        ISafetyLog? safetyLog = null)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _options = CreateSnapshot(optionsMonitor.CurrentValue);
        _optionsSubscription = optionsMonitor.OnChange((options, _) => UpdateOptions(options));
        _safetyLog = safetyLog ?? NullSafetyLog.Instance;
    }

    /// <inheritdoc />
    public bool HasActiveHeartbeat
    {
        get
        {
            lock (_syncRoot)
            {
                return HasActiveHeartbeatLocked(_timeProvider.GetUtcNow(), _options);
            }
        }
    }

    /// <inheritdoc />
    public DateTimeOffset? LastHeartbeatUtc
    {
        get
        {
            lock (_syncRoot)
            {
                return _lastHeartbeatUtc;
            }
        }
    }

    /// <inheritdoc />
    public TimeSpan HeartbeatTimeout
    {
        get
        {
            lock (_syncRoot)
            {
                return _options.HeartbeatTimeout;
            }
        }
    }

    /// <inheritdoc />
    public void ReportHeartbeat()
    {
        lock (_syncRoot)
        {
            var snapshot = _options;
            var now = _timeProvider.GetUtcNow();
            _lastHeartbeatUtc = now;
            LogHeartbeatReceivedLocked(now, snapshot);
        }
    }

    /// <inheritdoc />
    public void ClearHeartbeat()
    {
        lock (_syncRoot)
        {
            _lastHeartbeatUtc = null;
            var now = _timeProvider.GetUtcNow();
            EnsureHeartbeatExpiredLocked(now, _options, SafetyLogEvents.HeartbeatReasons.Cleared);
            _safetyLog.Record(SafetyLogEntry.HeartbeatCleared(now));
        }
    }

    /// <inheritdoc />
    public SteerCmd FilterSteerCommand(SteerCmd command)
    {
        ArgumentNullException.ThrowIfNull(command);

        lock (_syncRoot)
        {
            var snapshot = _options;
            var now = _timeProvider.GetUtcNow();

            if (HasActiveHeartbeatLocked(now, snapshot))
            {
                _lastSteerCommand = command.Clone();
                return command;
            }

            EnsureHeartbeatExpiredLocked(now, snapshot, SafetyLogEvents.HeartbeatReasons.Timeout);
            _safetyLog.Record(SafetyLogEntry.FailsafeApplied(
                now,
                SafetyLogEvents.Actuators.Steer,
                snapshot.SteerFailsafe.ToString().ToLowerInvariant()));
            return BuildSafeSteerCommand(command, snapshot);
        }
    }

    /// <inheritdoc />
    public SectionMask FilterSectionMask(SectionMask mask)
    {
        ArgumentNullException.ThrowIfNull(mask);

        lock (_syncRoot)
        {
            var snapshot = _options;
            var now = _timeProvider.GetUtcNow();

            if (HasActiveHeartbeatLocked(now, snapshot))
            {
                _lastSectionMask = mask.Clone();
                return mask;
            }

            EnsureHeartbeatExpiredLocked(now, snapshot, SafetyLogEvents.HeartbeatReasons.Timeout);
            _safetyLog.Record(SafetyLogEntry.FailsafeApplied(
                now,
                SafetyLogEvents.Actuators.Sections,
                snapshot.SectionFailsafe.ToString().ToLowerInvariant()));
            return BuildSafeSectionMask(mask, snapshot);
        }
    }

    public void Dispose()
    {
        _optionsSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void UpdateOptions(AgioSafetyOptions options)
    {
        lock (_syncRoot)
        {
            _options = CreateSnapshot(options);
        }
    }

    private bool HasActiveHeartbeatLocked(DateTimeOffset now, OptionsSnapshot snapshot)
    {
        if (_lastHeartbeatUtc is null)
        {
            return false;
        }

        return now - _lastHeartbeatUtc <= snapshot.HeartbeatTimeout;
    }

    private void LogHeartbeatReceivedLocked(DateTimeOffset now, OptionsSnapshot snapshot)
    {
        var resumed = !_heartbeatActive;
        _heartbeatActive = true;
        _safetyLog.Record(SafetyLogEntry.HeartbeatReceived(now, snapshot.HeartbeatTimeout, resumed));
    }

    private void EnsureHeartbeatExpiredLocked(DateTimeOffset now, OptionsSnapshot snapshot, string reason)
    {
        if (_heartbeatActive)
        {
            _heartbeatActive = false;
            _safetyLog.Record(SafetyLogEntry.HeartbeatExpired(now, snapshot.HeartbeatTimeout, reason));
        }
    }

    private SteerCmd BuildSafeSteerCommand(SteerCmd? template, OptionsSnapshot snapshot)
    {
        return snapshot.SteerFailsafe switch
        {
            AgioSafetyOptions.FailsafeAction.Hold => _lastSteerCommand?.Clone() ?? DisableSteer(template),
            _ => DisableSteer(template),
        };
    }

    private SectionMask BuildSafeSectionMask(SectionMask? template, OptionsSnapshot snapshot)
    {
        return snapshot.SectionFailsafe switch
        {
            AgioSafetyOptions.FailsafeAction.Hold => _lastSectionMask?.Clone() ?? DisableSections(template),
            _ => DisableSections(template),
        };
    }

    private static SteerCmd DisableSteer(SteerCmd? template)
    {
        var safe = CloneSteerOrCreate(template);
        safe.Enable = false;
        safe.TargetWheelAngleDeg = 0;
        safe.FeedForward = 0;
        safe.ControllerOutput = 0;
        return safe;
    }

    private static SectionMask DisableSections(SectionMask? template)
    {
        var safe = CloneSectionOrCreate(template);
        safe.Mask = 0;
        return safe;
    }

    private static SteerCmd CloneSteerOrCreate(SteerCmd? source) => source is null ? new SteerCmd() : source.Clone();

    private static SectionMask CloneSectionOrCreate(SectionMask? source) => source is null ? new SectionMask() : source.Clone();

    private static OptionsSnapshot CreateSnapshot(AgioSafetyOptions? options)
    {
        if (options is null)
        {
            options = new AgioSafetyOptions();
        }

        options.Failsafe ??= new AgioSafetyOptions.FailsafeSettings();
        return new OptionsSnapshot(options.HeartbeatTimeout, options.Failsafe.Steer, options.Failsafe.Sections);
    }

    private sealed record OptionsSnapshot(
        TimeSpan HeartbeatTimeout,
        AgioSafetyOptions.FailsafeAction SteerFailsafe,
        AgioSafetyOptions.FailsafeAction SectionFailsafe);

    private sealed class NullSafetyLog : ISafetyLog
    {
        public static NullSafetyLog Instance { get; } = new();

        public void Record(SafetyLogEntry entry)
        {
        }

        public string Export(string destinationDirectory) => throw new InvalidOperationException("Safety log export is not configured.");
    }
}
