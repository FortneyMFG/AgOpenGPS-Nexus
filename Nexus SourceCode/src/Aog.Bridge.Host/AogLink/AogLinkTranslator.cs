using System.Collections.Concurrent;
using Aog.Core.V1;
using Aog.Link.V1;
using Aog.Protos.Capabilities.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Aog.Bridge.Host.AogLink;

/// <summary>
/// Translates between the gRPC contracts published through <see cref="Aog.Core.V1"/> and
/// the nanopb-oriented <see cref="LinkEnvelope"/> representation that rides on AOG-Link transports.
/// </summary>
public sealed class AogLinkTranslator
{
    private readonly ILogger<AogLinkTranslator> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly AogLinkNodeIdentity _identity;
    private readonly ConcurrentDictionary<uint, PendingCommand> _pendingCommands = new();
    private readonly long _startupTimestamp;
    private uint _sequence;

    public AogLinkTranslator(AogLinkNodeIdentity identity, TimeProvider timeProvider, ILogger<AogLinkTranslator> logger)
    {
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _startupTimestamp = _timeProvider.GetTimestamp();
    }

    /// <summary>
    /// Creates a discovery response advertising the bridge capabilities.
    /// </summary>
    public LinkEnvelope CreateDiscoveryResponse(DiscoveryAnnounce announce, IEnumerable<CapabilityDescriptor> capabilities)
    {
        if (announce is null)
            throw new ArgumentNullException(nameof(announce));
        if (capabilities is null)
            throw new ArgumentNullException(nameof(capabilities));

        var response = new DiscoveryResponse
        {
            Identity = _identity.ToProto(),
        };

        response.Capabilities.AddRange(capabilities.Select(capability => capability.Clone()));

        return CreateEnvelope(
            LinkClass.System,
            MessageType.DiscoveryResponse,
            payload: response,
            destination: announce.Identity?.NodeId ?? 0,
            needsAck: false,
            priority: NodePriority.Default);
    }

    /// <summary>
    /// Creates a heartbeat frame for the bridge.
    /// </summary>
    public LinkEnvelope CreateHeartbeat(uint destination = 0)
    {
        var uptime = _timeProvider.GetElapsedTime(_startupTimestamp, _timeProvider.GetTimestamp());

        var heartbeat = new Heartbeat
        {
            Identity = _identity.ToProto(),
            UptimeMs = (ulong)uptime.TotalMilliseconds,
            StaleSequenceThreshold = 3,
        };

        return CreateEnvelope(
            LinkClass.System,
            MessageType.Heartbeat,
            heartbeat,
            destination,
            needsAck: false,
            priority: _identity.Priority);
    }

    /// <summary>
    /// Creates a time synchronisation frame advertising the bridge clock.
    /// </summary>
    public LinkEnvelope CreateTimeSync(uint destination = 0)
    {
        var now = _timeProvider.GetUtcNow();
        var monotonic = _timeProvider.GetTimestamp();

        var elapsed = _timeProvider.GetElapsedTime(_startupTimestamp, monotonic);

        var timeSync = new TimeSync
        {
            CurrentTime = Timestamp.FromDateTimeOffset(now),
            MonotonicTimeMs = (ulong)elapsed.TotalMilliseconds,
        };

        return CreateEnvelope(
            LinkClass.System,
            MessageType.Timesync,
            timeSync,
            destination,
            needsAck: false,
            priority: _identity.Priority);
    }

    /// <summary>
    /// Creates a telemetry frame wrapping a <see cref="Pose"/> payload.
    /// </summary>
    public LinkEnvelope CreatePoseTelemetry(Pose pose, uint destination = 0) =>
        CreateTelemetryEnvelope(MessageType.TelemetryPose, pose, destination);

    /// <summary>
    /// Creates a telemetry frame wrapping an <see cref="Imu"/> payload.
    /// </summary>
    public LinkEnvelope CreateImuTelemetry(Imu imu, uint destination = 0) =>
        CreateTelemetryEnvelope(MessageType.TelemetryImu, imu, destination);

    /// <summary>
    /// Creates a telemetry frame carrying a <see cref="SectionMask"/> payload.
    /// </summary>
    public LinkEnvelope CreateSectionTelemetry(SectionMask mask, uint destination = 0) =>
        CreateTelemetryEnvelope(MessageType.TelemetrySectionMask, mask, destination);

    /// <summary>
    /// Creates a telemetry frame carrying a <see cref="SteerState"/> payload.
    /// </summary>
    public LinkEnvelope CreateSteerStateTelemetry(SteerState state, uint destination = 0) =>
        CreateTelemetryEnvelope(MessageType.TelemetrySteerState, state, destination);

    /// <summary>
    /// Creates a telemetry frame carrying a <see cref="TimingCaps"/> payload.
    /// </summary>
    public LinkEnvelope CreateTimingTelemetry(TimingCaps timing, uint destination = 0) =>
        CreateTelemetryEnvelope(MessageType.TelemetryTiming, timing, destination);

