using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace Aog.Core.Mesh.RadioBridge;

/// <summary>
/// Manages framing, acknowledgement, and retry logic for the RadioBridge transport.
/// </summary>
public sealed class RadioBridgeTransport
{
    private const byte FrameVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly RadioBridgeOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<ushort, PendingOutbound> _pending = new();
    private readonly Queue<PendingOutbound> _sendQueue = new();
    private readonly Queue<RadioBridgeFrame> _ackQueue = new();
    private readonly Queue<ushort> _recentSequenceWindow = new();
    private readonly HashSet<ushort> _recentSequences = new();

    private RadioBridgeLinkMetrics _linkMetrics = RadioBridgeLinkMetrics.Unknown;
    private ushort _nextSequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioBridgeTransport"/> class.
    /// </summary>
    /// <param name="options">Transport configuration.</param>
    /// <param name="timeProvider">Time abstraction for deterministic testing.</param>
    public RadioBridgeTransport(RadioBridgeOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.DeviceId))
        {
            throw new ArgumentException("Device identifier is required for RadioBridge options.", nameof(options));
        }

        if (options.ReplayWindow <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Replay window must be positive.");
        }

        if (options.MaxRetransmissions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Max retransmissions must be positive.");
        }

        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _nextSequence = 1; // keep zero for control paths
    }

    /// <summary>
    /// Raised when an inbound publication is decoded successfully.
    /// </summary>
    public event Action<RadioBridgePublicationMessage>? PublicationReceived;

    /// <summary>
    /// Raised when the transport exhausts all retransmission attempts for an outbound frame.
    /// </summary>
    public event Action<RadioBridgeOutboundFailure>? OutboundDeliveryFailed;

    /// <summary>
    /// Gets the number of outstanding frames waiting for acknowledgement.
    /// </summary>
    public int PendingOutboundCount => _pending.Count;

    /// <summary>
    /// Gets the most recent link metrics supplied by the adapter.
    /// </summary>
    public RadioBridgeLinkMetrics LinkMetrics => _linkMetrics;

    /// <summary>
    /// Enqueues a mesh publication for transport.
    /// </summary>
    /// <param name="message">Publication to send over the radio bridge.</param>
    public void EnqueuePublication(RadioBridgePublicationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.PublisherDeviceId);

        if (_pending.Count >= _options.ReplayWindow)
        {
            throw new InvalidOperationException("Replay window is full; wait for acknowledgements before sending more data.");
        }

        var sequence = _nextSequence;
        _nextSequence = (ushort)((_nextSequence + 1) & 0xFFFF);
        if (_nextSequence == 0)
        {
            _nextSequence = 1;
        }

        var frame = BuildFrame(sequence, message);
        var pending = new PendingOutbound(sequence, frame, message, _timeProvider.GetUtcNow());
        _pending.Add(sequence, pending);
        _sendQueue.Enqueue(pending);
    }

    /// <summary>
    /// Attempts to retrieve the next frame that should be transmitted.
    /// </summary>
    /// <param name="frame">Binary frame ready to be written to the radio link.</param>
    /// <returns><c>true</c> when a frame is available; otherwise <c>false</c>.</returns>
    public bool TryGetNextFrame(out ReadOnlyMemory<byte> frame)
    {
        var now = _timeProvider.GetUtcNow();

        if (_ackQueue.Count > 0)
        {
            var ackFrame = _ackQueue.Dequeue();
            frame = RadioBridgeFrameCodec.Encode(ackFrame);
            return true;
        }

        while (_sendQueue.Count > 0)
        {
            var candidate = _sendQueue.Dequeue();
            if (!_pending.ContainsKey(candidate.Sequence))
            {
                continue;
            }

            if (!TryPrepareSend(candidate, now, initialSend: true, out frame))
            {
                continue;
            }

            return true;
        }

        PendingOutbound? dueForRetry = null;
        var earliest = DateTimeOffset.MaxValue;
        foreach (var pending in _pending.Values)
        {
            if (pending.NextSendAt <= now && pending.NextSendAt < earliest)
            {
                earliest = pending.NextSendAt;
                dueForRetry = pending;
            }
        }

        if (dueForRetry is not null)
        {
            if (TryPrepareSend(dueForRetry, now, initialSend: false, out frame))
            {
                return true;
            }
        }

        frame = ReadOnlyMemory<byte>.Empty;
        return false;
    }

    /// <summary>
    /// Processes an inbound frame and returns the outcome.
    /// </summary>
    /// <param name="buffer">Binary frame payload.</param>
    /// <returns>Outcome of the frame processing.</returns>
    public RadioBridgeProcessResult ProcessInboundFrame(ReadOnlySpan<byte> buffer)
    {
        RadioBridgeFrame frame;
        try
        {
            frame = RadioBridgeFrameCodec.Decode(buffer);
        }
        catch (InvalidOperationException)
        {
            return RadioBridgeProcessResult.CrcMismatch();
        }
        catch (ArgumentException)
        {
            return RadioBridgeProcessResult.Malformed();
        }

        if (frame.Version != FrameVersion)
        {
            return RadioBridgeProcessResult.UnsupportedVersion();
        }

        return frame.PayloadType switch
        {
            RadioBridgePayloadType.Ack => HandleAckFrame(frame),
            RadioBridgePayloadType.Data => HandleDataFrame(frame),
            _ => RadioBridgeProcessResult.UnsupportedPayload(),
        };
    }

    /// <summary>
    /// Updates the link quality metrics influencing resend back-off behaviour.
    /// </summary>
    /// <param name="metrics">Metrics reported by the physical link.</param>
    public void UpdateLinkMetrics(RadioBridgeLinkMetrics metrics)
    {
        _linkMetrics = metrics;
    }

    private bool TryPrepareSend(PendingOutbound pending, DateTimeOffset now, bool initialSend, out ReadOnlyMemory<byte> frame)
    {
        if (pending.Attempts >= _options.MaxRetransmissions)
        {
            _pending.Remove(pending.Sequence);
            OutboundDeliveryFailed?.Invoke(new RadioBridgeOutboundFailure(pending.Message, pending.Attempts));
            frame = default;
            return false;
        }

        pending.Attempts++;
        pending.LastSentAt = now;
        pending.NextSendAt = now + CalculateRetryInterval(pending.Attempts);

        var flags = pending.Frame.Flags;
        if (!initialSend)
        {
            flags |= RadioBridgeFrameFlags.Retransmission;
        }
        else
        {
            flags &= ~RadioBridgeFrameFlags.Retransmission;
        }

        var frameToSend = pending.Frame with { Flags = flags };
        frame = RadioBridgeFrameCodec.Encode(frameToSend);
        return true;
    }

    private RadioBridgeFrame BuildFrame(ushort sequence, RadioBridgePublicationMessage message)
    {
        var topicHash = RadioBridgeTopicHasher.ComputeHash(message.Topic);
        var envelope = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        var payload = Compress(envelope);
        var flags = RadioBridgeFrameFlags.Compressed;
        if (_options.EnableForwardErrorCorrection)
        {
            payload = Hamming12_8.Encode(payload);
            flags |= RadioBridgeFrameFlags.ForwardErrorCorrection;
        }

        return new RadioBridgeFrame(
            Version: FrameVersion,
            PayloadType: RadioBridgePayloadType.Data,
            Flags: flags,
            Sequence: sequence,
            Ack: 0,
            TopicHash: topicHash,
            Payload: payload);
    }

    private RadioBridgeProcessResult HandleAckFrame(RadioBridgeFrame frame)
    {
        if (_pending.Remove(frame.Ack, out var pending))
        {
            return RadioBridgeProcessResult.Acknowledged(frame.Ack);
        }

        return RadioBridgeProcessResult.StaleAcknowledgement(frame.Ack);
    }

    private RadioBridgeProcessResult HandleDataFrame(RadioBridgeFrame frame)
    {
        QueueAck(frame.Sequence);

        if (_recentSequences.Contains(frame.Sequence))
        {
            return RadioBridgeProcessResult.Duplicate(frame.Sequence);
        }

        if (_recentSequences.Count >= _options.ReplayWindow)
        {
            var removed = _recentSequenceWindow.Dequeue();
            _recentSequences.Remove(removed);
        }

        _recentSequences.Add(frame.Sequence);
        _recentSequenceWindow.Enqueue(frame.Sequence);

        try
        {
            var message = DecodePublication(
                frame.Payload.Span,
                frame.Flags.HasFlag(RadioBridgeFrameFlags.Compressed),
                frame.Flags.HasFlag(RadioBridgeFrameFlags.ForwardErrorCorrection));
            PublicationReceived?.Invoke(message);
            return RadioBridgeProcessResult.Delivered(frame.Sequence, message);
        }
        catch (Exception)
        {
            return RadioBridgeProcessResult.PayloadError(frame.Sequence);
        }
    }

    private void QueueAck(ushort sequence)
    {
        var ackFrame = new RadioBridgeFrame(
            Version: FrameVersion,
            PayloadType: RadioBridgePayloadType.Ack,
            Flags: RadioBridgeFrameFlags.None,
            Sequence: sequence,
            Ack: sequence,
            TopicHash: 0,
            Payload: ReadOnlyMemory<byte>.Empty);
        _ackQueue.Enqueue(ackFrame);
    }

    private RadioBridgePublicationMessage DecodePublication(
        ReadOnlySpan<byte> payload,
        bool compressed,
        bool forwardErrorCorrection)
    {
        var decodedPayload = forwardErrorCorrection ? Hamming12_8.Decode(payload) : payload.ToArray();
        var envelope = compressed ? Decompress(decodedPayload) : decodedPayload;
        var message = JsonSerializer.Deserialize<RadioBridgePublicationMessage>(envelope, SerializerOptions);
        if (message is null)
        {
            throw new InvalidOperationException("Unable to deserialize radio bridge publication envelope.");
        }

        return message;
    }

    private static byte[] Compress(ReadOnlySpan<byte> payload)
    {
        if (payload.Length == 0)
        {
            return Array.Empty<byte>();
        }

        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            brotli.Write(payload);
        }

        return output.ToArray();
    }

    private static byte[] Decompress(ReadOnlySpan<byte> payload)
    {
        if (payload.Length == 0)
        {
            return Array.Empty<byte>();
        }

        using var input = new MemoryStream(payload.ToArray());
        using var brotli = new BrotliStream(input, CompressionMode.Decompress, leaveOpen: true);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }

    private TimeSpan CalculateRetryInterval(int attempt)
    {
        var baseMilliseconds = Math.Max(_options.BaseRetryInterval.TotalMilliseconds, 10);
        var loss = Math.Clamp(_linkMetrics.PacketLoss, 0, 0.95);
        var lossFactor = 1.0 + (loss * 4.0);

        double rssiFactor = 1.0;
        if (_linkMetrics.Rssi >= -70)
        {
            rssiFactor = 0.75;
        }
        else if (_linkMetrics.Rssi <= -95)
        {
            rssiFactor = 1.5;
        }
        else if (_linkMetrics.Rssi <= -85)
        {
            rssiFactor = 1.2;
        }

        var attemptFactor = 1.0 + Math.Max(0, attempt - 1) * 0.6;
        var interval = baseMilliseconds * lossFactor * rssiFactor * attemptFactor;
        var capped = Math.Min(interval, _options.MaxRetryInterval.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(capped);
    }

    private sealed class PendingOutbound
    {
        public PendingOutbound(ushort sequence, RadioBridgeFrame frame, RadioBridgePublicationMessage message, DateTimeOffset now)
        {
            Sequence = sequence;
            Frame = frame;
            Message = message;
            LastSentAt = now;
            NextSendAt = now;
        }

        public ushort Sequence { get; }

        public RadioBridgeFrame Frame { get; }

        public RadioBridgePublicationMessage Message { get; }

        public int Attempts { get; set; }

        public DateTimeOffset LastSentAt { get; set; }

        public DateTimeOffset NextSendAt { get; set; }
    }
}

