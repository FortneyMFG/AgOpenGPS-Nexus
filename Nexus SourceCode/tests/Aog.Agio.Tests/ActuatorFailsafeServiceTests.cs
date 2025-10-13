using System;
using System.Collections.Generic;
using Aog.Core.V1;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class ActuatorFailsafeServiceTests
{
    [Fact]
    public void Commands_Disabled_When_No_Heartbeat()
    {
        var optionsMonitor = new TestOptionsMonitor<AgioSafetyOptions>(new AgioSafetyOptions());
        var timeProvider = new FakeTimeProvider();
        using var service = new ActuatorFailsafeService(optionsMonitor, timeProvider);

        var steer = new SteerCmd
        {
            Enable = true,
            TargetWheelAngleDeg = 12.5,
            FeedForward = 1.2,
            ControllerOutput = 0.8,
        };

        var filteredSteer = service.FilterSteerCommand(steer);

        Assert.False(filteredSteer.Enable);
        Assert.Equal(0, filteredSteer.TargetWheelAngleDeg);
        Assert.Equal(0, filteredSteer.FeedForward);
        Assert.Equal(0, filteredSteer.ControllerOutput);
        Assert.True(steer.Enable); // Original command untouched.

        var sections = new SectionMask { SectionCount = 8, Mask = 0b1111_1111 };
        var filteredSections = service.FilterSectionMask(sections);

        Assert.Equal(0u, filteredSections.Mask);
        Assert.Equal((uint)8, filteredSections.SectionCount);
        Assert.False(service.HasActiveHeartbeat);
        Assert.Null(service.LastHeartbeatUtc);
    }

    [Fact]
    public void Heartbeat_Allows_Commands_Until_Timeout()
    {
        var options = new AgioSafetyOptions { HeartbeatMs = 200 };
        var optionsMonitor = new TestOptionsMonitor<AgioSafetyOptions>(options);
        var timeProvider = new FakeTimeProvider();
        using var service = new ActuatorFailsafeService(optionsMonitor, timeProvider);

        service.ReportHeartbeat();

        var steer = new SteerCmd { Enable = true, TargetWheelAngleDeg = 5 }; // Clone stored.
        var filteredSteer = service.FilterSteerCommand(steer);

        Assert.Same(steer, filteredSteer);
        Assert.True(service.HasActiveHeartbeat);

        timeProvider.Advance(TimeSpan.FromMilliseconds(150));
        Assert.True(service.HasActiveHeartbeat);

        timeProvider.Advance(TimeSpan.FromMilliseconds(75));
        Assert.False(service.HasActiveHeartbeat);

        var safeSteer = service.FilterSteerCommand(new SteerCmd { Enable = true, TargetWheelAngleDeg = 1 });
        Assert.False(safeSteer.Enable);
    }

    [Fact]
    public void Hold_Failsafe_Preserves_Last_Commands()
    {
        var options = new AgioSafetyOptions
        {
            HeartbeatMs = 100,
            Failsafe = new AgioSafetyOptions.FailsafeSettings
            {
                Steer = AgioSafetyOptions.FailsafeAction.Hold,
                Sections = AgioSafetyOptions.FailsafeAction.Hold,
            },
        };
        var optionsMonitor = new TestOptionsMonitor<AgioSafetyOptions>(options);
        var timeProvider = new FakeTimeProvider();
        using var service = new ActuatorFailsafeService(optionsMonitor, timeProvider);

        service.ReportHeartbeat();

        var steer = new SteerCmd { Enable = true, TargetWheelAngleDeg = 7.5 };
        var mask = new SectionMask { SectionCount = 4, Mask = 0b0011 };

        service.FilterSteerCommand(steer);
        service.FilterSectionMask(mask);

        timeProvider.Advance(TimeSpan.FromMilliseconds(200));
        Assert.False(service.HasActiveHeartbeat);

        var heldSteer = service.FilterSteerCommand(new SteerCmd { Enable = true });
        var heldMask = service.FilterSectionMask(new SectionMask { SectionCount = 4 });

        Assert.Equal(7.5, heldSteer.TargetWheelAngleDeg);
        Assert.True(heldSteer.Enable);
        Assert.Equal((uint)0b0011, heldMask.Mask);
    }

    [Fact]
    public void Options_Update_Takes_Effect_Immediately()
    {
        var options = new AgioSafetyOptions { HeartbeatMs = 100 };
        var optionsMonitor = new TestOptionsMonitor<AgioSafetyOptions>(options);
        var timeProvider = new FakeTimeProvider();
        using var service = new ActuatorFailsafeService(optionsMonitor, timeProvider);

        Assert.Equal(TimeSpan.FromMilliseconds(100), service.HeartbeatTimeout);

        optionsMonitor.Update(new AgioSafetyOptions { HeartbeatMs = 400 });

        Assert.Equal(TimeSpan.FromMilliseconds(400), service.HeartbeatTimeout);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private T _currentValue;
        private readonly List<Action<T, string?>> _listeners = new();

        public TestOptionsMonitor(T currentValue)
        {
            _currentValue = currentValue ?? throw new ArgumentNullException(nameof(currentValue));
        }

        public T CurrentValue => _currentValue;

        public T Get(string? name) => _currentValue;

        public IDisposable OnChange(Action<T, string?> listener)
        {
            ArgumentNullException.ThrowIfNull(listener);
            _listeners.Add(listener);
            return new ChangeRegistration(this, listener);
        }

        public void Update(T value)
        {
            _currentValue = value ?? throw new ArgumentNullException(nameof(value));
            foreach (var listener in _listeners.ToArray())
            {
                listener(_currentValue, null);
            }
        }

        private void Remove(Action<T, string?> listener) => _listeners.Remove(listener);

        private sealed class ChangeRegistration : IDisposable
        {
            private readonly TestOptionsMonitor<T> _monitor;
            private readonly Action<T, string?> _listener;
            private bool _disposed;

            public ChangeRegistration(TestOptionsMonitor<T> monitor, Action<T, string?> listener)
            {
                _monitor = monitor;
                _listener = listener;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _monitor.Remove(_listener);
                _disposed = true;
            }
        }
    }
}
