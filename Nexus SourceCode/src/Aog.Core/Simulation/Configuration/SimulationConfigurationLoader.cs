using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aog.Core.Simulation.Configuration;

/// <summary>
/// Loads simulation configuration documents from JSON.
/// </summary>
public static class SimulationConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Parses a simulation configuration JSON payload.
    /// </summary>
    /// <param name="json">The JSON payload.</param>
    /// <returns>A <see cref="SimulationConfiguration"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the payload cannot be parsed or is missing required fields.</exception>
    public static SimulationConfiguration Load(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        SimulationConfigurationModel? model;
        try
        {
            model = JsonSerializer.Deserialize<SimulationConfigurationModel>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse simulation configuration JSON.", ex);
        }

        return Map(model);
    }

    private static SimulationConfiguration Map(SimulationConfigurationModel? model)
    {
        if (model is null)
        {
            throw new InvalidOperationException("Simulation configuration JSON did not produce a document.");
        }

        if (string.IsNullOrWhiteSpace(model.SchemaVersion))
        {
            throw new InvalidOperationException("The simulation configuration must declare a non-empty schemaVersion.");
        }

        if (model.Providers is null || model.Providers.Count == 0)
        {
            throw new InvalidOperationException("At least one provider must be declared in the simulation configuration.");
        }

        var providers = model.Providers
            .Select(ConvertProvider)
            .ToArray();

        var routes = model.Routes is null
            ? Array.Empty<SimulationRouteConfiguration>()
            : model.Routes.Select(ConvertRoute).ToArray();

        var scenarios = model.Scenarios is null
            ? Array.Empty<SimulationScenarioConfiguration>()
            : model.Scenarios.Select(ConvertScenario).ToArray();

        var options = model.Options is null ? null : ConvertOptions(model.Options);

        return new SimulationConfiguration(model.SchemaVersion, providers, routes, options, scenarios);
    }

    private static SimulationProviderConfiguration ConvertProvider(SimulationProviderModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ProviderId))
        {
            throw new InvalidOperationException("Provider entries must include a non-empty providerId.");
        }

        if (model.Outputs is null || model.Outputs.Count == 0)
        {
            throw new InvalidOperationException($"Provider '{model.ProviderId}' must declare at least one output topic.");
        }

        var outputs = model.Outputs.ToArray();
        var inputs = model.Inputs?.ToArray() ?? Array.Empty<string>();
        var settings = model.Settings ?? default;

        return new SimulationProviderConfiguration(model.ProviderId, outputs, inputs, model.Type, settings);
    }

    private static SimulationRouteConfiguration ConvertRoute(SimulationRouteModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Stream))
        {
            throw new InvalidOperationException("Route entries must include a non-empty stream.");
        }

        if (string.IsNullOrWhiteSpace(model.Source))
        {
            throw new InvalidOperationException($"Route for stream '{model.Stream}' must include a non-empty source.");
        }

        return new SimulationRouteConfiguration(model.Stream, model.Source, model.Mode ?? "simulation");
    }

    private static SimulationScenarioConfiguration ConvertScenario(SimulationScenarioModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ScenarioId))
        {
            throw new InvalidOperationException("Scenario entries must include a non-empty scenarioId.");
        }

        var routes = model.Routes is null
            ? Array.Empty<SimulationRouteConfiguration>()
            : model.Routes.Select(ConvertRoute).ToArray();

        var options = model.Options is null ? null : ConvertOptions(model.Options);

        return new SimulationScenarioConfiguration(model.ScenarioId, model.Description, routes, options);
    }

    private static SimulationOptionsConfiguration? ConvertOptions(SimulationOptionsModel model)
    {
        if (model is null)
        {
            return null;
        }

        return new SimulationOptionsConfiguration(model.Seed, model.TimeScale);
    }

    private sealed class SimulationConfigurationModel
    {
        [JsonPropertyName("schemaVersion")]
        public string? SchemaVersion { get; set; }

        [JsonPropertyName("providers")]
        public List<SimulationProviderModel>? Providers { get; set; }

        [JsonPropertyName("routes")]
        public List<SimulationRouteModel>? Routes { get; set; }

        [JsonPropertyName("options")]
        public SimulationOptionsModel? Options { get; set; }

        [JsonPropertyName("scenarios")]
        public List<SimulationScenarioModel>? Scenarios { get; set; }
    }

    private sealed class SimulationProviderModel
    {
        [JsonPropertyName("providerId")]
        public string? ProviderId { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("settings")]
        public JsonElement? Settings { get; set; }

        [JsonPropertyName("inputs")]
        public List<string>? Inputs { get; set; }

        [JsonPropertyName("outputs")]
        public List<string>? Outputs { get; set; }
    }

    private sealed class SimulationRouteModel
    {
        [JsonPropertyName("stream")]
        public string? Stream { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("mode")]
        public string? Mode { get; set; }
    }

    private sealed class SimulationScenarioModel
    {
        [JsonPropertyName("scenarioId")]
        public string? ScenarioId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("routes")]
        public List<SimulationRouteModel>? Routes { get; set; }

        [JsonPropertyName("options")]
        public SimulationOptionsModel? Options { get; set; }
    }

    private sealed class SimulationOptionsModel
    {
        [JsonPropertyName("seed")]
        public int? Seed { get; set; }

        [JsonPropertyName("timeScale")]
        public double? TimeScale { get; set; }
    }
}
