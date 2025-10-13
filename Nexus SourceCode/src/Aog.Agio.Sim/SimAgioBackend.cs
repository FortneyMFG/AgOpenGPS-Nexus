using System;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.Agio.Sim;

/// <summary>
/// Minimal simulation backend used as the default AGiO host target during early development.
/// </summary>
public sealed class SimAgioBackend : IAgioBackend
{
    /// <inheritdoc />
    public string Name => "Simulation";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<SimulationBackendMarker>();
    }

    private sealed record SimulationBackendMarker;
}
