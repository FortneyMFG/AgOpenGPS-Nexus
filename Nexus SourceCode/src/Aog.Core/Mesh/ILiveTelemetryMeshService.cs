using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Mesh;

/// <summary>
/// Defines the contract for the in-memory live telemetry mesh core service.
/// </summary>
public interface ILiveTelemetryMeshService
{
    /// <summary>
    /// Registers or updates the configuration for a mesh device.
    /// </summary>
    /// <param name="registration">Registration payload containing device metadata and access profiles.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask RegisterOrUpdateDeviceAsync(MeshDeviceRegistration registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a payload onto the live mesh.
    /// </summary>
    /// <param name="request">Publication request.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask PublishAsync(MeshPublishRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes the calling device to mesh publications matching its access profile.
    /// </summary>
    /// <param name="request">Subscription request describing optional topic filters.</param>
    /// <param name="cancellationToken">Token used to cancel the subscription.</param>
    IAsyncEnumerable<MeshPublication> SubscribeAsync(MeshSubscriptionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the presence heartbeat for a device and broadcasts it on the mesh.
    /// </summary>
    /// <param name="update">Presence update payload.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask UpdatePresenceAsync(MeshPresenceUpdate update, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the most recent presence snapshots, excluding entries that have expired.
    /// </summary>
    /// <param name="seasonId">Optional season filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    IReadOnlyList<MeshPresenceSnapshot> ListPresence(string? seasonId = null, string? jobId = null);
}
