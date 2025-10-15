using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Link.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Bridge.Host.AogLink.Transports;

/// <summary>
/// Implements the Ethernet/UDP transport defined by ADR-006. Telemetry is received over
/// multicast while command/ack flows use a dedicated unicast socket with retry semantics
/// handled by higher layers.
/// </summary>
public sealed class UdpAogLinkTransport : IAogLinkTransport
{
    private readonly BridgeHostOptions.UdpLinkOptions _options;
    private readonly ILogger<UdpAogLinkTransport> _logger;
    private readonly Channel<LinkEnvelope> _inbound;
    private readonly ConcurrentBag<Task> _backgroundTasks = new();
    private readonly CancellationTokenSource _cts = new();
    private UdpClient? _multicastClient;
    private UdpClient? _commandClient;
    private IPEndPoint _commandDestination;

    public UdpAogLinkTransport(IOptions<BridgeHostOptions> options, ILogger<UdpAogLinkTransport> logger)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        _options = options.Value.Udp;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inbound = Channel.CreateUnbounded<LinkEnvelope>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = false,
        });

        _commandDestination = new IPEndPoint(IPAddress.Loopback, _options.CommandPort);
    }

    /// <summary>
    /// Overrides the default command destination endpoint. Useful when the bridge has
    /// discovered the peer address through discovery flows.
    /// </summary>
    public void SetCommandDestination(IPEndPoint endpoint)
    {
        _commandDestination = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        var multicastAddress = IPAddress.Parse(_options.MulticastAddress);

        _multicastClient = new UdpClient(AddressFamily.InterNetwork)
        {
            ExclusiveAddressUse = false,
        };

        _multicastClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _multicastClient.Client.Bind(new IPEndPoint(IPAddress.Any, _options.MulticastPort));
        _multicastClient.JoinMulticastGroup(multicastAddress);

        _commandClient = new UdpClient(AddressFamily.InterNetwork)
        {
            ExclusiveAddressUse = false,
        };
        _commandClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _commandClient.Client.Bind(new IPEndPoint(IPAddress.Any, _options.CommandPort));

        _backgroundTasks.Add(Task.Run(() => ReceiveLoopAsync(_multicastClient, _cts.Token), cancellationToken));
        _backgroundTasks.Add(Task.Run(() => ReceiveLoopAsync(_commandClient, _cts.Token), cancellationToken));

        await Task.CompletedTask;
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

        _multicastClient?.Dispose();
        _commandClient?.Dispose();
    }

    public async ValueTask SendAsync(LinkEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        var target = envelope.Header?.MessageClass switch
        {
            LinkClass.Command => _commandDestination,
            LinkClass.Ack => _commandDestination,
            _ => new IPEndPoint(IPAddress.Parse(_options.MulticastAddress), _options.MulticastPort),
        };

        var client = envelope.Header?.MessageClass switch
        {
            LinkClass.Command => _commandClient,
            LinkClass.Ack => _commandClient,
            _ => _multicastClient,
        } ?? throw new InvalidOperationException("Transport has not been started.");

        var bytes = envelope.ToByteArray();
        await client.SendAsync(bytes, bytes.Length, target).WaitAsync(cancellationToken).ConfigureAwait(false);
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

    private async Task ReceiveLoopAsync(UdpClient client, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UDP receive loop encountered an error.");
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                var envelope = LinkEnvelope.Parser.ParseFrom(result.Buffer);
                await _inbound.Writer.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidProtocolBufferException ex)
            {
                _logger.LogWarning(ex, "Failed to decode UDP datagram as LinkEnvelope.");
            }
        }
    }
}
