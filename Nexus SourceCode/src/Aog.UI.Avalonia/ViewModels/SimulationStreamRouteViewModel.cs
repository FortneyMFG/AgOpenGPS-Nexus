using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model describing a routable simulation stream and its selectable providers.
/// </summary>
public sealed class SimulationStreamRouteViewModel : ObservableObject
{
    private readonly IReadOnlyList<string> _availableSources;
    private readonly IReadOnlyList<string> _availableModes;
    private string _selectedSource;
    private string _selectedMode;

    public SimulationStreamRouteViewModel(
        string stream,
        string selectedSource,
        string mode,
        IReadOnlyList<string> availableSources,
        IReadOnlyList<string> availableModes)
    {
        if (string.IsNullOrWhiteSpace(stream))
        {
            throw new ArgumentException("Stream must be provided.", nameof(stream));
        }

        if (availableSources is null || availableSources.Count == 0)
        {
            throw new ArgumentException("At least one source must be supplied.", nameof(availableSources));
        }

        if (availableModes is null || availableModes.Count == 0)
        {
            throw new ArgumentException("At least one mode must be supplied.", nameof(availableModes));
        }

        Stream = stream;
        _availableSources = availableSources;
        _availableModes = availableModes;
        _selectedSource = ContainsIgnoreCase(availableSources, selectedSource)
            ? selectedSource
            : availableSources[0];
        _selectedMode = ContainsIgnoreCase(availableModes, mode)
            ? mode
            : availableModes[0];
    }

    /// <summary>
    /// Gets the stream identifier being routed.
    /// </summary>
    public string Stream { get; }

    /// <summary>
    /// Gets the known providers that can satisfy the stream.
    /// </summary>
    public IReadOnlyList<string> AvailableSources => _availableSources;

    /// <summary>
    /// Gets the supported routing modes.
    /// </summary>
    public IReadOnlyList<string> AvailableModes => _availableModes;

    /// <summary>
    /// Gets or sets the currently selected source provider for the stream.
    /// </summary>
    public string SelectedSource
    {
        get => _selectedSource;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || value.Equals(_selectedSource, StringComparison.Ordinal))
            {
                return;
            }

            if (!ContainsIgnoreCase(_availableSources, value))
            {
                return;
            }

            SetProperty(ref _selectedSource, value);
        }
    }

    /// <summary>
    /// Gets or sets the selected routing mode for the stream.
    /// </summary>
    public string SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || value.Equals(_selectedMode, StringComparison.Ordinal))
            {
                return;
            }

            if (!ContainsIgnoreCase(_availableModes, value))
            {
                return;
            }

            SetProperty(ref _selectedMode, value);
        }
    }

    private static bool ContainsIgnoreCase(IEnumerable<string> source, string value)
    {
        return source.Any(candidate => candidate.Equals(value, StringComparison.OrdinalIgnoreCase));
    }
}
