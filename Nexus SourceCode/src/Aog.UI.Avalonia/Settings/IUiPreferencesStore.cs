namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Defines persistence operations for UI preferences.
/// </summary>
public interface IUiPreferencesStore
{
    /// <summary>Loads the previously saved preferences or defaults when no persisted state exists.</summary>
    UiPreferences Load();

    /// <summary>Persists the provided preferences.</summary>
    /// <param name="preferences">The preferences to persist.</param>
    void Save(UiPreferences preferences);
}