/// <summary>
/// Represents the outcome of processing an inbound radio frame.
/// </summary>
public readonly record struct RadioBridgeProcessResult(
    RadioBridgeProcessStatus Status,
    ushort? Sequence,
    RadioBridgePublicationMessage? Publication)
{
    /// <summary>
    /// Result when a frame fails CRC validation.
    /// </summary>
    public static RadioBridgeProcessResult CrcMismatch() => new(RadioBridgeProcessStatus.CrcMismatch, null, null);

    /// <summary>
    /// Result when a frame fails structural validation.
    /// </summary>
    public static RadioBridgeProcessResult Malformed() => new(RadioBridgeProcessStatus.MalformedFrame, null, null);

    /// <summary>
    /// Result when an unsupported version is received.
    /// </summary>
    public static RadioBridgeProcessResult UnsupportedVersion() => new(RadioBridgeProcessStatus.UnsupportedVersion, null, null);

    /// <summary>
    /// Result when the payload type is unknown.
    /// </summary>
    public static RadioBridgeProcessResult UnsupportedPayload() => new(RadioBridgeProcessStatus.UnsupportedPayloadType, null, null);

    /// <summary>
    /// Result when a duplicate frame is observed.
    /// </summary>
    public static RadioBridgeProcessResult Duplicate(ushort sequence) => new(RadioBridgeProcessStatus.DuplicateFrame, sequence, null);

    /// <summary>
    /// Result when a payload is delivered successfully.
    /// </summary>
    public static RadioBridgeProcessResult Delivered(ushort sequence, RadioBridgePublicationMessage publication) => new(RadioBridgeProcessStatus.PublicationDelivered, sequence, publication);

    /// <summary>
    /// Result when a payload fails to decode.
    /// </summary>
    public static RadioBridgeProcessResult PayloadError(ushort sequence) => new(RadioBridgeProcessStatus.PayloadDecodeFailure, sequence, null);

    /// <summary>
    /// Result when an acknowledgement matches a pending frame.
    /// </summary>
    public static RadioBridgeProcessResult Acknowledged(ushort sequence) => new(RadioBridgeProcessStatus.AcknowledgementProcessed, sequence, null);

    /// <summary>
    /// Result when an acknowledgement does not match a pending frame.
    /// </summary>
    public static RadioBridgeProcessResult StaleAcknowledgement(ushort sequence) => new(RadioBridgeProcessStatus.StaleAcknowledgement, sequence, null);
}

