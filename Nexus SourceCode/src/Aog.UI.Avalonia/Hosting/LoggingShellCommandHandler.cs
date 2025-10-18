using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Minimal handler that logs shell command dispatches for core injection points.
/// </summary>
public sealed class LoggingShellCommandHandler : IShellCommandHandler
{
    private static readonly HashSet<string> KnownInjectionPoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "menu.file",
        "menu.tools",
        "menu.settings",
        "menu.field",
        "toolbar.top",
        "shell.main",
        "shell.map",
        "dialog.boundary",
        "dialog.flags",
        "tools.offset",
    };

    private readonly ILogger<LoggingShellCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingShellCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger used for diagnostics.</param>
    public LoggingShellCommandHandler(ILogger<LoggingShellCommandHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool CanHandle(string injectionPoint) => KnownInjectionPoints.Contains(injectionPoint);

    /// <inheritdoc />
    public ValueTask<bool> HandleAsync(string injectionPoint, string commandId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Dispatching shell command {InjectionPoint}::{CommandId}", injectionPoint, commandId);
        return ValueTask.FromResult(true);
    }
}
