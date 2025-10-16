using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Resolves shell command invocations to registered handlers.
/// </summary>
public sealed class ShellCommandDispatcher : IShellCommandDispatcher
{
    private readonly IEnumerable<IShellCommandHandler> _handlers;
    private readonly ILogger<ShellCommandDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellCommandDispatcher"/> class.
    /// </summary>
    /// <param name="handlers">Registered command handlers.</param>
    /// <param name="logger">Logger used for diagnostics.</param>
    public ShellCommandDispatcher(IEnumerable<IShellCommandHandler> handlers, ILogger<ShellCommandDispatcher> logger)
    {
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask<bool> DispatchAsync(string injectionPoint, string commandId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(injectionPoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        var handled = false;
        foreach (var handler in _handlers.Where(handler => handler.CanHandle(injectionPoint)))
        {
            try
            {
                if (await handler.HandleAsync(injectionPoint, commandId, cancellationToken).ConfigureAwait(false))
                {
                    handled = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shell command handler failed for {InjectionPoint}::{CommandId}", injectionPoint, commandId);
            }
        }

        if (!handled)
        {
            _logger.LogWarning("No handler processed shell command {InjectionPoint}::{CommandId}", injectionPoint, commandId);
        }

        return handled;
    }
}
