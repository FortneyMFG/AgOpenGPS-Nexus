using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Mesh;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Mesh;

public sealed class MeshRetentionWorkerTests
{
    [Fact]
    public async Task StartAsync_CapturesPublicationsAndPublishesEvents()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 24, 10, 0, 0, TimeSpan.Zero));
        var mesh = new LiveTelemetryMeshService(clock);
        var store = new MeshRetentionStore(timeProvider: clock);
        var eventBus = new InMemoryEventBus();
        await using var worker = new MeshRetentionWorker(mesh, store, eventBus: eventBus, timeProvider: clock);

        var completion = new TaskCompletionSource<MeshTelemetryEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        eventBus.Subscribe<MeshTelemetryEvent>((evt, token) =>
        {
            completion.TrySetResult(evt);
            return ValueTask.CompletedTask;
        });

        await worker.StartAsync();

        await mesh.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
            "publisher",
            "Combine",
            ShareProfile: new MeshShareProfile(new[]
            {
                new MeshShareGrant("season:2025", "job:alpha", MeshDataTier.Coverage, new[] { "coverage" })
            })),
            default);

        var metadata = new Dictionary<string, string> { ["quality"] = "high" };
        await mesh.PublishAsync(new MeshPublishRequest(
            "publisher",
            "aog/live/season:2025/job:alpha/coverage",
            MeshDataTier.Coverage,
            new byte[] { 0x42 },
            clock.GetUtcNow(),
            metadata));

        var completed = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        completed.Should().Be(completion.Task);

        var telemetryEvent = await completion.Task;
        telemetryEvent.Topic.Should().Be("aog/live/season:2025/job:alpha/coverage");
        telemetryEvent.Payload.Should().Equal(0x42);
        telemetryEvent.MetadataJson.Should().Contain("quality");

        var retained = store.Query();
        retained.Should().HaveCount(1);
        retained[0].PublisherDeviceId.Should().Be("publisher");
    }
}
