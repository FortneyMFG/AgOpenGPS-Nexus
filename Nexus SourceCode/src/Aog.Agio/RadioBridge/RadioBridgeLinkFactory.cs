using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Aog.Core.Mesh.RadioBridge;

namespace Aog.Agio.RadioBridge;

/// <summary>
/// Default factory that creates ELRS radio bridge links.
/// </summary>
public sealed class RadioBridgeLinkFactory : IRadioBridgeLinkFactory
{
    /// <inheritdoc />
    public IRadioBridgeLink Create(RadioBridgeAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Endpoint.StartsWith("sim://", StringComparison.OrdinalIgnoreCase))
        {
            return new Simulation.SimulatedRadioBridgeLink(options.Endpoint);
        }

        if (options.Endpoint.StartsWith("lora://", StringComparison.OrdinalIgnoreCase))
        {
            return CreateSerialLink(options.Endpoint, scheme: "lora", defaultBaud: 57600);
        }

        if (options.Endpoint.StartsWith("serial://", StringComparison.OrdinalIgnoreCase))
        {
            return CreateSerialLink(options.Endpoint, scheme: "serial", defaultBaud: 420000);
        }

        throw new NotSupportedException($"Unsupported radio bridge endpoint '{options.Endpoint}'.");
    }

    private static IRadioBridgeLink CreateSerialLink(string endpoint, string scheme, int defaultBaud)
    {
        var uri = new Uri(endpoint);
        var portName = uri.Host;
        if (string.IsNullOrWhiteSpace(portName))
        {
            throw new ArgumentException("Serial endpoint must specify a port name.", nameof(endpoint));
        }

        var baudRate = defaultBaud;
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var pairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2 &&
                    parts[0].Equals("baud", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(Uri.UnescapeDataString(parts[1]), out var parsedBaud))
                {
                    baudRate = Math.Max(parsedBaud, 1);
                }
            }
        }

        return new SerialRadioBridgeLink($"{scheme}://{portName}", portName, baudRate);
    }

    private sealed class SerialRadioBridgeLink : IRadioBridgeLink
    {
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly Channel<ReadOnlyMemory<byte>> _channel;
        private SerialPort? _serialPort;
        private CancellationTokenSource? _readerCancellation;
        private Task? _readerTask;
        private RadioBridgeLinkMetrics _metrics = RadioBridgeLinkMetrics.Unknown;

        public SerialRadioBridgeLink(string name, string portName, int baudRate)
        {
            Name = name;
            _portName = portName;
            _baudRate = baudRate;
            _channel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false,
            });
        }

        public string Name { get; }

        public RadioBridgeLinkMetrics CurrentMetrics => _metrics;

        public event Action<RadioBridgeLinkMetrics>? LinkMetricsChanged;

        public async ValueTask ConnectAsync(CancellationToken cancellationToken)
        {
            if (_serialPort is not null)
            {
                return;
            }

            _serialPort = new SerialPort(_portName, _baudRate)
            {
                ReadTimeout = -1,
                WriteTimeout = -1,
                DtrEnable = true,
                RtsEnable = true,
            };

            _serialPort.Open();
            _serialPort.DiscardInBuffer();

            _readerCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _readerTask = Task.Run(() => ReadLoopAsync(_readerCancellation.Token), CancellationToken.None);
            await Task.Yield();
        }

        public async ValueTask SendFrameAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken)
        {
            if (_serialPort is null)
            {
                throw new InvalidOperationException("Serial link is not connected.");
            }

            await _serialPort.BaseStream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
            await _serialPort.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadFramesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var frame))
                {
                    yield return frame;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_readerCancellation is not null)
            {
                _readerCancellation.Cancel();
            }

            if (_readerTask is not null)
            {
                try
                {
                    await _readerTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            _serialPort?.Dispose();
            _serialPort = null;
            _readerCancellation?.Dispose();
            _readerCancellation = null;
        }

        private async Task ReadLoopAsync(CancellationToken cancellationToken)
        {
            if (_serialPort is null)
            {
                return;
            }

            var stream = _serialPort.BaseStream;
            var header = new byte[14];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await ReadExactAsync(stream, header, cancellationToken).ConfigureAwait(false);
                    var payloadLength = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(12, 2));
                    var frame = new byte[14 + payloadLength + 2];
                    header.CopyTo(frame, 0);
                    if (payloadLength > 0 || true)
                    {
                        await ReadExactAsync(stream, frame.AsMemory(14), payloadLength + 2, cancellationToken).ConfigureAwait(false);
                    }

                    await _channel.Writer.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                _channel.Writer.TryComplete();
                throw;
            }
            finally
            {
                _channel.Writer.TryComplete();
            }
        }

        private static async Task ReadExactAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var remaining = buffer.Length;
            var offset = 0;
            while (remaining > 0)
            {
                var read = await stream.ReadAsync(buffer.Slice(offset, remaining), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading radio frame.");
                }

                remaining -= read;
                offset += read;
            }
        }
    }
}
