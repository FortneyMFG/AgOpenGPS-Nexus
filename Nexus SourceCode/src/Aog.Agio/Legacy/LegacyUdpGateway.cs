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
    private const string LegacySteerCommandSource = "legacy/udp/steer_cmd";
    private const string LegacySteerStateSource = "legacy/udp/steer_state";
    private const string LegacySectionSource = "legacy/udp/sections";
    private readonly LegacyPoseCodec _poseCodec;
    private readonly LegacyDiscoveryCodec _discoveryCodec;
    private readonly LegacySteerCodec _steerCodec;
    private readonly ILegacyUdpTransport _transport;
    private readonly ILegacyPoseObserver _poseObserver;
    private readonly ILegacyDiscoveryObserver _discoveryObserver;
    private readonly ILegacySteerCommandObserver _steerCommandObserver;
    private readonly ILegacySteerStateObserver _steerStateObserver;
    private readonly ILegacySectionObserver _sectionObserver;
    private readonly ILegacyMeshPresencePublisher _meshPresencePublisher;
    private readonly TimeProvider _timeProvider;
    private long _sequence;
    private long _steerCommandSequence;
    private long _steerStateSequence;
    private long _sectionSequence;

    public LegacyUdpGateway(
        LegacyPoseCodec poseCodec,
        LegacyDiscoveryCodec discoveryCodec,
        LegacySteerCodec steerCodec,
        ILegacyUdpTransport transport,
        ILegacyPoseObserver poseObserver,
        ILegacyDiscoveryObserver discoveryObserver,
        ILegacySteerCommandObserver steerCommandObserver,
        ILegacySteerStateObserver steerStateObserver,
        ILegacySectionObserver sectionObserver,
        ILegacyMeshPresencePublisher meshPresencePublisher,
        TimeProvider timeProvider)
    {
        _poseCodec = poseCodec ?? throw new ArgumentNullException(nameof(poseCodec));
        _discoveryCodec = discoveryCodec ?? throw new ArgumentNullException(nameof(discoveryCodec));
        _steerCodec = steerCodec ?? throw new ArgumentNullException(nameof(steerCodec));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _poseObserver = poseObserver ?? throw new ArgumentNullException(nameof(poseObserver));
        _discoveryObserver = discoveryObserver ?? throw new ArgumentNullException(nameof(discoveryObserver));
        _steerCommandObserver = steerCommandObserver ?? throw new ArgumentNullException(nameof(steerCommandObserver));
        _steerStateObserver = steerStateObserver ?? throw new ArgumentNullException(nameof(steerStateObserver));
        _sectionObserver = sectionObserver ?? throw new ArgumentNullException(nameof(sectionObserver));
        _meshPresencePublisher = meshPresencePublisher ?? throw new ArgumentNullException(nameof(meshPresencePublisher));
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
    /// Encodes a typed steering command (optionally including section state) and transmits it.
    /// </summary>
    /// <param name="command">Steering command to encode.</param>
    /// <param name="sections">Optional section mask to include in the PGN.</param>
    /// <param name="metadata">Optional legacy metadata overrides.</param>
    /// <param name="cancellationToken">Cancellation token for the send operation.</param>
    public async ValueTask PublishSteerCommandAsync(
        SteerCmd command,
        SectionMask? sections = null,
        LegacySteerCommandMetadata? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var frame = _steerCodec.EncodeSteerCommand(command, sections, metadata);
        await _transport.SendAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes a discovery announcement over the UDP transport.
    /// </summary>
    /// <param name="announcement">Discovery announcement to broadcast.</param>
    /// <param name="cancellationToken">Cancellation token for the broadcast operation.</param>
    public async ValueTask PublishDiscoveryAsync(LegacyDiscoveryAnnouncement announcement, CancellationToken cancellationToken = default)
    {
        if (announcement is null)
        {
            throw new ArgumentNullException(nameof(announcement));
        }

        var frame = _discoveryCodec.Encode(announcement);
        await _transport.SendAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to decode a legacy datagram and forwards the resulting pose to observers.
    /// </summary>
    public async ValueTask HandleDatagramAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default)
    {
        if (_discoveryCodec.TryDecode(datagram.Span, out var announcement))
        {
            await _meshPresencePublisher.OnDiscoveryAsync(announcement, cancellationToken).ConfigureAwait(false);
            await _discoveryObserver.OnDiscoveryAsync(announcement, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_steerCodec.TryDecodeSteerState(datagram.Span, out var steerState, out var steerStateMetadata))
        {
            steerState.Header ??= new Header();
            steerState.Header.Source = LegacySteerStateSource;
            steerState.Header.Frame = "vehicle";
            steerState.Header.Sequence = (ulong)Interlocked.Increment(ref _steerStateSequence);
            steerState.Header.Timestamp = Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow());

            await _steerStateObserver.OnSteerStateAsync(steerState, steerStateMetadata, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_steerCodec.TryDecodeSteerCommand(datagram.Span, out var steerCommand, out var steerMetadata, out var sectionMask))
        {
            steerCommand.Header ??= new Header();
            steerCommand.Header.Source = LegacySteerCommandSource;
            steerCommand.Header.Frame = "vehicle";
            steerCommand.Header.Sequence = (ulong)Interlocked.Increment(ref _steerCommandSequence);
            steerCommand.Header.Timestamp = Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow());

            await _steerCommandObserver.OnSteerCommandAsync(steerCommand, steerMetadata, cancellationToken).ConfigureAwait(false);

            if (sectionMask.SectionCount != 0)
            {
                sectionMask.Header ??= new Header();
                sectionMask.Header.Source = LegacySectionSource;
                sectionMask.Header.Frame = "implement";
                sectionMask.Header.Sequence = (ulong)Interlocked.Increment(ref _sectionSequence);
                sectionMask.Header.Timestamp = Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow());

                await _sectionObserver.OnSectionMaskAsync(sectionMask, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

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
        await _meshPresencePublisher.PublishPresenceAsync(pose, metadata, cancellationToken).ConfigureAwait(false);
    }
}
