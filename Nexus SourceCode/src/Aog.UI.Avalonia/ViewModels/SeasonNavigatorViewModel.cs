using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides presentation models for navigating seasons, farms, and jobs per ADR-040.
/// </summary>
public sealed class SeasonNavigatorViewModel : ObservableObject
{
    private readonly List<SeasonSummaryViewModel> _allSeasons;
    private readonly List<SeasonSummaryViewModel> _filteredSeasons = new();
    private SeasonSummaryViewModel? _selectedSeason;
    private string _searchText = string.Empty;
    private bool _showOnlyActiveSeasons = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonNavigatorViewModel"/> class.
    /// </summary>
    /// <param name="seasons">Collection of seasons to surface in the navigator.</param>
    public SeasonNavigatorViewModel(IEnumerable<SeasonSummaryViewModel> seasons)
    {
        ArgumentNullException.ThrowIfNull(seasons);

        _allSeasons = seasons
            .OrderByDescending(season => season.StartDate)
            .ToList();

        RefreshFilteredSeasons();
        OnPropertyChanged(nameof(ShowOnlyActiveSeasons));
    }

    /// <summary>Gets a static sample navigator aligned with ADR-040 concepts.</summary>
    public static SeasonNavigatorViewModel CreateSample()
    {
        var spring2025 = new SeasonSummaryViewModel(
            id: "season:2025",
            name: "2025 Crop Year",
            startDate: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            endDate: new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero),
            notes: "Planning and execution checkpoints for spring planting and mid-season scouting.",
            createdBy: "planner.annika",
            createdAt: new DateTimeOffset(2025, 1, 2, 14, 0, 0, TimeSpan.Zero),
            lastModifiedAt: new DateTimeOffset(2025, 3, 21, 18, 30, 0, TimeSpan.Zero),
            farmNames: new[] { "South Farm", "Prairie Ridge" },
            jobs: new[]
            {
                new SeasonJobViewModel(
                    id: "job:2025-plant-corn",
                    name: "Corn planting – South Farm",
                    farmName: "South Farm",
                    fieldNames: new[] { "North 80", "Northwest 20" },
                    operationType: "Planting",
                    status: "In progress",
                    scheduledFor: new DateTimeOffset(2025, 4, 12, 0, 0, 0, TimeSpan.Zero),
                    lastWorkedAt: new DateTimeOffset(2025, 4, 13, 21, 15, 0, TimeSpan.Zero),
                    notes: "Operator resumed after rain delay; AB pass verified.",
                    isActive: true),
                new SeasonJobViewModel(
                    id: "job:2025-preseason-scout",
                    name: "Pre-season scouting sweep",
                    farmName: "Prairie Ridge",
                    fieldNames: new[] { "Ridge Block" },
                    operationType: "Scouting",
                    status: "Scheduled",
                    scheduledFor: new DateTimeOffset(2025, 4, 18, 0, 0, 0, TimeSpan.Zero),
                    lastWorkedAt: null,
                    notes: "Survey low spots for residue. Drone overlay planned.",
                    isActive: true),
                new SeasonJobViewModel(
                    id: "job:2025-variable-rate",
                    name: "Variable-rate prescription rollout",
                    farmName: "South Farm",
                    fieldNames: new[] { "Headland Strip" },
                    operationType: "Application",
                    status: "Drafting",
                    scheduledFor: null,
                    lastWorkedAt: null,
                    notes: "Waiting on soil lab upload; target map in progress.",
                    isActive: false),
            });

