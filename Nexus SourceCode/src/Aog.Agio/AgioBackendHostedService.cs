using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.Agio;

/// <summary>
/// Emits lifecycle logs for the configured backend.
/// </summary>
public sealed class AgioBackendHostedService : IHostedService
{
    private readonly ILogger<AgioBackendHostedService> _logger;
    private readonly AgioBackendRegistration _registration;

    public AgioBackendHostedService(ILogger<AgioBackendHostedService> logger, AgioBackendRegistration registration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AGiO backend {BackendName} ({BackendType}) loaded from {AssemblyName}.",
            _registration.BackendName,
            _registration.BackendType.FullName,
            _registration.AssemblyName);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AGiO backend {BackendName} shutting down.", _registration.BackendName);
        return Task.CompletedTask;
    }
}
