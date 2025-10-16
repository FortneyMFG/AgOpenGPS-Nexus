using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model for the flag manager dialog.
/// </summary>
public sealed class FlagManagerDialogViewModel : ObservableObject
{
    private readonly ObservableCollection<FlagEntryViewModel> _flags;
    private readonly ReadOnlyObservableCollection<FlagEntryViewModel> _flagsView;
    private readonly HashSet<string> _categories;
    private FlagEntryViewModel? _selectedFlag;
    private string? _selectedCategory;
    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;
    private int _nextSequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlagManagerDialogViewModel"/> class.
    /// </summary>
    public FlagManagerDialogViewModel(IEnumerable<FlagEntryViewModel> flags)
    {
        _flags = new ObservableCollection<FlagEntryViewModel>((flags ?? Array.Empty<FlagEntryViewModel>()).ToList());
        _flagsView = new ReadOnlyObservableCollection<FlagEntryViewModel>(_flags);
        _categories = new HashSet<string>(_flags.Select(flag => flag.Category), StringComparer.OrdinalIgnoreCase);
        _nextSequence = _flags.Count == 0 ? 1 : _flags.Max(flag => flag.Sequence) + 1;

        AddFlagCommand = new DelegateCommand(_ => AddFlag());
        DeleteSelectedCommand = new DelegateCommand(_ => DeleteSelected(), _ => SelectedFlag is not null);
        JumpToSelectedCommand = new DelegateCommand(_ => JumpToSelected(), _ => SelectedFlag is not null);
        ClearFilterCommand = new DelegateCommand(_ => ClearFilter(), _ => !string.IsNullOrWhiteSpace(SelectedCategory) || !string.IsNullOrWhiteSpace(SearchText));
    }

    /// <summary>Gets the list of known categories.</summary>
    public IReadOnlyCollection<string> Categories => _categories.ToList();

    /// <summary>Gets the underlying flag collection.</summary>
    public ReadOnlyObservableCollection<FlagEntryViewModel> Flags => _flagsView;

    /// <summary>Gets the filtered view of flags respecting the current filter criteria.</summary>
    public IReadOnlyList<FlagEntryViewModel> FilteredFlags => _flags
        .Where(MatchesCategory)
        .Where(MatchesSearch)
        .OrderByDescending(flag => flag.IsActive)
        .ThenBy(flag => flag.Sequence)
        .ToList();

    /// <summary>Gets or sets the selected flag.</summary>
    public FlagEntryViewModel? SelectedFlag
    {
        get => _selectedFlag;
        set
        {
            if (SetProperty(ref _selectedFlag, value))
            {
                RaiseCommandStates();
            }
        }
    }