        var harvest2024 = new SeasonSummaryViewModel(
            id: "season:2024-harvest",
            name: "2024 Harvest",
            startDate: new DateTimeOffset(2024, 8, 15, 0, 0, 0, TimeSpan.Zero),
            endDate: new DateTimeOffset(2024, 12, 1, 0, 0, 0, TimeSpan.Zero),
            notes: "Combine telemetry and grain cart logistics captured for post-season reports.",
            createdBy: "planner.mateo",
            createdAt: new DateTimeOffset(2024, 8, 10, 9, 45, 0, TimeSpan.Zero),
            lastModifiedAt: new DateTimeOffset(2024, 11, 3, 16, 5, 0, TimeSpan.Zero),
            farmNames: new[] { "Prairie Ridge" },
            jobs: new[]
            {
                new SeasonJobViewModel(
                    id: "job:2024-soy-harvest",
                    name: "Soybean harvest",
                    farmName: "Prairie Ridge",
                    fieldNames: new[] { "South 60" },
                    operationType: "Harvest",
                    status: "Completed",
                    scheduledFor: new DateTimeOffset(2024, 9, 20, 0, 0, 0, TimeSpan.Zero),
                    lastWorkedAt: new DateTimeOffset(2024, 9, 22, 23, 40, 0, TimeSpan.Zero),
                    notes: "Moisture stabilized at 12.7%. Combine 2 telemetry attached to run.",
                    isActive: false),
                new SeasonJobViewModel(
                    id: "job:2024-corn-harvest",
                    name: "Corn harvest",
                    farmName: "Prairie Ridge",
                    fieldNames: new[] { "North Ridge" },
                    operationType: "Harvest",
                    status: "In progress",
                    scheduledFor: new DateTimeOffset(2024, 10, 1, 0, 0, 0, TimeSpan.Zero),
                    lastWorkedAt: new DateTimeOffset(2024, 10, 5, 20, 5, 0, TimeSpan.Zero),
                    notes: "Headland loads logged for logistics replay.",
                    isActive: true),
            });

        var winterPlanning = new SeasonSummaryViewModel(
            id: "season:2024-planning",
            name: "2024/2025 Winter Planning",
            startDate: new DateTimeOffset(2024, 12, 15, 0, 0, 0, TimeSpan.Zero),
            endDate: new DateTimeOffset(2025, 2, 28, 0, 0, 0, TimeSpan.Zero),
            notes: "Pre-season layout templates, equipment presets, and agronomy consultations.",
            createdBy: "planner.sana",
            createdAt: new DateTimeOffset(2024, 12, 12, 11, 20, 0, TimeSpan.Zero),
            lastModifiedAt: new DateTimeOffset(2025, 1, 6, 8, 15, 0, TimeSpan.Zero),
            farmNames: new[] { "South Farm", "High Plains" },
            jobs: new[]
            {
                new SeasonJobViewModel(
                    id: "job:2025-layout-review",
                    name: "Field layout review",
                    farmName: "High Plains",
                    fieldNames: new[] { "Eastern Pivot" },
                    operationType: "Planning",
                    status: "In review",
                    scheduledFor: null,
                    lastWorkedAt: null,
                    notes: "Overlay irrigation constraints on planting layout.",
                    isActive: false),
                new SeasonJobViewModel(
                    id: "job:2025-equipment-calibration",
                    name: "Equipment calibration prep",
                    farmName: "South Farm",
                    fieldNames: new[] { "Shop" },
                    operationType: "Preparation",
                    status: "Not started",
                    scheduledFor: new DateTimeOffset(2025, 2, 5, 0, 0, 0, TimeSpan.Zero),
                    lastWorkedAt: null,
                    notes: "Compile last year's planter configuration for comparison.",
                    isActive: false),
            });

        return new SeasonNavigatorViewModel(new[] { spring2025, harvest2024, winterPlanning });
    }

    /// <summary>Gets the seasons available in the navigator after filtering.</summary>
    public IReadOnlyList<SeasonSummaryViewModel> Seasons => _filteredSeasons;

    /// <summary>Gets or sets the selected season in the navigator.</summary>
    public SeasonSummaryViewModel? SelectedSeason
    {
        get => _selectedSeason;
        set
        {
            if (SetProperty(ref _selectedSeason, value) && value is not null && value.SelectedJob is null)
            {
                value.SelectedJob = value.Jobs.FirstOrDefault();
            }
        }
    }

    /// <summary>Gets or sets the free-text search applied to seasons, farms, or jobs.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RefreshFilteredSeasons();
            }
        }
    }

    /// <summary>Gets or sets a value indicating whether only active seasons are shown.</summary>
    public bool ShowOnlyActiveSeasons
    {
        get => _showOnlyActiveSeasons;
        set
        {
            if (SetProperty(ref _showOnlyActiveSeasons, value))
            {
                RefreshFilteredSeasons();
            }
        }
    }

    /// <summary>Gets a value indicating whether any seasons match the current filters.</summary>
    public bool HasFilteredResults => _filteredSeasons.Count > 0;

    /// <summary>Gets a value indicating whether the current filters produced no seasons.</summary>
    public bool IsEmpty => _filteredSeasons.Count == 0;

    private void RefreshFilteredSeasons()
    {
        var previouslySelected = _selectedSeason;

        _filteredSeasons.Clear();

        foreach (var season in _allSeasons)
        {
            if (_showOnlyActiveSeasons && !season.HasActiveJobs)
            {
                continue;
            }

            if (!season.MatchesSearch(_searchText))
            {
                continue;
            }

            _filteredSeasons.Add(season);
        }

        OnPropertyChanged(nameof(Seasons));
        OnPropertyChanged(nameof(HasFilteredResults));
        OnPropertyChanged(nameof(IsEmpty));

        SeasonSummaryViewModel? target = null;

        if (previouslySelected is not null && _filteredSeasons.Contains(previouslySelected))
        {
            target = previouslySelected;
        }
        else if (_filteredSeasons.Count > 0)
        {
            target = _filteredSeasons[0];
        }

        if (!ReferenceEquals(_selectedSeason, target))
        {
            _selectedSeason = target;
            OnPropertyChanged(nameof(SelectedSeason));
        }

        if (_selectedSeason is { SelectedJob: null })
        {
            _selectedSeason.SelectedJob = _selectedSeason.Jobs.FirstOrDefault();
        }
    }
}

