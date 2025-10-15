using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model that allows operators to select multiple fields for a job session per ADR-043.
/// </summary>
public sealed class MultiFieldJobSelectorViewModel : ObservableObject
{
    private readonly ObservableCollection<JobFieldSelectionViewModel> _fields;
    private readonly DelegateCommand _selectAllCommand;
    private readonly DelegateCommand _clearSelectionCommand;
    private readonly DelegateCommand _invertSelectionCommand;
    private int _selectedFieldCount;
    private double _selectedAreaHectares;
    private double _averageCoveragePercent;
    private string _selectionSummary;
    private string _selectedFieldsDisplay;
    private string _statusMessage = "Select fields to mount.";
    private bool _hasError;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiFieldJobSelectorViewModel"/> class.
    /// </summary>
    /// <param name="fields">Field definitions surfaced from the job manifest.</param>
    public MultiFieldJobSelectorViewModel(IEnumerable<JobFieldDefinition> fields)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        var definitions = fields.ToList();
        if (definitions.Count == 0)
        {
            throw new ArgumentException("At least one field is required to mount a job.", nameof(fields));
        }

        _fields = new ObservableCollection<JobFieldSelectionViewModel>(
            definitions.Select(CreateFieldViewModel));

        foreach (var field in _fields)
        {
            field.PropertyChanged += OnFieldPropertyChanged;
        }

        _selectAllCommand = new DelegateCommand(_ => SelectAll(), _ => SelectedFieldCount < TotalFieldCount);
        _clearSelectionCommand = new DelegateCommand(_ => ClearSelection(), _ => SelectedFieldCount > 0);
        _invertSelectionCommand = new DelegateCommand(_ => InvertSelection(), _ => TotalFieldCount > 1);

        TotalAreaHectares = _fields.Sum(field => field.AreaHectares);
        _selectionSummary = string.Empty;
        _selectedFieldsDisplay = string.Empty;

