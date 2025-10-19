using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.Views;

namespace Aog.UI.Avalonia.Hosting;

public sealed class PluginManagerDialogHandler : IShellCommandHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PluginManagerDialogHandler> _logger;

    public PluginManagerDialogHandler(IServiceProvider serviceProvider, ILogger<PluginManagerDialogHandler> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanHandle(string injectionPoint) => string.Equals(injectionPoint, "menu.plugins", StringComparison.OrdinalIgnoreCase);

    public async ValueTask<bool> HandleAsync(string injectionPoint, string commandId, CancellationToken cancellationToken)
    {
        if (!CanHandle(injectionPoint))
        {
            return false;
        }

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var window = _serviceProvider.GetRequiredService<PluginManagerWindow>();
                window.DataContext ??= _serviceProvider.GetRequiredService<PluginManagerViewModel>();

                if (window.DataContext is PluginManagerViewModel manager)
                {
                    manager.Refresh();
                }

                if (_serviceProvider.GetService<Aog.UI.Avalonia.Views.Main.MainWindow>() is { } owner)
                {
                    window.Show(owner);
                }
                else
                {
                    window.Show();
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open plugin manager window.");
            return false;
        }
    }
}
