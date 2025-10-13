using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Core.Simulation;

/// <summary>
/// Describes the inputs and outputs for a simulation provider. The descriptor keeps the
/// metadata immutable so the registry can reason about the provider graph.
/// </summary>
public sealed class SimulationProviderDescriptor
{
    private readonly IReadOnlyCollection<string> _inputs;
    private readonly IReadOnlyCollection<string> _outputs;

    public SimulationProviderDescriptor(
        string providerId,
        IEnumerable<string> outputs,
        IEnumerable<string>? inputs = null)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider identifier must be a non-empty string.", nameof(providerId));
        }

        ProviderId = providerId;
        _outputs = Normalize(outputs, nameof(outputs));
        if (_outputs.Count == 0)
        {
            throw new ArgumentException("At least one output topic must be declared.", nameof(outputs));
        }

        _inputs = inputs is null
            ? Array.Empty<string>()
            : Normalize(inputs, nameof(inputs));
    }

    /// <summary>
    /// Unique identifier for the provider. Plugins should use a reverse-DNS prefix to avoid
    /// collisions (e.g. <c>sim.pose.basic</c>).
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Topics produced by the provider and published on the simulation bus.
    /// </summary>
    public IReadOnlyCollection<string> Outputs => _outputs;

    /// <summary>
    /// Topics required by the provider before it can produce its outputs.
    /// </summary>
    public IReadOnlyCollection<string> Inputs => _inputs;

    private static IReadOnlyCollection<string> Normalize(IEnumerable<string> values, string parameterName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Topic names must be non-empty strings.", parameterName);
            }

            if (!unique.Add(value))
            {
                throw new ArgumentException($"Duplicate topic '{value}' declared in {parameterName}.", parameterName);
            }
        }

        return unique.ToArray();
    }
}