        UpdateSelectionAggregates();
    }

    /// <summary>Gets the total number of fields available for the job.</summary>
    public int TotalFieldCount => _fields.Count;

    /// <summary>Gets the total aggregated area of the job fields in hectares.</summary>
    public double TotalAreaHectares { get; }

    /// <summary>Gets the fields surfaced in the selector.</summary>
    public IReadOnlyList<JobFieldSelectionViewModel> Fields => _fields;

    /// <summary>Gets the count of fields currently selected for the session.</summary>
    public int SelectedFieldCount
    {
        get => _selectedFieldCount;
        private set
        {
            if (SetProperty(ref _selectedFieldCount, value))
            {
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }

    /// <summary>Gets the aggregated area for the selected fields.</summary>
    public double SelectedAreaHectares
    {
        get => _selectedAreaHectares;
        private set => SetProperty(ref _selectedAreaHectares, value);
    }

    /// <summary>Gets the average coverage percentage across the selected fields.</summary>
    public double AverageCoveragePercent
    {
        get => _averageCoveragePercent;
        private set
        {
            if (SetProperty(ref _averageCoveragePercent, value))
            {
                OnPropertyChanged(nameof(AverageCoverageDisplay));
            }
        }
    }

    /// <summary>Gets a formatted string describing the average coverage across selected fields.</summary>
    public string AverageCoverageDisplay => SelectedFieldCount == 0
        ? "—"
        : $"{AverageCoveragePercent:0.#}% covered";

    /// <summary>Gets a summary sentence describing the current selection.</summary>
    public string SelectionSummary
    {
        get => _selectionSummary;
        private set => SetProperty(ref _selectionSummary, value);
    }

    /// <summary>Gets a display string enumerating the selected fields.</summary>
    public string SelectedFieldsDisplay
    {
        get => _selectedFieldsDisplay;
        private set => SetProperty(ref _selectedFieldsDisplay, value);
    }

    /// <summary>Gets a value indicating whether at least one field is selected.</summary>
    public bool HasSelection => SelectedFieldCount > 0;

    /// <summary>Gets the command that marks all fields as selected.</summary>
    public DelegateCommand SelectAllCommand => _selectAllCommand;

    /// <summary>Gets the command that clears the current selection.</summary>
    public DelegateCommand ClearSelectionCommand => _clearSelectionCommand;

    /// <summary>Gets the command that toggles each field's selection state.</summary>
    public DelegateCommand InvertSelectionCommand => _invertSelectionCommand;

    /// <summary>Gets the status message presented to the operator.</summary>
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

    /// <summary>
    /// Applies an explicit selection state using the provided field identifiers.
    /// </summary>
    public void ApplySelection(IEnumerable<string> selectedFieldIds)
    {
        if (selectedFieldIds is null)
        {
            throw new ArgumentNullException(nameof(selectedFieldIds));
        }

        var target = new HashSet<string>(selectedFieldIds, StringComparer.OrdinalIgnoreCase);

        foreach (var field in _fields)
        {
            field.IsSelected = target.Contains(field.FieldId);
        }

        UpdateSelectionAggregates();
    }

    /// <summary>
    /// Returns lightweight snapshots representing the currently selected fields.
    /// </summary>
    public IReadOnlyList<JobFieldSelectionSnapshot> CaptureSelectedFields()
    {
        return _fields
            .Where(field => field.IsSelected)
            .Select(field => field.CreateSnapshot())
            .ToList();
    }

    /// <summary>
    /// Updates the status banner with an informational message.
    /// </summary>
    public void SetStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        StatusMessage = message;
        HasError = false;
    }

    /// <summary>
    /// Updates the status banner with an error message.
    /// </summary>
    public void SetError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        StatusMessage = message;
        HasError = true;
    }

    private void SelectAll()
    {
        foreach (var field in _fields)
        {
            field.IsSelected = true;
        }

        UpdateSelectionAggregates();
    }

    private void ClearSelection()
    {
        foreach (var field in _fields)
        {
            field.IsSelected = false;
        }

        UpdateSelectionAggregates();
    }

    private void InvertSelection()
    {
        foreach (var field in _fields)
        {
            field.IsSelected = !field.IsSelected;
        }

        UpdateSelectionAggregates();
    }

    private void UpdateSelectionAggregates()
    {
        var selected = _fields.Where(field => field.IsSelected).ToList();

        SelectedFieldCount = selected.Count;
        SelectedAreaHectares = selected.Sum(field => field.AreaHectares);
        AverageCoveragePercent = selected.Count == 0
            ? 0
            : selected.Average(field => field.CoveragePercent);

        SelectionSummary = BuildSelectionSummary();
        SelectedFieldsDisplay = BuildSelectedFieldsDisplay(selected);

        RefreshCommandStates();
    }

    private string BuildSelectionSummary()
    {
        var fieldPortion = SelectedFieldCount switch
        {
            0 => "No fields selected",
            1 => "1 of 1 field selected",
            _ when SelectedFieldCount == TotalFieldCount => $"All {TotalFieldCount} fields selected",
            _ => $"{SelectedFieldCount} of {TotalFieldCount} fields selected",
        };

        var areaPortion = SelectedFieldCount == 0
            ? "—"
            : $"{SelectedAreaHectares:0.##} ha";

        return $"{fieldPortion} · {areaPortion}";
    }

    private static string BuildSelectedFieldsDisplay(IReadOnlyList<JobFieldSelectionViewModel> selected)
    {
        if (selected.Count == 0)
        {
            return "No fields selected";
        }

        if (selected.Count == 1)
        {
            return selected[0].Name;
        }

        if (selected.Count == 2)
        {
            return string.Join(" & ", selected.Select(field => field.Name));
        }

        return $"{selected[0].Name}, {selected[1].Name} + {selected.Count - 2} more";
    }

    private JobFieldSelectionViewModel CreateFieldViewModel(JobFieldDefinition definition)
    {
        var viewModel = new JobFieldSelectionViewModel(
            definition.FieldId,
            definition.Name,
            definition.AreaHectares,
            definition.CoveragePercent,
            definition.IsInitiallySelected);

        return viewModel;
    }

    private void RefreshCommandStates()
    {
        _selectAllCommand.RaiseCanExecuteChanged();
        _clearSelectionCommand.RaiseCanExecuteChanged();
        _invertSelectionCommand.RaiseCanExecuteChanged();
    }

    private void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(JobFieldSelectionViewModel.IsSelected))
        {
            UpdateSelectionAggregates();
        }
        else if (e.PropertyName == nameof(JobFieldSelectionViewModel.CoveragePercent))
        {
            // Coverage updates change the aggregate even when the selection is unchanged.
            AverageCoveragePercent = _fields.Where(field => field.IsSelected).ToList() is { Count: > 0 } selected
                ? selected.Average(field => field.CoveragePercent)
                : 0;
        }
    }
}

