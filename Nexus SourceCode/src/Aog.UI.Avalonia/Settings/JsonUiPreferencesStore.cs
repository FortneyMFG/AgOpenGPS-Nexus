using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Persists UI preferences to a JSON file located under the user's application data folder.
/// </summary>
public sealed class JsonUiPreferencesStore : IUiPreferencesStore
{
    private const string SettingsFileName = "ui-preferences.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private readonly ILogger<JsonUiPreferencesStore> _logger;
    private readonly string _settingsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonUiPreferencesStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public JsonUiPreferencesStore(ILogger<JsonUiPreferencesStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "AgOpenGPS", "Nexus");
        _settingsPath = Path.Combine(directory, SettingsFileName);
    }

    /// <inheritdoc />
    public UiPreferences Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogInformation("UI preferences file not found at {Path}; using defaults.", _settingsPath);
                return new UiPreferences();
            }

            using var stream = File.OpenRead(_settingsPath);
            var loaded = JsonSerializer.Deserialize<UiPreferences>(stream, SerializerOptions);
            if (loaded is null)
            {
                _logger.LogWarning("UI preferences file at {Path} was empty; using defaults.", _settingsPath);
                return new UiPreferences();
            }

            loaded.Window ??= new WindowPlacement();
            return loaded;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to load UI preferences from {Path}; using defaults.", _settingsPath);
            return new UiPreferences();
        }
    }

    /// <inheritdoc />
    public void Save(UiPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(_settingsPath);
        JsonSerializer.Serialize(stream, preferences, SerializerOptions);
        _logger.LogInformation("UI preferences saved to {Path}.", _settingsPath);
    }
}