/// <summary>Represents a season surfaced in the navigator UI.</summary>
public sealed class SeasonSummaryViewModel : ObservableObject
{
    private SeasonJobViewModel? _selectedJob;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonSummaryViewModel"/> class.
    /// </summary>
    public SeasonSummaryViewModel(
        string id,
        string name,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string? notes,
        string createdBy,
        DateTimeOffset createdAt,
        DateTimeOffset lastModifiedAt,
        IReadOnlyList<string> farmNames,
        IReadOnlyList<SeasonJobViewModel> jobs)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(createdBy);
        ArgumentNullException.ThrowIfNull(farmNames);
        ArgumentNullException.ThrowIfNull(jobs);

        if (farmNames.Count == 0)
        {
            throw new ArgumentException("At least one farm must be associated with the season", nameof(farmNames));
        }

        Id = id;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Notes = notes ?? string.Empty;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        LastModifiedAt = lastModifiedAt;
        FarmNames = farmNames;
        Jobs = jobs;
        _selectedJob = jobs.FirstOrDefault();
    }

    /// <summary>Gets the season identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the season name.</summary>
    public string Name { get; }

    /// <summary>Gets the start date for the season.</summary>
    public DateTimeOffset StartDate { get; }

    /// <summary>Gets the end date for the season.</summary>
    public DateTimeOffset EndDate { get; }

    /// <summary>Gets human-authored notes about the season.</summary>
    public string Notes { get; }

    /// <summary>Gets the author of the season document.</summary>
    public string CreatedBy { get; }

    /// <summary>Gets the season creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets the timestamp of the most recent update.</summary>
    public DateTimeOffset LastModifiedAt { get; }

    /// <summary>Gets the farms that participate in the season.</summary>
    public IReadOnlyList<string> FarmNames { get; }

    /// <summary>Gets the jobs associated with the season.</summary>
    public IReadOnlyList<SeasonJobViewModel> Jobs { get; }

    /// <summary>Gets or sets the currently selected job inside the season detail view.</summary>
    public SeasonJobViewModel? SelectedJob
    {
        get => _selectedJob;
        set => SetProperty(ref _selectedJob, value);
    }

    /// <summary>Gets a formatted date range for display.</summary>
    public string DateRangeDisplay => $"{StartDate:MMM d, yyyy} – {EndDate:MMM d, yyyy}";

    /// <summary>Gets the most recent update summary.</summary>
    public string LastUpdatedDisplay => $"Updated {LastModifiedAt:MMM d, yyyy}";

    /// <summary>Gets a formatted farm list.</summary>
    public string FarmListDisplay => string.Join(", ", FarmNames);

    /// <summary>Gets a string summarizing job counts.</summary>
    public string JobCountDisplay => Jobs.Count == 1 ? "1 job" : $"{Jobs.Count} jobs";

    /// <summary>Gets a status label for the season.</summary>
    public string StatusDisplay => HasActiveJobs ? "Active" : "Planning";

    /// <summary>Gets a flag indicating whether any jobs in the season are active.</summary>
    public bool HasActiveJobs => Jobs.Any(job => job.IsActive);

    /// <summary>Gets a human-readable summary of the authoring metadata.</summary>
    public string CreatedByDisplay => $"Created by {CreatedBy} on {CreatedAt:MMM d, yyyy}";

    /// <summary>Determines if the season matches the specified search query.</summary>
    public bool MatchesSearch(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var value = query.Trim();

        return Name.Contains(value, StringComparison.OrdinalIgnoreCase)
            || Notes.Contains(value, StringComparison.OrdinalIgnoreCase)
            || FarmNames.Any(farm => farm.Contains(value, StringComparison.OrdinalIgnoreCase))
            || Jobs.Any(job => job.MatchesSearch(value));
    }
}