/// <summary>Represents field metadata surfaced for selection in a multi-field job.</summary>
public sealed class JobFieldSelectionViewModel : ObservableObject
{
    private bool _isSelected;
    private double _coveragePercent;
    private readonly DelegateCommand _toggleSelectionCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobFieldSelectionViewModel"/> class.
    /// </summary>
    public JobFieldSelectionViewModel(
        string fieldId,
        string name,
        double areaHectares,
        double coveragePercent,
        bool isSelected)
    {
        ArgumentException.ThrowIfNullOrEmpty(fieldId);
        ArgumentException.ThrowIfNullOrEmpty(name);

        FieldId = fieldId;
        Name = name;
        AreaHectares = areaHectares;
        _isSelected = isSelected;
        _toggleSelectionCommand = new DelegateCommand(_ => ToggleSelection());
        CoveragePercent = coveragePercent;
    }

    /// <summary>Gets the field identifier.</summary>
    public string FieldId { get; }

    /// <summary>Gets the display name for the field.</summary>
    public string Name { get; }

    /// <summary>Gets the field area in hectares.</summary>
    public double AreaHectares { get; }

    /// <summary>Gets or sets a value indicating whether the field is selected.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(SelectionStateDisplay));
            }
        }
    }

    /// <summary>Gets or sets the coverage percentage for the field.</summary>
    public double CoveragePercent
    {
        get => _coveragePercent;
        set
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (SetProperty(ref _coveragePercent, clamped))
            {
                OnPropertyChanged(nameof(CoverageDisplay));
            }
        }
    }

    /// <summary>Gets a formatted area display string.</summary>
    public string AreaDisplay => $"{AreaHectares:0.##} ha";

    /// <summary>Gets a formatted coverage display string.</summary>
    public string CoverageDisplay => $"{CoveragePercent:0.#}% covered";

    /// <summary>Gets a display string describing whether the field is mounted.</summary>
    public string SelectionStateDisplay => IsSelected ? "Mounted" : "Available";

    /// <summary>Gets a command that toggles the selection state.</summary>
    public DelegateCommand ToggleSelectionCommand => _toggleSelectionCommand;

    /// <summary>
    /// Creates a snapshot of the current selection state for session journaling.
    /// </summary>
    public JobFieldSelectionSnapshot CreateSnapshot()
    {
        return new JobFieldSelectionSnapshot(FieldId, Name, AreaHectares, CoveragePercent);
    }

    private void ToggleSelection()
    {
        IsSelected = !IsSelected;
    }
}

/// <summary>Describes a field made available for session selection.</summary>
public sealed record JobFieldDefinition(
    string FieldId,
    string Name,
    double AreaHectares,
    double CoveragePercent,
    bool IsInitiallySelected = true);

/// <summary>Lightweight snapshot of a field selection captured when a session starts.</summary>
public sealed record JobFieldSelectionSnapshot(
    string FieldId,
    string Name,
    double AreaHectares,
    double CoveragePercent);

