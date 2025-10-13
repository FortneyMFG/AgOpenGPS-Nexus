using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocketCANSharp;
using SocketCANSharp.Network;

namespace Aog.Agio.Linux;

/// <summary>
/// Background service that captures frames from a SocketCAN interface.
/// </summary>
internal sealed class SocketCanListenerService : BackgroundService
{
    private const uint CanEffFlag = 0x80000000;
    private const uint CanRtrFlag = 0x40000000;
    private const uint CanErrFlag = 0x20000000;

    private readonly ILogger<SocketCanListenerService> _logger;
    private readonly SocketCanOptions _options;
    private readonly SocketCanBusService _bus;
    private readonly TimeProvider _timeProvider;
    private long _sequence;

    public SocketCanListenerService(
        ILogger<SocketCanListenerService> logger,
        IOptions<SocketCanOptions> options,
        SocketCanBusService bus,
        TimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsLinux())
        {
            _logger.LogWarning("SocketCAN backend is only supported on Linux. Listener not started.");
            return;
        }

        var interfaces = CanNetworkInterface.GetAllInterfaces(includeVirtual: true);
        var target = interfaces.FirstOrDefault(iface => string.Equals(iface.Name, _options.InterfaceName, StringComparison.Ordinal));
        if (target is null)
        {
            throw new InvalidOperationException($"SocketCAN interface '{_options.InterfaceName}' was not found.");
        }

        using var socket = new RawCanSocket { Blocking = false };

        socket.Bind(target);

        _logger.LogInformation("Listening for SocketCAN traffic on {Interface}.", target.Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var bytesRead = socket.Read(out var frame);
                if (bytesRead > 0)
                {
                    PublishFrame(frame);
                }
                else
                {
                    await Task.Delay(_options.PollInterval, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (SocketCanException ex) when (IsTransient(ex))
            {
                await Task.Delay(_options.PollInterval, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private void PublishFrame(SocketCANSharp.CanFrame frame)
    {
        if ((frame.CanId & CanErrFlag) != 0)
        {
            return;
        }

        var isExtended = (frame.CanId & CanEffFlag) != 0;
        var arbitrationId = isExtended
            ? frame.CanId & SocketCanConstants.CAN_EFF_MASK
            : frame.CanId & SocketCanConstants.CAN_SFF_MASK;

        var timestamp = _timeProvider.GetUtcNow();
        var message = new CanFrame
        {
            Header = new Header
            {
                Sequence = unchecked((ulong)Interlocked.Increment(ref _sequence)),
                Timestamp = Timestamp.FromDateTime(timestamp.UtcDateTime),
                Source = _options.InterfaceName,
            },
            ArbitrationId = arbitrationId,
            Payload = ByteString.CopyFrom(frame.Data, 0, frame.Length),
            IsExtendedId = isExtended,
            IsRemoteRequest = (frame.CanId & CanRtrFlag) != 0,
        };

        _bus.Publish(message);
    }

    private static bool IsTransient(SocketCanException exception)
    {
        return exception.SocketErrorCode switch
        {
            SocketError.WouldBlock => true,
            SocketError.TimedOut => true,
            SocketError.TryAgain => true,
            _ => false,
        };
    }
}
