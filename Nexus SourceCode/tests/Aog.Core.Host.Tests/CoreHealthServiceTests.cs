using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            IntervalSeconds = 2
        });
        var timeProvider = new TestTimeProvider();
        var service = new CoreHealthService(logger, optionsMonitor, timeProvider);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            await service.StartAsync(cts.Token);

            var firstDelay = await timeProvider.WaitForDelayAsync(cts.Token);
            Assert.Equal(TimeSpan.FromSeconds(2), firstDelay.Delay);

            optionsMonitor.Update(new CoreHealthOptions { IntervalSeconds = 5 });
            firstDelay.Complete();

            var secondDelay = await timeProvider.WaitForDelayAsync(cts.Token);
            Assert.Equal(TimeSpan.FromSeconds(5), secondDelay.Delay);
            Assert.Contains(logger.Messages, message => message.Contains("Core host health interval updated to 5s.", StringComparison.Ordinal));

            secondDelay.Complete();

            // Allow the service to schedule the next delay so StopAsync can cancel it cleanly.
            _ = await timeProvider.WaitForDelayAsync(cts.Token);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
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

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
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

        public override ValueTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            var request = new DelayRequest(delay);
            _requests.Enqueue(request);
            _signal.Release();

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(static state =>
                {
                    var tuple = (Tuple<DelayRequest, CancellationToken>)state!;
                    tuple.Item1.TryCancel(tuple.Item2);
                }, Tuple.Create(request, cancellationToken));
            }

            return new ValueTask(request.Task);
        }

        public async Task<DelayRequest> WaitForDelayAsync(CancellationToken cancellationToken = default)
        {
            await _signal.WaitAsync(cancellationToken);

            if (_requests.TryDequeue(out var request))
            {
                return request;
            }

            throw new InvalidOperationException("No delay request available.");
        }

        public sealed class DelayRequest
        {
            private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public DelayRequest(TimeSpan delay)
            {
                Delay = delay;
            }

            public TimeSpan Delay { get; }

            public Task Task => _tcs.Task;

            public void Complete() => _tcs.TrySetResult(true);

            public void TryCancel(CancellationToken token) => _tcs.TrySetCanceled(token);
        }
    }
}
