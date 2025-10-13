using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Aog.Core.Simulation.Configuration;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model for the scenario editor dialog.
/// </summary>
public sealed class ScenarioEditorViewModel : ObservableObject
{
    private readonly SimulationConfiguration? _configuration;
    private readonly IList<SimulationScenarioConfiguration> _scenarioDefinitions;
    private readonly Action<SimulationScenarioConfiguration> _applyScenario;
    private readonly Action _resetToDefaults;
    private readonly ObservableCollection<ScenarioDefinitionViewModel> _scenarios;
    private readonly DelegateCommand _applyScenarioCommand;
    private readonly DelegateCommand _addScenarioCommand;
    private readonly DelegateCommand _removeScenarioCommand;
    private readonly DelegateCommand _useDefaultsCommand;
    private ScenarioDefinitionViewModel? _selectedScenario;
    private string _statusMessage = "Select a scenario to review providers and options.";
    private bool _hasError;

    public ScenarioEditorViewModel(
        SimulationConfiguration? configuration,
        IList<SimulationScenarioConfiguration> scenarioDefinitions,
        Action<SimulationScenarioConfiguration> applyScenario,
        Action resetToDefaults)
    {
        _configuration = configuration;
        _scenarioDefinitions = scenarioDefinitions ?? throw new ArgumentNullException(nameof(scenarioDefinitions));
        _applyScenario = applyScenario ?? throw new ArgumentNullException(nameof(applyScenario));
        _resetToDefaults = resetToDefaults ?? throw new ArgumentNullException(nameof(resetToDefaults));

        _scenarios = new ObservableCollection<ScenarioDefinitionViewModel>(
            _scenarioDefinitions.Select(CreateScenarioViewModel));

        _applyScenarioCommand = new DelegateCommand(_ => ApplySelectedScenario(), _ => SelectedScenario is not null);
        _addScenarioCommand = new DelegateCommand(_ => AddScenario());
        _removeScenarioCommand = new DelegateCommand(_ => RemoveSelectedScenario(), _ => SelectedScenario is not null);
        _useDefaultsCommand = new DelegateCommand(_ => UseConfigurationDefaults());

        if (_scenarios.Count > 0)
        {
            SelectedScenario = _scenarios[0];
        }
    }

    /// <summary>Gets the available scenarios for selection.</summary>
    public ObservableCollection<ScenarioDefinitionViewModel> Scenarios => _scenarios;

    /// <summary>Gets or sets the currently selected scenario.</summary>
    public ScenarioDefinitionViewModel? SelectedScenario
    {
        get => _selectedScenario;
        set
        {
            if (!SetProperty(ref _selectedScenario, value))
            {
                return;
            }

            _applyScenarioCommand.RaiseCanExecuteChanged();
            _removeScenarioCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets the status text displayed at the bottom of the dialog.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets a value indicating whether the current status represents an error.</summary>
    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    /// <summary>Command invoked to apply the selected scenario to the simulation bar.</summary>
    public DelegateCommand ApplyScenarioCommand => _applyScenarioCommand;

    /// <summary>Command invoked to add a new scenario stub.</summary>
    public DelegateCommand AddScenarioCommand => _addScenarioCommand;

    /// <summary>Command invoked to remove the selected scenario.</summary>
    public DelegateCommand RemoveScenarioCommand => _removeScenarioCommand;

    /// <summary>Command invoked to revert to the configuration defaults.</summary>
    public DelegateCommand UseDefaultsCommand => _useDefaultsCommand;

    /// <summary>
    /// Attempts to export the selected scenario to JSON matching the simulation schema.
    /// </summary>
    public bool TryExportSelectedScenario(out string json, out string? errorMessage)
    {
        json = string.Empty;
        errorMessage = null;

        var scenario = CaptureScenarioFromSelection(out errorMessage);
        if (scenario is null)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                SetError(errorMessage);
            }

            return false;
        }

        json = SerializeScenario(scenario);
        SetStatus($"Scenario '{scenario.ScenarioId}' exported successfully.");
        return true;
    }

    /// <summary>
    /// Attempts to import a scenario JSON payload and merge it into the editor.
    /// </summary>
    public bool TryImportScenarioJson(string json, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            errorMessage = "Scenario JSON cannot be empty.";
            SetError(errorMessage);
            return false;
        }

