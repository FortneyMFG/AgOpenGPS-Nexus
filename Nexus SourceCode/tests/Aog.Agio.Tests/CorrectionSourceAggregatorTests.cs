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
        var result = await aggregator.RunAsync(
            (payload, token) =>
            {
                published.Add(payload.ToArray());
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(CorrectionSourceOutcome.Cancelled, result.Outcome);
        Assert.Single(published);
        Assert.True(radioSource.RunInvoked);
        Assert.True(radioSource.Disposed);
        Assert.True(networkSource.RunInvoked);
        Assert.True(networkSource.Disposed);
        Assert.Equal(1, radioFactory.InvocationCount);
        Assert.Equal(1, networkFactory.InvocationCount);
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
        var result = await aggregator.RunAsync(
            (payload, token) =>
            {
                published.Add(payload.ToArray());
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(CorrectionSourceOutcome.Cancelled, result.Outcome);
        Assert.Single(published);
        Assert.Equal(1, baseFactory.InvocationCount);
        Assert.Equal(1, networkFactory.InvocationCount);
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
        private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

        public List<TimeSpan> Delays { get; } = new();

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override long GetTimestamp() => Stopwatch.GetTimestamp();

        public override TimeSpan GetElapsedTime(long startingTimestamp) => TimeProvider.System.GetElapsedTime(startingTimestamp);

        public override TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
            TimeProvider.System.GetElapsedTime(startingTimestamp, endingTimestamp);

        public override ValueTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            Delays.Add(delay);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }
}
