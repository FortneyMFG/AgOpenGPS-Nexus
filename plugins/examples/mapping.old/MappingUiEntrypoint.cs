using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using MappingPlugin.Views;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Sdk.Core;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.ViewModels.Shell;

namespace MappingPlugin;

public sealed class MappingUiEntrypoint : IUiEntrypoint
{
    private AppShellViewModel? _shell;
    private Control? _mapControl;

    public void Initialize(IHostServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var provider = services.Services;
        _shell = provider.GetService<AppShellViewModel>();
        var mainWindow = provider.GetService<MainWindowViewModel>();
        if (_shell is null || mainWindow is null)
        {
            Console.WriteLine("[MappingPlugin] Host services unavailable (shell or main window null).");
            return;
        }

        var mapView = new MapSurfaceView
        {
            DataContext = mainWindow
        };

        _mapControl = mapView;
        _shell.MainContent = mapView;
        _shell.CurrentView = "Field Map";
        _shell.StatusText = "Mapping surface ready.";
        Console.WriteLine("[MappingPlugin] Mapping surface injected.");
    }

    public ValueTask ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[MappingPlugin] Shutdown invoked.");
        if (_shell is not null && _shell.MainContent == _mapControl)
        {
            _shell.MainContent = null;
            _shell.CurrentView = "Select a tool";
        }

        _mapControl = null;
        _shell = null;
        return ValueTask.CompletedTask;
    }
}