    /// <summary>
    /// Creates a steering command frame and tracks it for acknowledgement handling.
    /// </summary>
    public LinkEnvelope CreateSteerCommand(SteerCmd command, uint destination)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        return CreateCommandEnvelope(
            MessageType.CommandSteer,
            command,
            destination,
            priority: NodePriority.High);
    }

    /// <summary>
    /// Creates a section mask command frame.
    /// </summary>
    public LinkEnvelope CreateSectionCommand(SectionMask mask, uint destination)
    {
        if (mask is null)
            throw new ArgumentNullException(nameof(mask));

        return CreateCommandEnvelope(
            MessageType.CommandSectionMask,
            mask,
            destination,
            priority: NodePriority.Default);
    }

    /// <summary>
    /// Attempts to translate an incoming envelope into its gRPC counterpart.
    /// </summary>
    /// <param name="envelope">Incoming envelope.</param>
    /// <param name="message">Translated gRPC message, or <c>null</c> if unrecognised.</param>
    /// <param name="kind">Classified message kind.</param>
    /// <returns><c>true</c> when the message type is known.</returns>
    public bool TryTranslateIncoming(LinkEnvelope envelope, out object? message, out AogLinkMessageKind kind)
    {
        if (envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        kind = AogLinkMessageKind.Unknown;
        message = null;

        switch (envelope.Header?.MessageType)
        {
            case MessageType.TelemetryPose:
                message = envelope.Pose?.Clone();
                kind = AogLinkMessageKind.Pose;
                break;
            case MessageType.TelemetryImu:
                message = envelope.Imu?.Clone();
                kind = AogLinkMessageKind.Imu;
                break;
            case MessageType.TelemetrySectionMask:
                message = envelope.SectionMask?.Clone();
                kind = AogLinkMessageKind.SectionMask;
                break;
            case MessageType.TelemetrySteerState:
                message = envelope.SteerState?.Clone();
                kind = AogLinkMessageKind.SteerState;
                break;
            case MessageType.TelemetryTiming:
                message = envelope.Timing?.Clone();
                kind = AogLinkMessageKind.Timing;
                break;
            case MessageType.CommandAck:
                message = envelope.CommandAck?.Clone();
                kind = AogLinkMessageKind.CommandAck;
                break;
            case MessageType.DiscoveryAnnounce:
                message = envelope.DiscoveryAnnounce?.Clone();
                kind = AogLinkMessageKind.DiscoveryAnnounce;
                break;
            case MessageType.DiscoveryResponse:
                message = envelope.DiscoveryResponse?.Clone();
                kind = AogLinkMessageKind.DiscoveryResponse;
                break;
            case MessageType.Heartbeat:
                message = envelope.Heartbeat?.Clone();
                kind = AogLinkMessageKind.Heartbeat;
                break;
            case MessageType.Timesync:
                message = envelope.TimeSync?.Clone();
                kind = AogLinkMessageKind.TimeSync;
                break;
        }

        if (kind != AogLinkMessageKind.Unknown)
        {
            StampHeader(message as IMessage);
        }

        return kind != AogLinkMessageKind.Unknown;
    }

    /// <summary>
    /// Records a command frame for acknowledgement handling.
    /// </summary>
    public void TrackCommand(LinkEnvelope command)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));
        if (command.Header is null)
            throw new ArgumentException("Command envelope must include a header.", nameof(command));

        var sequence = command.Header.Sequence;
        var pending = new PendingCommand(
            command.Clone(),
            _timeProvider.GetUtcNow(),
            Attempt: 1);
        _pendingCommands[sequence] = pending;
    }

    /// <summary>
    /// Attempts to match an acknowledgement with a pending command.
    /// </summary>
    public bool TryHandleAck(CommandAck ack, out LinkEnvelope? acknowledged)
    {
        if (ack is null)
            throw new ArgumentNullException(nameof(ack));

        acknowledged = null;

        if (!_pendingCommands.TryRemove(ack.AcknowledgedSequence, out var pending))
        {
            return false;
        }

        acknowledged = pending.Command;
        return true;
    }

    /// <summary>
    /// Collects commands that require retransmission after their acknowledgement deadline elapsed.
    /// </summary>
    /// <param name="ackTimeout">Maximum duration to wait for acknowledgements.</param>
    /// <param name="maxRetries">Maximum retry count per command.</param>
    /// <param name="resends">Commands that should be retried.</param>
    /// <returns><c>true</c> when any commands are scheduled for retransmission.</returns>
    public bool TryCollectResends(TimeSpan ackTimeout, int maxRetries, out IReadOnlyList<LinkEnvelope> resends)
    {
        if (ackTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ackTimeout));
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries));

        var now = _timeProvider.GetUtcNow();
        var results = new List<LinkEnvelope>();

        foreach (var (sequence, pending) in _pendingCommands)
        {
            if (now - pending.LastAttempt < ackTimeout)
            {
                continue;
            }

            if (pending.Attempt >= maxRetries + 1)
            {
                _pendingCommands.TryRemove(sequence, out _);
                _logger.LogWarning("Command {Sequence} exceeded retry budget and will be dropped.", sequence);
                continue;
            }

            var updated = pending with
            {
                Attempt = pending.Attempt + 1,
                LastAttempt = now,
            };

            _pendingCommands[sequence] = updated;
            results.Add(updated.Command.Clone());
        }

        resends = results;
        return results.Count > 0;
    }

    private LinkEnvelope CreateTelemetryEnvelope(MessageType messageType, IMessage payload, uint destination)
    {
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        return CreateEnvelope(
            LinkClass.Telemetry,
            messageType,
            payload,
            destination,
            needsAck: false,
            priority: _identity.Priority);
    }

    private LinkEnvelope CreateCommandEnvelope(MessageType messageType, IMessage payload, uint destination, NodePriority priority)
    {
        var envelope = CreateEnvelope(
            LinkClass.Command,
            messageType,
            payload,
            destination,
            needsAck: true,
            priority: priority);

        TrackCommand(envelope);
        return envelope;
    }

    private LinkEnvelope CreateEnvelope(LinkClass @class, MessageType type, IMessage payload, uint destination, bool needsAck, NodePriority priority)
    {
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        var header = new FrameHeader
        {
            Version = 1,
            MessageClass = @class,
            MessageType = type,
            Sequence = GetNextSequence(),
            Source = _identity.Address,
            Destination = destination,
            PayloadLength = (uint)payload.CalculateSize(),
            Flags = new FrameFlags
            {
                NeedsAck = needsAck,
                Priority = priority,
            },
        };

        var envelope = new LinkEnvelope
        {
            Header = header,
        };

        switch (payload)
        {
            case Pose pose:
                envelope.Pose = pose.Clone();
                break;
            case Imu imu:
                envelope.Imu = imu.Clone();
                break;
            case SectionMask mask:
                envelope.SectionMask = mask.Clone();
                break;
            case SteerState steerState:
                envelope.SteerState = steerState.Clone();
                break;
            case SteerCmd steerCmd:
                envelope.SteerCommand = steerCmd.Clone();
                break;
            case TimingCaps timing:
                envelope.Timing = timing.Clone();
                break;
            case DiscoveryResponse discoveryResponse:
                envelope.DiscoveryResponse = discoveryResponse.Clone();
                break;
            case Heartbeat heartbeat:
                envelope.Heartbeat = heartbeat.Clone();
                break;
            case TimeSync timeSync:
                envelope.TimeSync = timeSync.Clone();
                break;
            case DiscoveryAnnounce announce:
                envelope.DiscoveryAnnounce = announce.Clone();
                break;
            case CommandAck ack:
                envelope.CommandAck = ack.Clone();
                break;
            default:
                throw new ArgumentException($"Unsupported payload type {payload.GetType().Name}.", nameof(payload));
        }

        return envelope;
    }

    private uint GetNextSequence() => Interlocked.Increment(ref _sequence);

    private void StampHeader(IMessage? message)
    {
        if (message is null)
        {
            return;
        }

        switch (message)
        {
            case Pose pose:
                pose.Header ??= new Header();
                pose.Header.Sequence = pose.Header.Sequence == 0 ? GetNextSequence() : pose.Header.Sequence;
                pose.Header.Timestamp ??= Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow());
                pose.Header.Source ??= "aog-link";
                break;
            case Imu imu:
                imu.Header ??= new Header();
                imu.Header.Sequence = imu.Header.Sequence == 0 ? GetNextSequence() : imu.Header.Sequence;
                imu.Header.Timestamp ??= Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow());
                imu.Header.Source ??= "aog-link";
                break;
            case SectionMask mask:
                mask.Header ??= new Header();
                mask.Header.Sequence = mask.Header.Sequence == 0 ? GetNextSequence() : mask.Header.Sequence;
                mask.Header.Timestamp ??= Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow());
                mask.Header.Source ??= "aog-link";
                break;
            case SteerState steerState:
                steerState.Header ??= new Header();
                steerState.Header.Sequence = steerState.Header.Sequence == 0 ? GetNextSequence() : steerState.Header.Sequence;
                steerState.Header.Timestamp ??= Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow());
                steerState.Header.Source ??= "aog-link";
                break;
            case TimingCaps timing:
                timing.Header ??= new Header();
                timing.Header.Sequence = timing.Header.Sequence == 0 ? GetNextSequence() : timing.Header.Sequence;
                timing.Header.Timestamp ??= Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow());
                timing.Header.Source ??= "aog-link";
                break;
        }
    }

    private sealed record PendingCommand(LinkEnvelope Command, DateTimeOffset LastAttempt, int Attempt);
}

/// <summary>
/// Enumerates the supported message kinds emitted by the translator.
/// </summary>
public enum AogLinkMessageKind
{
    Unknown = 0,
    Pose,
    Imu,
    SectionMask,
    SteerState,
    Timing,
    CommandAck,
    DiscoveryAnnounce,
    DiscoveryResponse,
    Heartbeat,
    TimeSync,
}
