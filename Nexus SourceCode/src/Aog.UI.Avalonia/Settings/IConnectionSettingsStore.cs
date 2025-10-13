namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Provides persistence for connection settings configured from the UI shell.
/// </summary>
public interface IConnectionSettingsStore
{
    /// <summary>
    /// Loads the persisted settings or returns defaults when none exist.
    /// </summary>
    /// <returns>The stored connection settings.</returns>
    ConnectionSettings Load();

    /// <summary>
    /// Persists the provided connection settings.
    /// </summary>
    /// <param name="settings">The settings to persist.</param>
    void Save(ConnectionSettings settings);
}
