using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Aog.UI.Avalonia.Views.Main;
using Aog.UI.Avalonia.Hosting;

namespace Aog.UI.Avalonia.App;

public partial class NexusApp : Application
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly ILogger<NexusApp> _logger;

    public NexusApp()
    : this(
        AvaloniaServiceProviderAccessor.Current,
        AvaloniaServiceProviderAccessor.Current?.GetService<ILogger<NexusApp>>() 
            ?? NullLogger<NexusApp>.Instance)
{
}


    public NexusApp(IServiceProvider serviceProvider, ILogger<NexusApp> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_serviceProvider is not null)
            {
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                desktop.MainWindow = mainWindow;
            }
            else
            {
                _logger.LogWarning("Service provider not available; skipping main window creation.");
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
