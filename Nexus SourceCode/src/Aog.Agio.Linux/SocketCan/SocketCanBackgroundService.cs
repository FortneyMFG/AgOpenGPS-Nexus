using System;
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

namespace Aog.Agio.Linux.SocketCan;

/// <summary>
/// Background worker that listens for SocketCAN frames and republishes them via gRPC.
/// </summary>
public sealed class SocketCanBackgroundService : BackgroundService
{
    private readonly ISocketCanClientFactory _clientFactory;
    private readonly ISocketCanFramePublisher _publisher;
    private readonly IOptionsMonitor<SocketCanOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SocketCanBackgroundService> _logger;
    private long _sequence;

    public SocketCanBackgroundService(
        ISocketCanClientFactory clientFactory,
        ISocketCanFramePublisher publisher,
        IOptionsMonitor<SocketCanOptions> options,
        TimeProvider timeProvider,
        ILogger<SocketCanBackgroundService> logger)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = SnapshotOptions(_options.CurrentValue);
            if (string.IsNullOrWhiteSpace(options.InterfaceName))
            {
                _logger.LogInformation(
                    "SocketCAN monitor disabled. Configure AgioHost:Linux:SocketCan:InterfaceName to enable.");

                var enabled = await WaitForInterfaceAsync(stoppingToken).ConfigureAwait(false);
                if (!enabled)
                {
                    break;
                }

                continue;
            }

            ISocketCanClient? client = null;
            var reconfigured = false;
            using var reconfigureCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            using var changeRegistration = _options.OnChange((updated, _) =>
            {
                if (!stoppingToken.IsCancellationRequested && RequiresRestart(options, updated))
                {
                    reconfigured = true;
                    reconfigureCts.Cancel();
                }
            });
            try
            {
                client = await _clientFactory.CreateAsync(options, reconfigureCts.Token).ConfigureAwait(false);
                _logger.LogInformation("SocketCAN interface {Interface} connected.", client.InterfaceName);
                await PumpAsync(client, options, reconfigureCts.Token).ConfigureAwait(false);
            }
            catch (SocketCanInterfaceNotFoundException ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "SocketCAN interface {Interface} not found. Retrying in {Delay}.",
                    ex.InterfaceName,
                    options.ReconnectDelay);
            }
            catch (SocketCanException ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "SocketCAN error on interface {Interface}.", options.InterfaceName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException) when (reconfigureCts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                reconfigured = true;
                _logger.LogInformation(
                    "SocketCAN options changed. Reconfiguring interface {Interface}.",
                    options.InterfaceName);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unexpected SocketCAN failure on {Interface}.", options.InterfaceName);
            }
            finally
            {
                client?.Dispose();
            }

            if (!reconfigured)
            {
                using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, reconfigureCts.Token);
                await DelayAsync(options.ReconnectDelay, delayCts.Token).ConfigureAwait(false);
            }
        }
    }

    private async Task PumpAsync(ISocketCanClient client, SocketCanOptions options, CancellationToken cancellationToken)
    {
        var sourceBase = string.IsNullOrEmpty(options.SourcePrefix)
            ? client.InterfaceName
            : string.Concat(options.SourcePrefix, "/", client.InterfaceName);

        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = client.ReadFrame(cancellationToken);
            if (result.Status == SocketCanFrameReadStatus.Timeout)
            {
                continue;
            }

            var frame = TranslateFrame(result.Frame, sourceBase);
            await _publisher.PublishAsync(frame, cancellationToken).ConfigureAwait(false);
        }
    }

    private static SocketCanOptions SnapshotOptions(SocketCanOptions? source)
    {
        if (source is null)
        {
            return new SocketCanOptions();
        }

        return new SocketCanOptions
        {
            InterfaceName = source.InterfaceName,
            IncludeVirtualInterfaces = source.IncludeVirtualInterfaces,
            ReceiveOwnMessages = source.ReceiveOwnMessages,
            ReceiveTimeout = source.ReceiveTimeout,
            ReconnectDelay = source.ReconnectDelay,
            SourcePrefix = source.SourcePrefix,
        };
    }

    private static bool RequiresRestart(SocketCanOptions? current, SocketCanOptions? updated)
    {
        if (current is null || updated is null)
        {
            return true;
        }

        return !string.Equals(current.InterfaceName, updated.InterfaceName, StringComparison.Ordinal)
            || current.IncludeVirtualInterfaces != updated.IncludeVirtualInterfaces
            || current.ReceiveOwnMessages != updated.ReceiveOwnMessages
            || current.ReceiveTimeout != updated.ReceiveTimeout
            || current.ReconnectDelay != updated.ReconnectDelay
            || !string.Equals(current.SourcePrefix, updated.SourcePrefix, StringComparison.Ordinal);
    }

    private async Task<bool> WaitForInterfaceAsync(CancellationToken stoppingToken)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = _options.OnChange((updated, _) =>
        {
            if (!string.IsNullOrWhiteSpace(updated.InterfaceName))
            {
                completion.TrySetResult(true);
            }
        });

        try
        {
            await completion.Task.WaitAsync(stoppingToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private CanFrame TranslateFrame(SocketCANSharp.CanFrame frame, string source)
    {
        var protobuf = new CanFrame
        {
            Header = new Header
            {
                Source = source,
                Timestamp = Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow()),
                Sequence = (ulong)Interlocked.Increment(ref _sequence),
            },
            ArbitrationId = GetArbitrationId(frame),
            Payload = ByteString.CopyFrom(frame.Data, 0, frame.Length),
            IsExtendedId = (frame.CanId & (uint)CanIdFlags.CAN_EFF_FLAG) != 0,
            IsRemoteRequest = (frame.CanId & (uint)CanIdFlags.CAN_RTR_FLAG) != 0,
        };

        return protobuf;
    }

    private static uint GetArbitrationId(SocketCANSharp.CanFrame frame)
    {
        var raw = SocketCanUtils.ExtractRawCanId(frame.CanId);
        if ((frame.CanId & (uint)CanIdFlags.CAN_EFF_FLAG) != 0)
        {
            return raw & SocketCanConstants.CAN_EFF_MASK;
        }

        return raw & SocketCanConstants.CAN_SFF_MASK;
    }

    private static async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero || delay == Timeout.InfiniteTimeSpan)
        {
            return;
        }

        try
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