        try
        {
            var scenario = ParseScenario(json);
            UpsertScenario(scenario);
            _applyScenario(scenario);
            SetStatus($"Scenario '{scenario.ScenarioId}' loaded.");
            return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
        {
            errorMessage = ex.Message;
            SetError($"Failed to load scenario: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Reports an external status message (for example, I/O completion) to the banner.
    /// </summary>
    /// <param name="message">Message to display.</param>
    public void ReportExternalStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        SetStatus(message);
    }

    /// <summary>
    /// Reports an external error (for example, an I/O failure) to the view-model status banner.
    /// </summary>
    /// <param name="message">Error message to display.</param>
    public void ReportExternalError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        SetError(message);
    }

    private void AddScenario()
    {
        if (_configuration is null)
        {
            SetError("A simulation configuration must be loaded before scenarios can be created.");
            return;
        }

        var routes = _configuration.Routes ?? Array.Empty<SimulationRouteConfiguration>();
        if (routes.Count == 0)
        {
            SetError("The configuration does not declare any routes to seed a scenario.");
            return;
        }

        var newScenarioId = CreateUniqueScenarioId();
        var scenario = new SimulationScenarioConfiguration(
            newScenarioId,
            "New scenario",
            routes,
            _configuration.Options);

        UpsertScenario(scenario, select: true, overwrite: false);
        SetStatus($"Scenario '{newScenarioId}' added. Configure routes and options, then apply.");
    }

    private void RemoveSelectedScenario()
    {
        if (SelectedScenario is null)
        {
            return;
        }

        var scenarioId = SelectedScenario.ScenarioId;
        var index = FindScenarioIndex(scenarioId);
        if (index >= 0)
        {
            _scenarioDefinitions.RemoveAt(index);
        }

        _scenarios.Remove(SelectedScenario);
        SelectedScenario = _scenarios.FirstOrDefault();
        SetStatus($"Scenario '{scenarioId}' removed.");
    }

    private void UseConfigurationDefaults()
    {
        _resetToDefaults();
        SetStatus("Reverted to configuration defaults.");
    }

    private void ApplySelectedScenario()
    {
        var scenario = CaptureScenarioFromSelection(out var errorMessage);
        if (scenario is null)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                SetError(errorMessage);
            }

            return;
        }

