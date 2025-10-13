using System;
using System.Collections.Generic;
using System.Text;

namespace Aog.Core.Simulation;

/// <summary>
/// Represents an ordered view of simulation providers with helper utilities for diagnostics.
/// </summary>
public sealed class SimulationGraph
{
    internal SimulationGraph(IReadOnlyList<SimulationProviderDescriptor> orderedProviders)
    {
        Providers = orderedProviders ?? throw new ArgumentNullException(nameof(orderedProviders));
    }

    /// <summary>
    /// Providers ordered so each entry appears after its dependencies.
    /// </summary>
    public IReadOnlyList<SimulationProviderDescriptor> Providers { get; }

    /// <summary>
    /// Renders a human-friendly summary of the graph for logging or diagnostic output.
    /// </summary>
    public string FormatSummary()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Simulation Provider Graph:");
        foreach (var provider in Providers)
        {
            builder
                .Append(" - ")
                .Append(provider.ProviderId)
                .Append(" | outputs: ")
                .Append('[')
                .Append(string.Join(", ", provider.Outputs))
                .Append("] | inputs: ");

            if (provider.Inputs.Count == 0)
            {
                builder.Append("[]");
            }
            else
            {
                builder
                    .Append('[')
                    .Append(string.Join(", ", provider.Inputs))
                    .Append(']');
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}
