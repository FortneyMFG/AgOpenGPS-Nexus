using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.UI.Avalonia.ViewModels.Shell;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Handles shell layout commands dispatched from block shortcuts and menu actions.
/// </summary>
public sealed class LayoutShellCommandHandler : IShellCommandHandler
{
    private readonly BlockLayoutViewModel _layout;
    private readonly AppShellViewModel _shell;
    private readonly ILogger<LayoutShellCommandHandler> _logger;

    public LayoutShellCommandHandler(
        BlockLayoutViewModel layout,
        AppShellViewModel shell,
        ILogger<LayoutShellCommandHandler> logger)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool CanHandle(string injectionPoint) =>
        string.Equals(injectionPoint, "Layout", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public ValueTask<bool> HandleAsync(string injectionPoint, string commandId, CancellationToken cancellationToken)
    {
        if (!CanHandle(injectionPoint))
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(HandleCommand(commandId));
    }

    private bool HandleCommand(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return false;
        }

        return commandId switch
        {
            "ToggleFieldDock" => ToggleFieldDock(),
            _ => LogUnknown(commandId),
        };
    }

    private bool ToggleFieldDock()
    {
        var shouldPin = !_layout.IsFieldDockPinned;
        _layout.IsFieldDockPinned = shouldPin;
        _shell.StatusText = shouldPin
            ? "Field settings dock pinned. Drag blocks onto the workspace."
            : "Field settings dock hidden.";
        return true;
    }

    private bool LogUnknown(string commandId)
    {
        _logger.LogDebug("Unhandled layout command {CommandId}", commandId);
        return false;
    }
}
