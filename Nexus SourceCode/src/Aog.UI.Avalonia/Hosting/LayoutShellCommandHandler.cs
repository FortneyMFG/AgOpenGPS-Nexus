using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.UI.Avalonia.ViewModels.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Handles shell layout commands dispatched from block shortcuts and menu actions.
/// </summary>
public sealed class LayoutShellCommandHandler : IShellCommandHandler
{
    private readonly IServiceProvider _services;
    private BlockLayoutViewModel? _layout;
    private AppShellViewModel? _shell;
    private readonly ILogger<LayoutShellCommandHandler> _logger;

    public LayoutShellCommandHandler(
        IServiceProvider services,
        ILogger<LayoutShellCommandHandler> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
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
        var layout = GetLayout();
        var shell = GetShell();

        if (!layout.IsLocked)
        {
            var shouldPin = !layout.IsFieldDockPinned;
            layout.IsFieldDockPinned = shouldPin;
            shell.IsFieldMenuOpen = shouldPin;
            shell.StatusText = shouldPin
                ? "Field settings dock pinned. Drag blocks onto the workspace."
                : "Field settings dock hidden.";
            return true;
        }

        if (layout.IsFieldDockPinned)
        {
            layout.IsFieldDockPinned = false;
            shell.IsFieldMenuOpen = false;
            shell.StatusText = "Field settings dock hidden.";
            return true;
        }

        var shouldOpen = !shell.IsFieldMenuOpen;
        shell.IsFieldMenuOpen = shouldOpen;
        shell.StatusText = shouldOpen
            ? "Field settings dock open. Pin it to keep it visible."
            : "Field settings dock hidden.";
        return true;
    }

    private BlockLayoutViewModel GetLayout() =>
        _layout ??= _services.GetRequiredService<BlockLayoutViewModel>();

    private AppShellViewModel GetShell() =>
        _shell ??= _services.GetRequiredService<AppShellViewModel>();

    private bool LogUnknown(string commandId)
    {
        _logger.LogDebug("Unhandled layout command {CommandId}", commandId);
        return false;
    }
}
