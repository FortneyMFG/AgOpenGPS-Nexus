using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Core.Host.Tests;

public sealed class CoreHealthServiceTests
{
    [Fact]
    public async Task HealthService_AppliesUpdatedInterval_OnOptionsChange()
    {
        var logger = new TestLogger<CoreHealthService>();
        var optionsMonitor = new MutableOptionsMonitor<CoreHealthOptions>(new CoreHealthOptions
        {
            IntervalSeconds = 2,
        });
        var timeProvider = new TestTimeProvider();
        var service = new CoreHealthService(logger, optionsMonitor, timeProvider);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            await service.StartAsync(cts.Token).ConfigureAwait(false);

            var firstDelay = await timeProvider.WaitForDelayAsync(cts.Token).ConfigureAwait(false);
            Assert.Equal(TimeSpan.FromSeconds(2), firstDelay.Delay);

            optionsMonitor.Update(new CoreHealthOptions { IntervalSeconds = 5 });
            firstDelay.Complete();

            var secondDelay = await timeProvider.WaitForDelayAsync(cts.Token).ConfigureAwait(false);
            Assert.Equal(TimeSpan.FromSeconds(5), secondDelay.Delay);
            Assert.Contains(
                logger.Messages,
                message => message.Contains("Core host health interval updated to 5s.", StringComparison.Ordinal));

            secondDelay.Complete();

            // Allow the service to schedule the next delay so StopAsync can cancel it cleanly.
            _ = await timeProvider.WaitForDelayAsync(cts.Token).ConfigureAwait(false);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
            service.Dispose();
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }

        public List<string> Messages { get; } = new();

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class MutableOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
        where TOptions : class
    {
        private TOptions _currentValue;
        private readonly object _gate = new();
        private event Action<TOptions, string?>? _listeners;

        public MutableOptionsMonitor(TOptions initialValue)
        {
            _currentValue = initialValue;
        }

        public TOptions CurrentValue
        {
            get
            {
                lock (_gate)
                {
                    return _currentValue;
                }
            }
        }

        public TOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<TOptions, string?> listener)
        {
            lock (_gate)
            {
                _listeners += listener;
            }

            return new ListenerHandle(this, listener);
        }

        public void Update(TOptions value)
        {
            Action<TOptions, string?>? listeners;
            lock (_gate)
            {
                _currentValue = value;
                listeners = _listeners;
            }

            listeners?.Invoke(value, Options.DefaultName);
        }

        private sealed class ListenerHandle : IDisposable
        {
            private MutableOptionsMonitor<TOptions>? _owner;
            private Action<TOptions, string?>? _listener;

            public ListenerHandle(MutableOptionsMonitor<TOptions> owner, Action<TOptions, string?> listener)
            {
                _owner = owner;
                _listener = listener;
            }

            public void Dispose()
            {
                var owner = Interlocked.Exchange(ref _owner, null);
                var listener = Interlocked.Exchange(ref _listener, null);

                if (owner is null || listener is null)
                {
                    return;
                }

                owner.RemoveListener(listener);
            }
        }

        private void RemoveListener(Action<TOptions, string?> listener)
        {
            lock (_gate)
            {
                _listeners -= listener;
            }
        }
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private readonly ConcurrentQueue<DelayRequest> _requests = new();
        private readonly SemaphoreSlim _signal = new(0);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var request = new DelayRequest(dueTime, callback, state);
            _requests.Enqueue(request);
            _signal.Release();
            return new TestTimer(request);
        }

        public async Task<DelayRequest> WaitForDelayAsync(CancellationToken cancellationToken = default)
        {
            await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (_requests.TryDequeue(out var request))
            {
                return request;
            }

            throw new InvalidOperationException("No delay request available.");
        }

        public sealed class DelayRequest
        {
            private readonly TimerCallback _callback;
            private readonly object? _state;
            private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            internal DelayRequest(TimeSpan delay, TimerCallback callback, object? state)
            {
                Delay = delay;
                _callback = callback;
                _state = state;
            }

            public TimeSpan Delay { get; private set; }

            public Task Task => _tcs.Task;

            internal void UpdateDelay(TimeSpan delay) => Delay = delay;

            public void Complete()
            {
                _callback(_state);
                _tcs.TrySetResult(true);
            }

            public bool TryCancel(CancellationToken cancellationToken)
            {
                return _tcs.TrySetCanceled(cancellationToken);
            }
        }

        private sealed class TestTimer : ITimer
        {
            private DelayRequest _request;
            private bool _disposed;

            public TestTimer(DelayRequest request)
            {
                _request = request;
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (_disposed)
                {
                    return false;
                }

                _request.UpdateDelay(dueTime);
                return true;
            }

            public void Dispose()
            {
                DisposeAsync().GetAwaiter().GetResult();
            }

            public ValueTask DisposeAsync()
            {
                if (_disposed)
                {
                    return ValueTask.CompletedTask;
                }

                _disposed = true;
                _request.TryCancel(CancellationToken.None);
                return ValueTask.CompletedTask;
            }
        }
    }
}
