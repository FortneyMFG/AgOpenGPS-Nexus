using System;

namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Coordinates access to the UI preference store with basic caching.
/// </summary>
public sealed class UiPreferencesService : IUiPreferencesService
{
    private readonly IUiPreferencesStore _store;
    private readonly object _gate = new();
    private UiPreferences _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiPreferencesService"/> class.
    /// </summary>
    /// <param name="store">The backing store used to persist preferences.</param>
    public UiPreferencesService(IUiPreferencesStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _current = store.Load();
    }

    /// <inheritdoc />
    public UiPreferences GetPreferences()
    {
        lock (_gate)
        {
            return _current.Clone();
        }
    }

    /// <inheritdoc />
    public void UpdateTheme(UiTheme theme)
    {
        lock (_gate)
        {
            if (_current.Theme == theme)
            {
                return;
            }

            _current.Theme = theme;
            Persist();
        }
    }

    /// <inheritdoc />
    public void UpdateWindowPlacement(WindowPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        lock (_gate)
        {
            _current.Window = placement.Clone();
            Persist();
        }
    }

    /// <inheritdoc />
    public void UpdateTelemetryOptIn(bool isOptedIn)
    {
        lock (_gate)
        {
            if (_current.TelemetryOptIn == isOptedIn)
            {
                return;
            }

            _current.TelemetryOptIn = isOptedIn;
            Persist();
        }
    }

    private void Persist()
    {
        _store.Save(_current);
    }
}
