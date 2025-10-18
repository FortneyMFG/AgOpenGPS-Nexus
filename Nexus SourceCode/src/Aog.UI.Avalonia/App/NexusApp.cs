using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aog.UI.Avalonia.Views.Main;

namespace Aog.UI.Avalonia.App;

public partial class NexusApp : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NexusApp> _logger;

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
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
