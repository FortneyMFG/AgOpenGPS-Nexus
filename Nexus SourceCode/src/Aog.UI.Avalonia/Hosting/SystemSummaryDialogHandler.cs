using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.Views.Main;
using Aog.UI.Avalonia.Views.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Handles shell commands that surface the system summary dialog from the Tools menu.
/// </summary>
public sealed class SystemSummaryDialogHandler : IShellCommandHandler
{
    private const string CommandId = "core.shell.systemSummary";
    private static readonly HashSet<string> SupportedInjectionPoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "menu.tools",
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SystemSummaryDialogHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="SystemSummaryDialogHandler"/> class.</summary>
    public SystemSummaryDialogHandler(IServiceProvider serviceProvider, ILogger<SystemSummaryDialogHandler> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool CanHandle(string injectionPoint) => SupportedInjectionPoints.Contains(injectionPoint);

    /// <inheritdoc />
    public async ValueTask<bool> HandleAsync(string injectionPoint, string commandId, CancellationToken cancellationToken)
    {
        if (!SupportedInjectionPoints.Contains(injectionPoint) || !string.Equals(commandId, CommandId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Task? dialogTask = null;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var owner = _serviceProvider.GetRequiredService<MainWindow>();
            var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            var dialog = new SystemSummaryDialog(mainViewModel)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            dialogTask = dialog.ShowDialog(owner);
        });

        if (dialogTask is null)
        {
            _logger.LogWarning("Failed to open system summary dialog for {InjectionPoint}::{CommandId}", injectionPoint, commandId);
            return false;
        }

        await dialogTask.ConfigureAwait(false);
        return true;
    }
}