    /// <summary>Gets or sets the selected category filter.</summary>
    public string? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                OnPropertyChanged(nameof(FilteredFlags));
                RaiseCommandStates();
            }
        }
    }

    /// <summary>Gets or sets the search text applied to flag labels and notes.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(FilteredFlags));
                RaiseCommandStates();
            }
        }
    }

    /// <summary>Gets the status message describing the last operation.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets the command that adds a new flag.</summary>
    public ICommand AddFlagCommand { get; }

    /// <summary>Gets the command that deletes the selected flag.</summary>
    public ICommand DeleteSelectedCommand { get; }

    /// <summary>Gets the command that focuses the map on the selected flag.</summary>
    public ICommand JumpToSelectedCommand { get; }

    /// <summary>Gets the command that clears active filters.</summary>
    public ICommand ClearFilterCommand { get; }

    /// <summary>
    /// Creates a sample view-model instance populated with representative data.
    /// </summary>
    public static FlagManagerDialogViewModel CreateSample()
    {
        var samples = new[]
        {
            new FlagEntryViewModel(1, "North headland", "Boundary", Color.FromRgb(255, 99, 71), new(51.21452, -102.4563), "Check drainage tile outlet."),
            new FlagEntryViewModel(2, "Rock patch", "Obstacles", Color.FromRgb(255, 215, 0), new(51.2141, -102.4548), "Large stone cluster; clear before harvest."),
            new FlagEntryViewModel(3, "Soil sample", "Agronomy", Color.FromRgb(72, 209, 204), new(51.21502, -102.4552), "Sample ID AGR-482A"),
            new FlagEntryViewModel(4, "Wet spot", "Boundary", Color.FromRgb(70, 130, 180), new(51.21382, -102.4576), "Monitor after rainfall events."),
        };

        samples[0].IsActive = true;
        samples[1].IsActive = true;

        return new FlagManagerDialogViewModel(samples)
        {
            SelectedFlag = samples[0],
            StatusMessage = "Loaded 4 flags from map metadata.",
        };
    }

    private void AddFlag()
    {
        var sequence = _nextSequence++;
        var category = string.IsNullOrWhiteSpace(SelectedCategory) ? "General" : SelectedCategory!;
        _categories.Add(category);
        OnPropertyChanged(nameof(Categories));

        var newFlag = new FlagEntryViewModel(
            sequence,
            $"Waypoint {sequence.ToString(CultureInfo.InvariantCulture)}",
            category,
            Color.FromRgb(0x6A, 0x5A, 0xCD),
            new GeoCoordinate(51.2145 + sequence * 0.00012, -102.456 + sequence * 0.00008),
            "New flag created from dialog.")
        {
            IsActive = true,
        };

        _flags.Add(newFlag);
        SelectedFlag = newFlag;
        StatusMessage = $"Added flag '{newFlag.Label}'.";
        OnPropertyChanged(nameof(FilteredFlags));
    }

    private void DeleteSelected()
    {
        if (SelectedFlag is null)
        {
            return;
        }

        var removed = SelectedFlag;
        _flags.Remove(removed);
        StatusMessage = $"Removed flag '{removed.Label}'.";
        SelectedFlag = _flags.FirstOrDefault();
        OnPropertyChanged(nameof(FilteredFlags));
    }

    private void JumpToSelected()
    {
        if (SelectedFlag is null)
        {
            return;
        }

        SelectedFlag.LastVisited = DateTimeOffset.UtcNow;
        StatusMessage = $"Centered map on '{SelectedFlag.Label}'.";
        SelectedFlag.MarkVisited();
    }

    private void ClearFilter()
    {
        SelectedCategory = null;
        SearchText = string.Empty;
        StatusMessage = "Cleared filters.";
        OnPropertyChanged(nameof(FilteredFlags));
    }

    private bool MatchesCategory(FlagEntryViewModel flag)
    {
        if (string.IsNullOrWhiteSpace(SelectedCategory))
        {
            return true;
        }

        return string.Equals(flag.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesSearch(FlagEntryViewModel flag)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return flag.Label.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(flag.Notes) && flag.Notes.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
    }

    private void RaiseCommandStates()
    {
        if (DeleteSelectedCommand is DelegateCommand delete)
        {
            delete.RaiseCanExecuteChanged();
        }

        if (JumpToSelectedCommand is DelegateCommand jump)
        {
            jump.RaiseCanExecuteChanged();
        }

        if (ClearFilterCommand is DelegateCommand clear)
        {
            clear.RaiseCanExecuteChanged();
        }
    }
}

/// <summary>
/// Represents an individual flag entry.
/// </summary>
public sealed class FlagEntryViewModel : ObservableObject
{
    private bool _isActive;
    private string _notes;
    private DateTimeOffset? _lastVisited;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlagEntryViewModel"/> class.
    /// </summary>
    public FlagEntryViewModel(int sequence, string label, string category, Color color, GeoCoordinate coordinate, string notes)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        Sequence = sequence;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Color = color;
        Coordinate = coordinate;
        _notes = notes ?? string.Empty;
    }

    /// <summary>Gets the sequence number for the flag.</summary>
    public int Sequence { get; }

    /// <summary>Gets the display label.</summary>
    public string Label { get; }

    /// <summary>Gets the category.</summary>
    public string Category { get; }

    /// <summary>Gets the colour associated with the flag.</summary>
    public Color Color { get; }

    /// <summary>Gets the geographic coordinate.</summary>
    public GeoCoordinate Coordinate { get; }

    /// <summary>Gets or sets the notes attached to the flag.</summary>
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value ?? string.Empty);
    }

    /// <summary>Gets or sets a value indicating whether the flag is active.</summary>
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    /// <summary>Gets or sets the last time the flag was visited.</summary>
    public DateTimeOffset? LastVisited
    {
        get => _lastVisited;
        set => SetProperty(ref _lastVisited, value);
    }

    /// <summary>
    /// Marks the flag as having been visited.
    /// </summary>
    public void MarkVisited()
    {
        LastVisited = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Represents a geographic coordinate pair.
/// </summary>
public readonly record struct GeoCoordinate(double Latitude, double Longitude)
{
    /// <summary>Converts the coordinate to a display string.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Latitude:F5}, {Longitude:F5}");
}