/// <summary>Represents a job entry displayed within a season.</summary>
public sealed class SeasonJobViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonJobViewModel"/> class.
    /// </summary>
    public SeasonJobViewModel(
        string id,
        string name,
        string farmName,
        IReadOnlyList<string> fieldNames,
        string operationType,
        string status,
        DateTimeOffset? scheduledFor,
        DateTimeOffset? lastWorkedAt,
        string? notes,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(farmName);
        ArgumentException.ThrowIfNullOrEmpty(operationType);
        ArgumentException.ThrowIfNullOrEmpty(status);
        ArgumentNullException.ThrowIfNull(fieldNames);

        if (fieldNames.Count == 0)
        {
            throw new ArgumentException("At least one field must be provided for a job.", nameof(fieldNames));
        }

        if (fieldNames.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Field names cannot be empty.", nameof(fieldNames));
        }

        Id = id;
        Name = name;
        FarmName = farmName;
        FieldNames = fieldNames;
        OperationType = operationType;
        Status = status;
        ScheduledFor = scheduledFor;
        LastWorkedAt = lastWorkedAt;
        Notes = notes ?? string.Empty;
        IsActive = isActive;
    }

    /// <summary>Gets the job identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the display name of the job.</summary>
    public string Name { get; }

    /// <summary>Gets the farm associated with the job.</summary>
    public string FarmName { get; }

    /// <summary>Gets the fields associated with the job.</summary>
    public IReadOnlyList<string> FieldNames { get; }

    /// <summary>Gets the primary field name for display.</summary>
    public string PrimaryFieldName => FieldNames[0];

    /// <summary>Gets the operation type for the job.</summary>
    public string OperationType { get; }

    /// <summary>Gets the lifecycle status of the job.</summary>
    public string Status { get; }

    /// <summary>Gets the scheduled execution date, if any.</summary>
    public DateTimeOffset? ScheduledFor { get; }

    /// <summary>Gets the timestamp of the last activity on the job, if any.</summary>
    public DateTimeOffset? LastWorkedAt { get; }

    /// <summary>Gets operator notes for the job.</summary>
    public string Notes { get; }

    /// <summary>Gets a value indicating whether the job is currently active.</summary>
    public bool IsActive { get; }

    /// <summary>Gets a short summary describing the operation and its state.</summary>
    public string ActivitySummary => $"{OperationType} · {Status}";

    /// <summary>Gets a display subtitle describing the farm and field.</summary>
    public string Subtitle => FieldNames.Count switch
    {
        1 => $"{FarmName} · {FieldNames[0]}",
        2 => $"{FarmName} · {FieldNames[0]} & {FieldNames[1]}",
        _ => $"{FarmName} · {FieldNames[0]} + {FieldNames.Count - 1} more",
    };

    /// <summary>Gets a schedule summary for the job.</summary>
    public string ScheduleDisplay => ScheduledFor.HasValue
        ? $"Scheduled {ScheduledFor:MMM d, yyyy}"
        : "Schedule pending";

    /// <summary>Gets a summary of the last recorded progress.</summary>
    public string LastWorkedDisplay => LastWorkedAt.HasValue
        ? $"Last worked {LastWorkedAt:MMM d, yyyy}"
        : "No activity recorded";

    /// <summary>Gets a description of how many fields participate in the job.</summary>
    public string FieldCountDisplay => FieldNames.Count == 1 ? "1 field" : $"{FieldNames.Count} fields";

    /// <summary>Gets a summary string describing the field roster.</summary>
    public string FieldSummary => BuildFieldSummary();

    /// <summary>Determines if the job matches the provided search term.</summary>
    public bool MatchesSearch(string query)
    {
        return Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || FarmName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || FieldNames.Any(field => field.Contains(query, StringComparison.OrdinalIgnoreCase))
            || OperationType.Contains(query, StringComparison.OrdinalIgnoreCase)
            || Status.Contains(query, StringComparison.OrdinalIgnoreCase)
            || Notes.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private string BuildFieldSummary()
    {
        return FieldNames.Count switch
        {
            1 => FieldNames[0],
            2 => string.Join(" & ", FieldNames),
            _ => $"{FieldNames[0]}, {FieldNames[1]} + {FieldNames.Count - 2} more",
        };
    }
}
