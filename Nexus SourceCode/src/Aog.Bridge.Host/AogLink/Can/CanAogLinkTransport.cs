using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Link.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace Aog.Bridge.Host.AogLink.Can;

/// <summary>
/// Implements the CAN/CAN-FD transport for AOG-Link. Packets are segmented into CAN-FD
/// frames respecting the identifier layout defined in ADR-006.
/// </summary>
public sealed class CanAogLinkTransport : Aog.Bridge.Host.AogLink.Transports.IAogLinkTransport
{
    private const int CanFdPayloadBytes = 54;

    private readonly ICanBus _bus;
    private readonly ILogger<CanAogLinkTransport> _logger;
    private readonly Channel<LinkEnvelope> _inbound;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<uint, FragmentCollector> _fragments = new();
    private readonly ConcurrentBag<Task> _backgroundTasks = new();

    public CanAogLinkTransport(ICanBus bus, ILogger<CanAogLinkTransport> logger)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inbound = Channel.CreateUnbounded<LinkEnvelope>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = false,
        });
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        _backgroundTasks.Add(Task.Run(() => ReadLoopAsync(_cts.Token), cancellationToken));
        return ValueTask.CompletedTask;
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();

        while (_backgroundTasks.TryTake(out var task))
        {
            try
            {
                await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    public async ValueTask SendAsync(LinkEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        var payload = envelope.ToByteArray();
        var identifier = BuildIdentifier(envelope.Header);
        var sequence = envelope.Header?.Sequence ?? 0;

        if (payload.Length <= CanFdPayloadBytes)
        {
            var frame = BuildSingleFrame(envelope.Header, payload, identifier);
            await _bus.SendAsync(frame, cancellationToken).ConfigureAwait(false);
            return;
        }

        var fragmentCount = (int)Math.Ceiling(payload.Length / (double)CanFdPayloadBytes);

        for (var index = 0; index < fragmentCount; index++)
        {
            var offset = index * CanFdPayloadBytes;
            var length = Math.Min(CanFdPayloadBytes, payload.Length - offset);
            var slice = payload.AsSpan(offset, length);

            var buffer = new byte[length + 6];
            buffer[0] = envelope.Header?.Version is { } version ? (byte)version : (byte)1;
            buffer[1] = (byte)(sequence >> 8);
            buffer[2] = (byte)(sequence & 0xFF);
            buffer[3] = (byte)fragmentCount;
            buffer[4] = (byte)index;
            buffer[5] = ConvertFlags(envelope.Header?.Flags, fragmented: true);
            slice.CopyTo(buffer.AsSpan(6));

            await _bus.SendAsync(new AogCanFrame(identifier, buffer), cancellationToken).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<LinkEnvelope> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _inbound.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_inbound.Reader.TryRead(out var envelope))
            {
                yield return envelope;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        await foreach (var frame in _bus.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await HandleFrameAsync(frame, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                _logger.LogWarning(ex, "Failed to decode CAN frame into LinkEnvelope fragment.");
            }
        }
    }

    private async Task HandleFrameAsync(AogCanFrame frame, CancellationToken cancellationToken)
    {
        if (frame.Data.Length < 5)
        {
            throw new FormatException("CAN frame payload too small for AOG-Link header.");
        }

        var sequence = (uint)((frame.Data[1] << 8) | frame.Data[2]);
        var flags = frame.Data.Length > 5 ? frame.Data[5] : (byte)0;
        var fragmented = (flags & 0x40) != 0; // bit 6 mirrors FrameFlags.Fragmented

        if (!fragmented && frame.Data.Length >= 6)
        {
            var payload = frame.Data[5..];
            var envelope = LinkEnvelope.Parser.ParseFrom(payload);
            envelope.Header = ApplyHeaderFromIdentifier(frame.Identifier, envelope.Header);
            await _inbound.Writer.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
            var decodedEnvelope = LinkEnvelope.Parser.ParseFrom(payload);
            decodedEnvelope.Header = ApplyHeaderFromIdentifier(frame.Identifier, decodedEnvelope.Header);
            await _inbound.Writer.WriteAsync(decodedEnvelope, cancellationToken).ConfigureAwait(false);
            return;
        }

        var fragmentCount = frame.Data[3];
        var fragmentIndex = frame.Data[4];
        var payloadSlice = frame.Data.Length > 6 ? frame.Data[6..] : Array.Empty<byte>();

        var collector = _fragments.GetOrAdd(sequence, _ => new FragmentCollector(fragmentCount));
        collector.Add(fragmentIndex, frame.Data[6..]);

        if (!collector.IsComplete)
        {
            return;
        }

        _fragments.TryRemove(sequence, out _);
        var combined = collector.Combine();
        var reassembledEnvelope = LinkEnvelope.Parser.ParseFrom(combined);
        reassembledEnvelope.Header = ApplyHeaderFromIdentifier(frame.Identifier, reassembledEnvelope.Header);
        await _inbound.Writer.WriteAsync(reassembledEnvelope, cancellationToken).ConfigureAwait(false);
        var decoded = LinkEnvelope.Parser.ParseFrom(combined);
        decoded.Header = ApplyHeaderFromIdentifier(frame.Identifier, decoded.Header);
        await _inbound.Writer.WriteAsync(decoded, cancellationToken).ConfigureAwait(false);
    }

    private static AogCanFrame BuildSingleFrame(FrameHeader? header, byte[] payload, uint identifier)
    {
        var buffer = new byte[payload.Length + 6];
        buffer[0] = header?.Version is { } version ? (byte)version : (byte)1;
        var sequence = header?.Sequence ?? 0;
        buffer[1] = (byte)(sequence >> 8);
        buffer[2] = (byte)(sequence & 0xFF);
        var length = (ushort)(header?.PayloadLength ?? (uint)payload.Length);
        buffer[3] = (byte)(length >> 8);
        buffer[4] = (byte)(length & 0xFF);
        buffer[5] = ConvertFlags(header?.Flags, fragmented: false);
        Array.Copy(payload, 0, buffer, 6, payload.Length);
        return new AogCanFrame(identifier, buffer);
    }

    private static byte ConvertFlags(FrameFlags? flags, bool fragmented)
    {
        byte value = 0;

        if (flags?.NeedsAck == true)
        {
            value |= 0x01;
        }

        if (flags?.IsReply == true)
        {
            value |= 0x02;
        }

        if (fragmented || flags?.Fragmented == true)
        {
            value |= 0x40;
        }

        var priority = (byte)(flags?.Priority ?? NodePriority.Default);
        value |= (byte)(priority << 3);
        return value;
    }

    private static uint BuildIdentifier(FrameHeader? header)
    {
        if (header is null)
        {
            return 0;
        }

        var priority = (uint)(header.Flags?.Priority ?? NodePriority.Default) & 0x07;
        var cls = (uint)header.MessageClass & 0x03;
        var type = (uint)header.MessageType & 0x3FF;
        var destination = (uint)(header.Destination & 0x7F);
        var source = (uint)(header.Source & 0x7F);

        return (priority << 26)
            | (cls << 24)
            | (type << 14)
            | (destination << 7)
            | source;
    }

    private static FrameHeader ApplyHeaderFromIdentifier(uint identifier, FrameHeader? header)
    {
        header ??= new FrameHeader();
        header.Flags ??= new FrameFlags();

        header.Flags.Priority = (NodePriority)((identifier >> 26) & 0x07);
        header.MessageClass = (LinkClass)((identifier >> 24) & 0x03);
        header.MessageType = (MessageType)((identifier >> 14) & 0x3FF);
        header.Destination = (identifier >> 7) & 0x7F;
        header.Source = identifier & 0x7F;
        return header;
    }

    private sealed class FragmentCollector
    {
        private readonly byte[][] _fragments;
        private int _received;

        public FragmentCollector(int fragmentCount)
        {
            if (fragmentCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fragmentCount));
            }

            _fragments = new byte[fragmentCount][];
        }

        public bool IsComplete => _received == _fragments.Length;

        public void Add(int index, ReadOnlySpan<byte> payload)
        {
            if (index < 0 || index >= _fragments.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (_fragments[index] is not null)
            {
                return;
            }

            _fragments[index] = payload.ToArray();
            Interlocked.Increment(ref _received);
        }

        public byte[] Combine()
        {
            var total = 0;
            foreach (var fragment in _fragments)
            {
                if (fragment is null)
                {
                    throw new InvalidOperationException("Fragments missing when attempting to combine.");
                }

                total += fragment.Length;
            }

            var result = new byte[total];
            var offset = 0;

            foreach (var fragment in _fragments)
            {
                fragment.CopyTo(result, offset);
                offset += fragment.Length;
            }

            return result;
        }
    }
}
