using Microsoft.Extensions.DependencyInjection;

namespace Aog.Agio;

/// <summary>
/// Defines the contract implemented by hardware or simulation backends.
/// </summary>
public interface IAgioBackend
{
    /// <summary>
    /// Gets the display name for the backend.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers backend services with the AGiO host dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    void ConfigureServices(IServiceCollection services);
}
