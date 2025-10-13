using Aog.UI.Avalonia.ViewModels;
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
        services.TryAddSingleton<App>();
        services.TryAddSingleton<MainWindow>();
        services.TryAddSingleton<MainWindowViewModel>();
        return services;
    }
}
