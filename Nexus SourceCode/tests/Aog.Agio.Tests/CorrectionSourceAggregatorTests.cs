using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Corrections;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class CorrectionSourceAggregatorTests
{
    [Fact]
    public async Task RunAsync_FallsBackWhenRadioSourceFaults()
    {
        var radioSource = new FakeCorrectionSource(
            "radio/primary",
            CorrectionSourceResult.Faulted(new InvalidOperationException("radio failure")));

        var networkSource = new FakeCorrectionSource(
            "ntrip",
            CorrectionSourceResult.Cancelled(),
            new[] { new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02 }) });

        var radioFactory = new FakeCorrectionSourceFactory(
            CorrectionSourceKind.SerialRadio,
            "radio",
            _ => ValueTask.FromResult<ICorrectionSource?>(radioSource));

        var networkFactory = new FakeCorrectionSourceFactory(
            CorrectionSourceKind.NetworkService,
            "network",
            _ => ValueTask.FromResult<ICorrectionSource?>(networkSource));

        var aggregator = CreateAggregator(
            new[] { radioFactory, networkFactory },
            options =>
            {
                options.SourceFailureBackoff = TimeSpan.Zero;
                options.ExhaustedBackoff = TimeSpan.Zero;
            });

        var published = new List<byte[]>();
        using var cts = new CancellationTokenSource();
        var runTask = aggregator.RunAsync(
            (payload, token) =>
            {
                published.Add(payload.ToArray());
                return ValueTask.CompletedTask;
            },
            cts.Token);

        await WaitUntilAsync(() => published.Count >= 1, TimeSpan.FromSeconds(1));
        cts.Cancel();

        var result = await runTask;

        Assert.Equal(CorrectionSourceOutcome.Cancelled, result.Outcome);
        Assert.NotEmpty(published);
        Assert.True(radioSource.RunInvoked);
        Assert.True(radioSource.Disposed);
        Assert.True(networkSource.RunInvoked);
        Assert.True(networkSource.Disposed);
        Assert.True(radioFactory.InvocationCount >= 1);
        Assert.True(networkFactory.InvocationCount >= 1);
    }

    [Fact]
    public async Task RunAsync_UsesNetworkWhenEnabledAndLocalUnavailable()
    {
        var baseFactory = new FakeCorrectionSourceFactory(
            CorrectionSourceKind.LocalBaseStation,
            "base",
            _ => ValueTask.FromResult<ICorrectionSource?>(null));

        var networkSource = new FakeCorrectionSource(
            "ntrip",
            CorrectionSourceResult.Cancelled(),
            new[] { new ReadOnlyMemory<byte>(new byte[] { 0x42 }) });

        var networkFactory = new FakeCorrectionSourceFactory(
            CorrectionSourceKind.NetworkService,
            "network",
            _ => ValueTask.FromResult<ICorrectionSource?>(networkSource));

        var aggregator = CreateAggregator(
            new[] { baseFactory, networkFactory },
            options =>
            {
                options.EnableNetworkSources = true;
                options.SourceFailureBackoff = TimeSpan.Zero;
                options.ExhaustedBackoff = TimeSpan.Zero;
                options.PreferredOrder = new List<CorrectionSourceKind>
                {
                    CorrectionSourceKind.LocalBaseStation,
                    CorrectionSourceKind.NetworkService,
                };
            });

        var published = new List<byte[]>();
        using var cts = new CancellationTokenSource();
        var runTask = aggregator.RunAsync(
            (payload, token) =>
            {
                published.Add(payload.ToArray());
                return ValueTask.CompletedTask;
            },
            cts.Token);

        await WaitUntilAsync(() => published.Count >= 1, TimeSpan.FromSeconds(1));
        cts.Cancel();

        var result = await runTask;

        Assert.Equal(CorrectionSourceOutcome.Cancelled, result.Outcome);
        Assert.NotEmpty(published);
        Assert.True(baseFactory.InvocationCount >= 1);
        Assert.True(networkFactory.InvocationCount >= 1);
    }

    [Fact]
    public async Task RunAsync_ThrowsWhenOnlyNetworkSourcesButPolicyDisallowsThem()
    {
        var networkFactory = new FakeCorrectionSourceFactory(
            CorrectionSourceKind.NetworkService,
            "network",
            _ => ValueTask.FromResult<ICorrectionSource?>(null));

        var aggregator = CreateAggregator(
            new[] { networkFactory },
            options =>
            {
                options.EnableNetworkSources = false;
                options.PreferredOrder = new List<CorrectionSourceKind>
                {
                    CorrectionSourceKind.LocalBaseStation,
                    CorrectionSourceKind.SerialRadio,
                    CorrectionSourceKind.NetworkService,
                };
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            aggregator.RunAsync((payload, token) => ValueTask.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WaitsBeforeRetryingWhenNoSourcesAvailable()
    {
        var radioFactory = new FakeCorrectionSourceFactory(
            CorrectionSourceKind.SerialRadio,
            "radio",
            _ => ValueTask.FromResult<ICorrectionSource?>(null));

        var timeProvider = new RecordingTimeProvider();

        var aggregator = CreateAggregator(
            new[] { radioFactory },
            options =>
            {
                options.EnableNetworkSources = true;
                options.SourceFailureBackoff = TimeSpan.Zero;
                options.ExhaustedBackoff = TimeSpan.FromSeconds(3);
            },
            timeProvider);

        using var cts = new CancellationTokenSource();
        var runTask = aggregator.RunAsync((payload, token) => ValueTask.CompletedTask, cts.Token);

        await WaitUntilAsync(() => timeProvider.Delays.Count > 0, TimeSpan.FromSeconds(1));
        cts.Cancel();

        var result = await runTask;
        Assert.Equal(CorrectionSourceOutcome.Cancelled, result.Outcome);
        Assert.Contains(TimeSpan.FromSeconds(3), timeProvider.Delays);
    }

    private static CorrectionSourceAggregator CreateAggregator(
        IEnumerable<ICorrectionSourceFactory> factories,
        Action<CorrectionSourceAggregatorOptions>? configure,
        TimeProvider? timeProvider = null)
    {
        var options = new CorrectionSourceAggregatorOptions();
        configure?.Invoke(options);
        var logger = new TestLogger<CorrectionSourceAggregator>();
        return new CorrectionSourceAggregator(factories, Options.Create(options), logger, timeProvider);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (!predicate())
        {
            if (sw.Elapsed > timeout)
            {
                throw new TimeoutException("Condition was not met within the allotted time.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class FakeCorrectionSourceFactory : ICorrectionSourceFactory
    {
        private readonly Func<CancellationToken, ValueTask<ICorrectionSource?>> _creator;

        public FakeCorrectionSourceFactory(
            CorrectionSourceKind kind,
            string name,
            Func<CancellationToken, ValueTask<ICorrectionSource?>> creator)
        {
            Kind = kind;
            Name = name;
            _creator = creator;
        }

        public string Name { get; }

        public CorrectionSourceKind Kind { get; }

        public int InvocationCount { get; private set; }

        public async ValueTask<ICorrectionSource?> TryCreateAsync(CancellationToken cancellationToken)
        {
            InvocationCount++;
            return await _creator(cancellationToken);
        }
    }

    private sealed class FakeCorrectionSource : ICorrectionSource
    {
        private readonly IReadOnlyList<ReadOnlyMemory<byte>> _payloads;
        private readonly CorrectionSourceResult _result;

        public FakeCorrectionSource(
            string name,
            CorrectionSourceResult result,
            IReadOnlyList<ReadOnlyMemory<byte>>? payloads = null)
        {
            Name = name;
            _result = result;
            _payloads = payloads ?? Array.Empty<ReadOnlyMemory<byte>>();
        }

        public string Name { get; }

        public bool RunInvoked { get; private set; }

        public bool Disposed { get; private set; }

        public async Task<CorrectionSourceResult> RunAsync(
            Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> publish,
            CancellationToken cancellationToken)
        {
            RunInvoked = true;
            foreach (var payload in _payloads)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await publish(payload, cancellationToken);
            }

            return _result;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingTimeProvider : TimeProvider
    {
        public List<TimeSpan> Delays { get; } = new();

        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

        public override long GetTimestamp() => Stopwatch.GetTimestamp();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Delays.Add(dueTime);
            return new CallbackTimer(callback, state);
        }

        private sealed class CallbackTimer : ITimer
        {
            private readonly TimerCallback _callback;
            private readonly object? _state;

            public CallbackTimer(TimerCallback callback, object? state)
            {
                _callback = callback ?? throw new ArgumentNullException(nameof(callback));
                _state = state;
                _callback(_state);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (dueTime == Timeout.InfiniteTimeSpan)
                {
                    return true;
                }

                _callback(_state);
                return true;
            }

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
