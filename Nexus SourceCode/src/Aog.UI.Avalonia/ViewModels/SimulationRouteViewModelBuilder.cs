using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aog.Core.Simulation.Configuration;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Helper for constructing <see cref="SimulationStreamRouteViewModel"/> collections from configuration models.
/// </summary>
internal static class SimulationRouteViewModelBuilder
{
    private static readonly string[] DefaultModeOptions = new[] { "simulation", "hardware", "replay" };

    public static ReadOnlyCollection<SimulationStreamRouteViewModel> BuildRoutes(
        SimulationConfiguration? configuration,
        IEnumerable<SimulationRouteConfiguration> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        if (configuration is null)
        {
            var fallback = routes
                .Select(route => new SimulationStreamRouteViewModel(
                    route.Stream,
                    route.Source,
                    route.Mode,
                    new[] { route.Source },
                    DefaultModeOptions))
                .OrderBy(route => route.Stream, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ReadOnlyCollection<SimulationStreamRouteViewModel>(fallback);
        }

        var providersByStream = BuildProvidersByStream(configuration);
        var modeOptions = BuildModeOptions(configuration, routes);

        var viewModels = routes
            .Select(route =>
            {
                if (!providersByStream.TryGetValue(route.Stream, out var sources) || sources.Count == 0)
                {
                    sources = new List<string> { route.Source };
                }

                return new SimulationStreamRouteViewModel(
                    route.Stream,
                    route.Source,
                    route.Mode,
                    sources,
                    modeOptions);
            })
            .OrderBy(route => route.Stream, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ReadOnlyCollection<SimulationStreamRouteViewModel>(viewModels);
    }

    private static Dictionary<string, List<string>> BuildProvidersByStream(SimulationConfiguration configuration)
    {
        var providersByStream = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in configuration.Providers)
        {
            foreach (var output in provider.Outputs)
            {
                if (!providersByStream.TryGetValue(output, out var list))
                {
                    list = new List<string>();
                    providersByStream[output] = list;
                }

                if (!list.Any(candidate => candidate.Equals(provider.ProviderId, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(provider.ProviderId);
                }
            }
        }

        return providersByStream;
    }

    private static ReadOnlyCollection<string> BuildModeOptions(
        SimulationConfiguration configuration,
        IEnumerable<SimulationRouteConfiguration> routes)
    {
        var modeOptions = new HashSet<string>(DefaultModeOptions, StringComparer.OrdinalIgnoreCase);

        foreach (var route in configuration.Routes)
        {
            modeOptions.Add(route.Mode);
        }

        foreach (var route in routes)
        {
            modeOptions.Add(route.Mode);
        }

        var ordered = modeOptions
            .OrderBy(mode => mode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ReadOnlyCollection<string>(ordered);
    }
}
