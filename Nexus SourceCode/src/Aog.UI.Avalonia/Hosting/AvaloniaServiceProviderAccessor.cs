using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Provides access to the Avalonia application's root <see cref="IServiceProvider"/>.
/// </summary>
internal static class AvaloniaServiceProviderAccessor
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes the accessor with the root service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider created by the host.</param>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var existing = Interlocked.CompareExchange(ref _serviceProvider, serviceProvider, null);
        if (existing is not null && !ReferenceEquals(existing, serviceProvider))
        {
            throw new InvalidOperationException("Avalonia service provider has already been initialized.");
        }
    }

    /// <summary>
    /// Attempts to retrieve the configured service provider.
    /// </summary>
    /// <param name="serviceProvider">The resolved provider when available.</param>
    /// <returns><c>true</c> when the provider has been initialized; otherwise <c>false</c>.</returns>
    public static bool TryGetServiceProvider(out IServiceProvider serviceProvider)
    {
        var provider = Volatile.Read(ref _serviceProvider);

        serviceProvider = provider!;
        return provider is not null;
    }

    /// <summary>
    /// Gets the initialized service provider.
    /// </summary>
    public static IServiceProvider Current =>
        _serviceProvider ?? throw new InvalidOperationException("Avalonia service provider has not been initialized.");

    /// <summary>
    /// Resolves a service from the root provider.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    public static T GetRequiredService<T>() where T : notnull => Current.GetRequiredService<T>();
}
