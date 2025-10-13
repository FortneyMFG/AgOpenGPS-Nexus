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
    private readonly LegacyPoseCodec _poseCodec;
    private readonly LegacyAutoSteerCodec _autoSteerCodec;
    private readonly ILegacyUdpTransport _transport;
    private readonly ILegacyPoseObserver _poseObserver;
    private readonly TimeProvider _timeProvider;
    private readonly object _stateLock = new();
    private long _sequence;
    private SteerCmd _steerSnapshot = new();
    private SectionMask _sectionSnapshot = new();

    public LegacyUdpGateway(
        LegacyPoseCodec poseCodec,
        LegacyAutoSteerCodec autoSteerCodec,
        ILegacyUdpTransport transport,
        ILegacyPoseObserver poseObserver,
        TimeProvider timeProvider)
    {
        _poseCodec = poseCodec ?? throw new ArgumentNullException(nameof(poseCodec));
        _autoSteerCodec = autoSteerCodec ?? throw new ArgumentNullException(nameof(autoSteerCodec));
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

        var frame = _poseCodec.EncodePose(pose, metadata);
        await _transport.SendAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to decode a legacy datagram and forwards the resulting pose to observers.
    /// </summary>
    public async ValueTask HandleDatagramAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default)
    {
        if (!_poseCodec.TryDecodePose(datagram.Span, out var pose, out var metadata))
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

    /// <summary>
    /// Encodes the supplied steer command into the legacy auto-steer PGN and transmits it.
    /// </summary>
    public async ValueTask PublishSteerCommandAsync(SteerCmd command, CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        byte[] frame;
        lock (_stateLock)
        {
            _steerSnapshot = command.Clone();
            frame = _autoSteerCodec.EncodeAutoSteerFrame(_steerSnapshot, _sectionSnapshot);
        }

        await _transport.SendAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the section mask snapshot and emits the combined auto-steer PGN.
    /// </summary>
    public async ValueTask PublishSectionMaskAsync(SectionMask mask, CancellationToken cancellationToken = default)
    {
        if (mask is null)
        {
            throw new ArgumentNullException(nameof(mask));
        }

        byte[] frame;
        lock (_stateLock)
        {
            _sectionSnapshot = mask.Clone();
            frame = _autoSteerCodec.EncodeAutoSteerFrame(_steerSnapshot, _sectionSnapshot);
        }

        await _transport.SendAsync(frame, cancellationToken).ConfigureAwait(false);
    }
}