        _applyScenario(scenario);
        SetStatus($"Scenario '{scenario.ScenarioId}' applied.");
    }

    private ScenarioDefinitionViewModel CreateScenarioViewModel(SimulationScenarioConfiguration scenario)
    {
        var routes = SimulationRouteViewModelBuilder
            .BuildRoutes(_configuration, scenario.Routes)
            .Select(route => new SimulationStreamRouteViewModel(
                route.Stream,
                route.SelectedSource,
                route.SelectedMode,
                route.AvailableSources,
                route.AvailableModes))
            .ToList();

        return new ScenarioDefinitionViewModel(scenario, routes);
    }

    private void UpsertScenario(SimulationScenarioConfiguration scenario, bool select = true, bool overwrite = true)
    {
        var existingIndex = FindScenarioIndex(scenario.ScenarioId);
        ScenarioDefinitionViewModel? viewModel = null;

        if (existingIndex >= 0)
        {
            if (overwrite)
            {
                _scenarioDefinitions[existingIndex] = scenario;
            }

            viewModel = _scenarios.FirstOrDefault(vm => vm.ScenarioId.Equals(scenario.ScenarioId, StringComparison.OrdinalIgnoreCase));
            if (viewModel is not null)
            {
                viewModel.UpdateFromConfiguration(
                    scenario,
                    SimulationRouteViewModelBuilder
                        .BuildRoutes(_configuration, scenario.Routes)
                        .Select(route => new SimulationStreamRouteViewModel(
                            route.Stream,
                            route.SelectedSource,
                            route.SelectedMode,
                            route.AvailableSources,
                            route.AvailableModes)));
            }
        }
        else
        {
            _scenarioDefinitions.Add(scenario);
            viewModel = CreateScenarioViewModel(scenario);
            _scenarios.Add(viewModel);
        }

        if (select && viewModel is not null)
        {
            SelectedScenario = viewModel;
        }
    }

    private SimulationScenarioConfiguration? CaptureScenarioFromSelection(out string? errorMessage)
    {
        errorMessage = null;

        if (SelectedScenario is null)
        {
            errorMessage = "Select a scenario before applying or exporting.";
            return null;
        }

        try
        {
            var scenario = SelectedScenario.ToConfiguration();
            UpsertScenario(scenario);
            return scenario;
        }
        catch (InvalidOperationException ex)
        {
            errorMessage = ex.Message;
            return null;
        }
    }

    private int FindScenarioIndex(string scenarioId)
    {
        for (var i = 0; i < _scenarioDefinitions.Count; i++)
        {
            if (_scenarioDefinitions[i].ScenarioId.Equals(scenarioId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private string CreateUniqueScenarioId()
    {
        var suffix = _scenarioDefinitions.Count + 1;
        string candidate;

        do
        {
            candidate = $"scenario-{suffix}";
            suffix++;
        }
        while (_scenarioDefinitions.Any(s => s.ScenarioId.Equals(candidate, StringComparison.OrdinalIgnoreCase)));

        return candidate;
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        HasError = false;
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        HasError = true;
    }

    private static string SerializeScenario(SimulationScenarioConfiguration scenario)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("scenarioId", scenario.ScenarioId);
        if (!string.IsNullOrWhiteSpace(scenario.Description))
        {
            writer.WriteString("description", scenario.Description);
        }

        writer.WritePropertyName("routes");
        writer.WriteStartArray();
        foreach (var route in scenario.Routes)
        {
            writer.WriteStartObject();
            writer.WriteString("stream", route.Stream);
            writer.WriteString("source", route.Source);
            writer.WriteString("mode", route.Mode);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        if (scenario.Options is not null && (scenario.Options.Seed.HasValue || scenario.Options.TimeScale.HasValue))
        {
            writer.WritePropertyName("options");
            writer.WriteStartObject();
            if (scenario.Options.Seed.HasValue)
            {
                writer.WriteNumber("seed", scenario.Options.Seed.Value);
            }

            if (scenario.Options.TimeScale.HasValue)
            {
                writer.WriteNumber("timeScale", scenario.Options.TimeScale.Value);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static SimulationScenarioConfiguration ParseScenario(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Scenario JSON must be an object.");
        }

        var scenarioId = ReadRequiredString(root, "scenarioId");
        var description = root.TryGetProperty("description", out var descriptionElement) && descriptionElement.ValueKind == JsonValueKind.String
            ? descriptionElement.GetString()
            : null;

        if (!root.TryGetProperty("routes", out var routesElement) || routesElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Scenario JSON must include a routes array.");
        }

        var routes = new List<SimulationRouteConfiguration>();
        foreach (var routeElement in routesElement.EnumerateArray())
        {
            if (routeElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("Scenario routes must be objects.");
            }

            var stream = ReadRequiredString(routeElement, "stream");
            var source = ReadRequiredString(routeElement, "source");
            var mode = routeElement.TryGetProperty("mode", out var modeElement) && modeElement.ValueKind == JsonValueKind.String
                ? modeElement.GetString() ?? "simulation"
                : "simulation";

            routes.Add(new SimulationRouteConfiguration(stream, source, mode));
        }

        if (routes.Count == 0)
        {
            throw new InvalidOperationException("Scenario routes must contain at least one entry.");
        }

        SimulationOptionsConfiguration? options = null;
        if (root.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Object)
        {
            int? seed = null;
            double? timeScale = null;

            if (optionsElement.TryGetProperty("seed", out var seedElement))
            {
                if (seedElement.ValueKind != JsonValueKind.Number || !seedElement.TryGetInt32(out var seedValue))
                {
                    throw new InvalidOperationException("Scenario option 'seed' must be an integer.");
                }

                seed = seedValue;
            }

            if (optionsElement.TryGetProperty("timeScale", out var timeScaleElement))
            {
                if (timeScaleElement.ValueKind != JsonValueKind.Number)
                {
                    throw new InvalidOperationException("Scenario option 'timeScale' must be numeric.");
                }

                timeScale = timeScaleElement.GetDouble();
            }

            options = seed is null && timeScale is null ? null : new SimulationOptionsConfiguration(seed, timeScale);
        }

        return new SimulationScenarioConfiguration(scenarioId, description, routes, options);
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Scenario JSON must include a string '{propertyName}'.");
        }

        var result = value.GetString();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException($"Scenario property '{propertyName}' cannot be empty.");
        }

        return result;
    }
}

/// <summary>
/// Represents an editable scenario definition.
/// </summary>
public sealed class ScenarioDefinitionViewModel : ObservableObject
{
    private string _scenarioId;
    private string _description;
    private readonly ObservableCollection<SimulationStreamRouteViewModel> _routes;

    public ScenarioDefinitionViewModel(
        SimulationScenarioConfiguration scenario,
        IEnumerable<SimulationStreamRouteViewModel> routes)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(routes);

        _scenarioId = scenario.ScenarioId;
        _description = scenario.Description ?? string.Empty;
        _routes = new ObservableCollection<SimulationStreamRouteViewModel>(routes);
        Options = new ScenarioOptionsEditorViewModel();
        Options.LoadFromConfiguration(scenario.Options);
    }

    /// <summary>Gets or sets the scenario identifier.</summary>
    public string ScenarioId
    {
        get => _scenarioId;
        set
        {
            if (SetProperty(ref _scenarioId, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    /// <summary>Gets or sets the scenario description.</summary>
    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    /// <summary>Gets a friendly display name for the scenario list.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Description)
        ? ScenarioId
        : $"{ScenarioId} — {Description}";

    /// <summary>Gets the editable routes associated with the scenario.</summary>
    public ObservableCollection<SimulationStreamRouteViewModel> Routes => _routes;

    /// <summary>Gets the options editor associated with the scenario.</summary>
    public ScenarioOptionsEditorViewModel Options { get; }

    /// <summary>
    /// Converts the current editor state into a configuration object suitable for serialization.
    /// </summary>
    public SimulationScenarioConfiguration ToConfiguration()
    {
        if (string.IsNullOrWhiteSpace(ScenarioId))
        {
            throw new InvalidOperationException("Scenario identifier cannot be empty.");
        }

        if (_routes.Count == 0)
        {
            throw new InvalidOperationException("Scenarios must define at least one routed stream.");
        }

        var routeConfigurations = _routes
            .Select(route => route.ToConfiguration())
            .ToArray();

        var options = Options.ToConfiguration();
        var description = string.IsNullOrWhiteSpace(Description) ? null : Description;
        return new SimulationScenarioConfiguration(ScenarioId, description, routeConfigurations, options);
    }

    /// <summary>
    /// Updates the editor to reflect a new configuration snapshot.
    /// </summary>
    public void UpdateFromConfiguration(
        SimulationScenarioConfiguration scenario,
        IEnumerable<SimulationStreamRouteViewModel> routes)
    {
        ScenarioId = scenario.ScenarioId;
        Description = scenario.Description ?? string.Empty;
        Options.LoadFromConfiguration(scenario.Options);

        _routes.Clear();
        foreach (var route in routes)
        {
            _routes.Add(route);
        }
    }
}

/// <summary>
/// Supports editing the seed and time scale options for a scenario.
/// </summary>
public sealed class ScenarioOptionsEditorViewModel : ObservableObject
{
    private string _seedText = string.Empty;
    private string _timeScaleText = string.Empty;

    /// <summary>Gets or sets the seed text.</summary>
    public string SeedText
    {
        get => _seedText;
        set => SetProperty(ref _seedText, value ?? string.Empty);
    }

    /// <summary>Gets or sets the time scale text.</summary>
    public string TimeScaleText
    {
        get => _timeScaleText;
        set => SetProperty(ref _timeScaleText, value ?? string.Empty);
    }

    /// <summary>
    /// Loads the editor fields from the provided configuration snapshot.
    /// </summary>
    /// <param name="options">Options to hydrate the editor with.</param>
    public void LoadFromConfiguration(SimulationOptionsConfiguration? options)
    {
        if (options is null)
        {
            SeedText = string.Empty;
            TimeScaleText = string.Empty;
            return;
        }

        SeedText = options.Seed.HasValue ? options.Seed.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        TimeScaleText = options.TimeScale.HasValue ? options.TimeScale.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }

    /// <summary>
    /// Converts the current editor state into a <see cref="SimulationOptionsConfiguration"/>.
    /// </summary>
    public SimulationOptionsConfiguration? ToConfiguration()
    {
        int? seed = null;
        double? timeScale = null;

        if (!string.IsNullOrWhiteSpace(SeedText))
        {
            if (!int.TryParse(SeedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSeed))
            {
                throw new InvalidOperationException("Seed must be an integer.");
            }

            seed = parsedSeed;
        }

        if (!string.IsNullOrWhiteSpace(TimeScaleText))
        {
            if (!double.TryParse(TimeScaleText, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedTimeScale))
            {
                throw new InvalidOperationException("Time scale must be numeric.");
            }

            timeScale = parsedTimeScale;
        }

        return seed is null && timeScale is null
            ? null
            : new SimulationOptionsConfiguration(seed, timeScale);
    }
}
