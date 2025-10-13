using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Persists connection settings to a JSON file located in the user's application data folder.
/// </summary>
public class JsonConnectionSettingsStore : IConnectionSettingsStore
{
    private const string SettingsFileName = "connection-settings.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private readonly ILogger<JsonConnectionSettingsStore> _logger;
    private readonly string _settingsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonConnectionSettingsStore"/> class.
    /// </summary>
    /// <param name="logger">The logger used for diagnostic messages.</param>
    public JsonConnectionSettingsStore(ILogger<JsonConnectionSettingsStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "AgOpenGPS", "Nexus");
        _settingsPath = Path.Combine(directory, SettingsFileName);
    }

    /// <inheritdoc />
    public ConnectionSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogInformation("Connection settings file not found at {Path}; using defaults.", _settingsPath);
                return new ConnectionSettings();
            }

            using var stream = File.OpenRead(_settingsPath);
            var loaded = JsonSerializer.Deserialize<ConnectionSettings>(stream, SerializerOptions);
            if (loaded is null)
            {
                _logger.LogWarning("Connection settings file at {Path} was empty; using defaults.", _settingsPath);
                return new ConnectionSettings();
            }

            return loaded;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to load connection settings from {Path}; using defaults.", _settingsPath);
            return new ConnectionSettings();
        }
    }

    /// <inheritdoc />
    public void Save(ConnectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(_settingsPath);
        JsonSerializer.Serialize(stream, settings, SerializerOptions);
        _logger.LogInformation("Connection settings saved to {Path}.", _settingsPath);
    }
}
