using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Agio.Legacy;

/// <summary>
/// Bridges legacy UDP PGNs to typed gRPC contracts and vice versa.
/// </summary>
public sealed class LegacyUdpGateway
{
    private const string LegacyGpsSource = "legacy/udp/main_gps";
    private readonly LegacyPoseCodec _codec;
    private readonly ILegacyUdpTransport _transport;
    private readonly ILegacyPoseObserver _poseObserver;
    private readonly TimeProvider _timeProvider;
    private long _sequence;

    public LegacyUdpGateway(
        LegacyPoseCodec codec,
        ILegacyUdpTransport transport,
        ILegacyPoseObserver poseObserver,
        TimeProvider timeProvider)
    {
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _poseObserver = poseObserver ?? throw new ArgumentNullException(nameof(poseObserver));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Encodes a typed pose into its legacy PGN representation and transmits it.
    /// </summary>
    public async ValueTask PublishPoseAsync(Pose pose, LegacyPoseMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        if (pose is null)
        {
            throw new ArgumentNullException(nameof(pose));
        }

        var frame = _codec.EncodePose(pose, metadata);
        await _transport.SendAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to decode a legacy datagram and forwards the resulting pose to observers.
    /// </summary>
    public async ValueTask HandleDatagramAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default)
    {
        if (!_codec.TryDecodePose(datagram.Span, out var pose, out var metadata))
        {
            return;
        }

        pose.Header ??= new Header();
        pose.Header.Source = LegacyGpsSource;
        pose.Header.Frame = "earth";
        pose.Header.Sequence = (ulong)Interlocked.Increment(ref _sequence);
        pose.Header.Timestamp = Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow());

        await _poseObserver.OnPoseAsync(pose, metadata, cancellationToken).ConfigureAwait(false);
    }
}
