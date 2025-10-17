using System.Threading;
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
    private readonly IOptionsMonitor<CoreHealthOptions> _optionsMonitor;
    private readonly TimeProvider _timeProvider;
    private readonly IDisposable? _optionsChangeSubscription;
    private int _intervalSeconds;

    public CoreHealthService(
        ILogger<CoreHealthService> logger,
        IOptionsMonitor<CoreHealthOptions> optionsMonitor,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _timeProvider = timeProvider ?? TimeProvider.System;

        UpdateInterval(_optionsMonitor.CurrentValue, logChange: false);
        _optionsChangeSubscription = _optionsMonitor.OnChange((options, _) => UpdateInterval(options, logChange: true));
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
            Volatile.Read(ref _intervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delaySeconds = Volatile.Read(ref _intervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _logger.LogInformation("Core host heartbeat OK.");
        }
    }

    public override void Dispose()
    {
        _optionsChangeSubscription?.Dispose();
        base.Dispose();
    }

    private void UpdateInterval(CoreHealthOptions options, bool logChange)
    {
        ArgumentNullException.ThrowIfNull(options);

        var newValue = options.IntervalSeconds;
        var previous = Interlocked.Exchange(ref _intervalSeconds, newValue);

        if (logChange && previous != newValue)
        {
            _logger.LogInformation("Core host health interval updated to {IntervalSeconds}s.", newValue);
        }
    }
}
