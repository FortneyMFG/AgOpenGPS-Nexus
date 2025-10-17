using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.Telemetry;
using Aog.UI.Avalonia.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.UI.Avalonia;

public partial class NexusApp : Application
{
    private readonly IServiceProvider _services;

    public NexusApp(
        IServiceProvider services,
        IUiPreferencesService preferencesService,
        IThemeManager themeManager,
        ICrashTelemetryService crashTelemetryService)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(preferencesService);
        ArgumentNullException.ThrowIfNull(themeManager);
        ArgumentNullException.ThrowIfNull(crashTelemetryService);

        _services = services;

        var preferences = preferencesService.GetPreferences();
        themeManager.ApplyTheme(preferences.Theme);
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}