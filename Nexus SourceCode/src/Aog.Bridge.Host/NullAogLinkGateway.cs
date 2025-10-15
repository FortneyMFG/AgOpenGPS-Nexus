using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Bridge.Host;

public sealed class NullAogLinkGateway : IAogLinkGateway
{
    private readonly ILogger<NullAogLinkGateway> _logger;
    private readonly IOptions<BridgeHostOptions> _options;

    public NullAogLinkGateway(ILogger<NullAogLinkGateway> logger, IOptions<BridgeHostOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var bridgeOptions = _options.Value;

        _logger.LogInformation(
            "Starting placeholder AOG-Link gateway for node {NodeId} (firmware {FirmwareVersion}).",
            bridgeOptions.NodeId,
            bridgeOptions.FirmwareVersion);

        _logger.LogInformation(
            "gRPC binding {Address}:{Port} (allow insecure HTTP/2: {AllowInsecure}).",
            bridgeOptions.Grpc.BindAddress,
            bridgeOptions.Grpc.Port,
            bridgeOptions.Grpc.AllowInsecureHttp2);

        _logger.LogInformation(
            "UDP multicast {MulticastAddress}:{MulticastPort}, command port {CommandPort}, interface {InterfaceName}.",
            bridgeOptions.Udp.MulticastAddress,
            bridgeOptions.Udp.MulticastPort,
            bridgeOptions.Udp.CommandPort,
            string.IsNullOrWhiteSpace(bridgeOptions.Udp.NetworkInterfaceName)
                ? "auto"
                : bridgeOptions.Udp.NetworkInterfaceName);

        _logger.LogInformation(
            "Command retries {RetryCount}, ACK timeout {AckTimeout} ms, heartbeat {HeartbeatInterval} ms.",
            bridgeOptions.Udp.RetryCount,
            bridgeOptions.Udp.CommandAckTimeoutMs,
            bridgeOptions.Udp.HeartbeatIntervalMs);

        _logger.LogWarning(
            "AOG-Link translator has not been configured yet. Commands and telemetry will not flow until NX-118 completes.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping placeholder AOG-Link gateway.");
        return Task.CompletedTask;
    }
}