/// <summary>
/// Statuses emitted by <see cref="RadioBridgeTransport.ProcessInboundFrame"/>.
/// </summary>
public enum RadioBridgeProcessStatus
{
    /// <summary>
    /// Publication payload delivered to listeners.
    /// </summary>
    PublicationDelivered,

    /// <summary>
    /// Duplicate frame ignored (ack queued).
    /// </summary>
    DuplicateFrame,

    /// <summary>
    /// Payload failed to decode.
    /// </summary>
    PayloadDecodeFailure,

    /// <summary>
    /// CRC validation failed.
    /// </summary>
    CrcMismatch,

    /// <summary>
    /// Frame structure invalid.
    /// </summary>
    MalformedFrame,

    /// <summary>
    /// Unsupported frame version.
    /// </summary>
    UnsupportedVersion,

    /// <summary>
    /// Unsupported payload type received.
    /// </summary>
    UnsupportedPayloadType,

    /// <summary>
    /// Acknowledgement mapped to pending frame.
    /// </summary>
    AcknowledgementProcessed,

    /// <summary>
    /// Acknowledgement did not map to pending frame.
    /// </summary>
    StaleAcknowledgement,
}

/// <summary>
/// Describes an outbound delivery failure.
/// </summary>
public sealed record RadioBridgeOutboundFailure(RadioBridgePublicationMessage Message, int Attempts);
