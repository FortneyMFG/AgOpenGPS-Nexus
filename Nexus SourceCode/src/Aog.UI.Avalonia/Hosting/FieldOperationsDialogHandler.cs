using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.Views.FieldOperations;
using Aog.UI.Avalonia.Views.Main;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Handles shell commands that surface field operation dialogs such as boundary editor, flag manager, and shift position.
/// </summary>
public sealed class FieldOperationsDialogHandler : IShellCommandHandler
{
    private static readonly HashSet<string> SupportedInjectionPoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "dialog.boundary",
        "dialog.flags",
        "tools.offset",
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FieldOperationsDialogHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldOperationsDialogHandler"/> class.
    /// </summary>
    public FieldOperationsDialogHandler(IServiceProvider serviceProvider, ILogger<FieldOperationsDialogHandler> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool CanHandle(string injectionPoint) => SupportedInjectionPoints.Contains(injectionPoint);

    /// <inheritdoc />
    public async ValueTask<bool> HandleAsync(string injectionPoint, string commandId, CancellationToken cancellationToken)
    {
        if (!SupportedInjectionPoints.Contains(injectionPoint))
        {
            return false;
        }

        Task? dialogTask = null;
        var handled = false;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            Window? dialog = injectionPoint.ToLowerInvariant() switch
            {
                "dialog.boundary" => new Views.BoundaryWindow(viewModel.CreateBoundaryToolViewModel()),
                "dialog.flags" => new FlagManagerDialog(viewModel.CreateFlagManagerDialogViewModel()),
                "tools.offset" => new ShiftPositionDialog(viewModel.CreateShiftPositionDialogViewModel()),
                _ => null,
            };

            if (dialog is null)
            {
                _logger.LogWarning("No dialog available for injection point {InjectionPoint} (command {CommandId}).", injectionPoint, commandId);
                return;
            }

            var owner = _serviceProvider.GetRequiredService<MainWindow>();
            handled = true;
            dialogTask = dialog.ShowDialog(owner);
        });

        if (!handled || dialogTask is null)
        {
            return handled;
        }

        await dialogTask.ConfigureAwait(false);
        return true;
    }
}

