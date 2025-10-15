using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.AogLink;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.Agio.Tests.AogLink;

public sealed class AogLinkTransportManagerTests
{
    [Fact]
    public async Task StartAsync_OnlyStartsEnabledDrivers()
    {
        var enabled = new TestDriver("enabled", isEnabled: true);
        var disabled = new TestDriver("disabled", isEnabled: false);
        var manager = new AogLinkTransportManager(new IAogLinkTransportDriver[] { enabled, disabled }, NullLogger<AogLinkTransportManager>.Instance);

        await manager.StartAsync(CancellationToken.None);
        await Task.Delay(10);
        await manager.StopAsync(CancellationToken.None);

        enabled.StartCount.Should().Be(1);
        enabled.StopCount.Should().Be(1);
        disabled.StartCount.Should().Be(0);
    }

    private sealed class TestDriver : IAogLinkTransportDriver
    {
        private readonly List<AogLinkFrame> _frames = new();

        public TestDriver(string name, bool isEnabled)
        {
            Name = name;
            IsEnabled = isEnabled;
        }

        public string Name { get; }

        public bool IsEnabled { get; }

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCount++;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
            return Task.CompletedTask;
        }

        public ValueTask SendAsync(AogLinkFrame frame, CancellationToken cancellationToken)
        {
            _frames.Add(frame);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<AogLinkFrame> ReceiveAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var frame in _frames)
            {
                yield return frame;
                await Task.Yield();
            }
        }
    }
}
