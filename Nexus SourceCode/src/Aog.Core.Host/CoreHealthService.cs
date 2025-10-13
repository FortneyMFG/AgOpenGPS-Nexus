using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Core.Host;

/// <summary>
/// Emits periodic health log messages so deployment tooling can verify the core host is responsive.
/// </summary>
public sealed class CoreHealthService : BackgroundService
{
    private readonly ILogger<CoreHealthService> _logger;
    private readonly CoreHealthOptions _options;

    public CoreHealthService(ILogger<CoreHealthService> logger, IOptions<CoreHealthOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Nexus Core host.");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Nexus Core host.");
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Core host health reporting active. Interval: {IntervalSeconds}s.",
            _options.IntervalSeconds);

        var delay = TimeSpan.FromSeconds(_options.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _logger.LogInformation("Core host heartbeat OK.");
        }
    }
}
