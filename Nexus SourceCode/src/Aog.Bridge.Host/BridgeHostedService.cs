using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.Bridge.Host;

public sealed class BridgeHostedService : IHostedService
{
    private readonly IAogLinkGateway _gateway;
    private readonly ILogger<BridgeHostedService> _logger;

    public BridgeHostedService(IAogLinkGateway gateway, ILogger<BridgeHostedService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting AOG-Link bridge host services.");
        await _gateway.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping AOG-Link bridge host services.");
        await _gateway.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
