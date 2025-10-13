using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Aog.Core.Simulation.Configuration;

/// <summary>
/// Represents a simulation configuration document loaded from the JSON schema defined in tools/schemas.
/// </summary>
public sealed class SimulationConfiguration
{
    public SimulationConfiguration(
        string schemaVersion,
        IReadOnlyList<SimulationProviderConfiguration> providers,
        IReadOnlyList<SimulationRouteConfiguration> routes,
        SimulationOptionsConfiguration? options,
        IReadOnlyList<SimulationScenarioConfiguration> scenarios)
    {
        SchemaVersion = string.IsNullOrWhiteSpace(schemaVersion)
            ? throw new ArgumentException("Schema version must be provided.", nameof(schemaVersion))
            : schemaVersion;
        Providers = providers ?? throw new ArgumentNullException(nameof(providers));
        if (Providers.Count == 0)
        {
            throw new ArgumentException("At least one provider must be declared.", nameof(providers));
        }

        Routes = routes ?? throw new ArgumentNullException(nameof(routes));
        Options = options;
        Scenarios = scenarios ?? throw new ArgumentNullException(nameof(scenarios));
    }

    /// <summary>
    /// Version of the schema the document adheres to.
    /// </summary>
    public string SchemaVersion { get; }

    /// <summary>
    /// Providers declared in the configuration.
    /// </summary>
    public IReadOnlyList<SimulationProviderConfiguration> Providers { get; }

    /// <summary>
    /// Default stream routing rules declared at the root of the document.
    /// </summary>
    public IReadOnlyList<SimulationRouteConfiguration> Routes { get; }

    /// <summary>
    /// Global options applied to all scenarios unless overridden.
    /// </summary>
    public SimulationOptionsConfiguration? Options { get; }

    /// <summary>
    /// Scenario definitions bundled with the configuration.
    /// </summary>
    public IReadOnlyList<SimulationScenarioConfiguration> Scenarios { get; }

    /// <summary>
    /// Creates immutable provider descriptors that can be registered with the simulation catalog.
    /// </summary>
    public IReadOnlyList<SimulationProviderDescriptor> CreateProviderDescriptors()
    {
        return Providers.Select(provider => provider.ToDescriptor()).ToArray();
    }
}

/// <summary>
/// Describes a provider entry from the configuration document.
/// </summary>
public sealed class SimulationProviderConfiguration
{
    public SimulationProviderConfiguration(
        string providerId,
        IReadOnlyList<string> outputs,
        IReadOnlyList<string> inputs,
        string? type,
        JsonElement settings)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider identifier must be provided.", nameof(providerId));
        }

        ProviderId = providerId;
        Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
        if (Outputs.Count == 0)
        {
            throw new ArgumentException("Providers must declare at least one output topic.", nameof(outputs));
        }

        Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        Type = type;
        Settings = settings;
    }

    /// <summary>
    /// Unique identifier for the provider.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Topics produced by the provider.
    /// </summary>
    public IReadOnlyList<string> Outputs { get; }

    /// <summary>
    /// Topics required before the provider can emit outputs.
    /// </summary>
    public IReadOnlyList<string> Inputs { get; }

    /// <summary>
    /// Optional implementation type hint for the provider.
    /// </summary>
    public string? Type { get; }

    /// <summary>
    /// Arbitrary provider-specific settings payload.
    /// </summary>
    public JsonElement Settings { get; }

    internal SimulationProviderDescriptor ToDescriptor()
    {
        return new SimulationProviderDescriptor(ProviderId, Outputs, Inputs);
    }
}

/// <summary>
/// Declares the source that produces a particular stream during a scenario.
/// </summary>
public sealed class SimulationRouteConfiguration
{
    public SimulationRouteConfiguration(string stream, string source, string mode)
    {
        if (string.IsNullOrWhiteSpace(stream))
        {
            throw new ArgumentException("Stream must be provided.", nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source must be provided.", nameof(source));
        }

        Stream = stream;
        Source = source;
        Mode = string.IsNullOrWhiteSpace(mode) ? "simulation" : mode;
    }

    public string Stream { get; }

    public string Source { get; }

    public string Mode { get; }
}

/// <summary>
/// Global or per-scenario options controlling deterministic behaviour.
/// </summary>
public sealed class SimulationOptionsConfiguration
{
    public SimulationOptionsConfiguration(int? seed, double? timeScale)
    {
        Seed = seed;
        TimeScale = timeScale;
    }

    public int? Seed { get; }

    public double? TimeScale { get; }
}

/// <summary>
/// Individual scenario override embedded in the configuration.
/// </summary>
public sealed class SimulationScenarioConfiguration
{
    public SimulationScenarioConfiguration(
        string scenarioId,
        string? description,
        IReadOnlyList<SimulationRouteConfiguration> routes,
        SimulationOptionsConfiguration? options)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException("Scenario identifier must be provided.", nameof(scenarioId));
        }

        ScenarioId = scenarioId;
        Description = description;
        Routes = routes ?? throw new ArgumentNullException(nameof(routes));
        Options = options;
    }

    public string ScenarioId { get; }

    public string? Description { get; }

    public IReadOnlyList<SimulationRouteConfiguration> Routes { get; }

    public SimulationOptionsConfiguration? Options { get; }
}
