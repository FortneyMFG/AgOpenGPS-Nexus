using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Routes shell commands to backend service lifecycle operations.
/// </summary>
public sealed class BackendServiceCommandHandler : IShellCommandHandler
{
    private static readonly HashSet<string> SupportedInjectionPoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "services.backend",
    };

    private readonly BackendServiceManager _manager;
    private readonly ILogger<BackendServiceCommandHandler> _logger;

    public BackendServiceCommandHandler(BackendServiceManager manager, ILogger<BackendServiceCommandHandler> logger)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanHandle(string injectionPoint) => SupportedInjectionPoints.Contains(injectionPoint);

    public async ValueTask<bool> HandleAsync(string injectionPoint, string commandId, CancellationToken cancellationToken)
    {
        if (!CanHandle(injectionPoint))
        {
            return false;
        }

        try
        {
            return commandId.ToLowerInvariant() switch
            {
                "services.core.start" => await _manager.StartAsync(BackendServiceKind.CoreHost, cancellationToken).ConfigureAwait(false),
                "services.core.stop" => await _manager.StopAsync(BackendServiceKind.CoreHost, cancellationToken).ConfigureAwait(false),
                "services.agio.windows.start" => await _manager.StartAsync(BackendServiceKind.AgioWindows, cancellationToken).ConfigureAwait(false),
                "services.agio.windows.stop" => await _manager.StopAsync(BackendServiceKind.AgioWindows, cancellationToken).ConfigureAwait(false),
                "services.agio.sim.start" => await _manager.StartAsync(BackendServiceKind.AgioSim, cancellationToken).ConfigureAwait(false),
                "services.agio.sim.stop" => await _manager.StopAsync(BackendServiceKind.AgioSim, cancellationToken).ConfigureAwait(false),
                _ => false,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backend service command '{CommandId}' failed.", commandId);
            throw;
        }
    }
}
