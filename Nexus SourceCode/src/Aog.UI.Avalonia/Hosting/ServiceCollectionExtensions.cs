using System;
using Aog.Core.Replay;
using Aog.UI.Avalonia.App;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.Telemetry;
using Aog.UI.Avalonia.Theming;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.ViewModels.Shell;
using Aog.UI.Avalonia.Views;
using Aog.UI.Avalonia.Views.Main;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Extension methods for registering the Avalonia UI shell with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Avalonia shell window, view-models, and application with the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The original service collection to support fluent configuration.</returns>
    public static IServiceCollection AddAvaloniaUiShell(this IServiceCollection services)
    {
        services.TryAddSingleton<NexusApp>();
        services.TryAddSingleton<IRunModePlatform, SystemRunModePlatform>();
        services.TryAddSingleton<IAvaloniaRunModeService, AvaloniaRunModeService>();
        services.TryAddSingleton<IConnectionSettingsStore, JsonConnectionSettingsStore>();
        services.TryAddSingleton<IUiPreferencesStore, JsonUiPreferencesStore>();
        services.TryAddSingleton<IUiPreferencesService, UiPreferencesService>();
        services.TryAddSingleton<IBlockCatalog, BlockCatalog>();
        services.TryAddSingleton<IBlockLayoutStore, BlockLayoutStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBlockProvider, CoreBlockProvider>());
        services.TryAddSingleton<BlockLayoutViewModel>();
        services.TryAddSingleton<IThemeManager, ThemeManager>();
        services.TryAddSingleton<ICrashTelemetryService, CrashTelemetryService>();
        services.TryAddSingleton<IShellCommandDispatcher, ShellCommandDispatcher>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IShellCommandHandler, FieldOperationsDialogHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IShellCommandHandler, LoggingShellCommandHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IShellCommandHandler, SystemSummaryDialogHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IShellCommandHandler, PluginManagerDialogHandler>());
        services.TryAddSingleton<BackendServiceManager>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IShellCommandHandler, BackendServiceCommandHandler>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ConnectionSettingsViewModel>();
        services.TryAddSingleton<TelemetryPrivacyViewModel>();
        services.TryAddSingleton<IReplayController, NullReplayController>();
        services.TryAddSingleton<MainWindow>();
        services.TryAddSingleton<MainWindowViewModel>();
        services.TryAddSingleton<IFieldOperationsDialogHost>(sp => sp.GetRequiredService<MainWindowViewModel>());
        services.TryAddSingleton<ISystemSummaryViewModel>(sp => sp.GetRequiredService<MainWindowViewModel>());
        services.TryAddSingleton<AppShellViewModel>();
        services.TryAddSingleton<PluginManagerViewModel>();
        services.AddTransient<PluginManagerWindow>();
        services.TryAddSingleton<Plugins.PluginRegistry>();
        services.TryAddSingleton<Plugins.PluginPathProvider>();
        services.TryAddSingleton<Plugins.PluginHost>();
        services.TryAddSingleton<Plugins.PluginBootstrapper>();
        return services;
    }
}








