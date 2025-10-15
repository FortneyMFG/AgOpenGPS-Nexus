using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Surfaces curated crop selections for the active field envelope per ADR-045.
/// </summary>
public sealed class CropQuickSelectViewModel : ObservableObject
{
    private readonly List<CropQuickSelectGroupViewModel> _groups = new();
    private CropQuickSelectOptionViewModel? _selectedOption;
    private string _selectionStatus = string.Empty;
    private string _selectionDetails = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="CropQuickSelectViewModel"/> class.
    /// </summary>
    /// <param name="activeFieldDisplay">Display string describing the active field or envelope.</param>
    /// <param name="seasonContextDisplay">Display string describing the season context.</param>
    /// <param name="currentPlanDisplay">Summary of the current planned/actual crop.</param>
    /// <param name="panelDescription">Description shown at the top of the panel.</param>
    /// <param name="lastActionDisplay">Summary of the last assignment event.</param>
    public CropQuickSelectViewModel(
        string activeFieldDisplay,
        string seasonContextDisplay,
        string currentPlanDisplay,
        string panelDescription,
        string lastActionDisplay)
    {
        if (string.IsNullOrWhiteSpace(activeFieldDisplay))
        {
            throw new ArgumentException("Active field display is required.", nameof(activeFieldDisplay));
        }

        if (string.IsNullOrWhiteSpace(seasonContextDisplay))
        {
            throw new ArgumentException("Season context display is required.", nameof(seasonContextDisplay));
        }

        ActiveFieldDisplay = activeFieldDisplay.Trim();
        SeasonContextDisplay = seasonContextDisplay.Trim();
        CurrentPlanDisplay = string.IsNullOrWhiteSpace(currentPlanDisplay) ? string.Empty : currentPlanDisplay.Trim();
        PanelDescription = string.IsNullOrWhiteSpace(panelDescription) ? string.Empty : panelDescription.Trim();
        LastActionDisplay = string.IsNullOrWhiteSpace(lastActionDisplay) ? string.Empty : lastActionDisplay.Trim();

        _selectionStatus = "Select a crop to assign to the active envelope.";
        _selectionDetails = "Rotations and favorites are curated from crop history.";
    }

    /// <summary>Gets the display name for the active field or envelope.</summary>
    public string ActiveFieldDisplay { get; }

    /// <summary>Gets the display string describing the active season context.</summary>
    public string SeasonContextDisplay { get; }

    /// <summary>Gets a summary of the current plan or actual crop.</summary>
    public string CurrentPlanDisplay { get; }

    /// <summary>Gets the descriptive text shown at the top of the panel.</summary>
    public string PanelDescription { get; }

    /// <summary>Gets the summary describing the most recent crop assignment.</summary>
    public string LastActionDisplay { get; }

    /// <summary>Gets the crop groups surfaced in the UI.</summary>
    public IReadOnlyList<CropQuickSelectGroupViewModel> Groups => _groups;

    /// <summary>Gets the primary status message describing the current selection.</summary>
    public string SelectionStatus
    {
        get => _selectionStatus;
        private set => SetProperty(ref _selectionStatus, value);
    }

    /// <summary>Gets additional detail describing the current selection.</summary>
    public string SelectionDetails
    {
        get => _selectionDetails;
        private set => SetProperty(ref _selectionDetails, value);
    }

    /// <summary>Gets a value indicating whether an option is currently selected.</summary>
    public bool HasSelection => _selectedOption is not null;

    /// <summary>
    /// Creates the sample quick-select view-model used by the design-time shell.
    /// </summary>
    public static CropQuickSelectViewModel CreateSample()
    {
        var viewModel = new CropQuickSelectViewModel(
            activeFieldDisplay: "North 80 — South Farm",
            seasonContextDisplay: "2025 Crop Year • Envelope: Planting prep",
            currentPlanDisplay: "Planned: Soybeans • Actual 2024: Corn",
            panelDescription: "Assign crops to the active field envelope. Rotations and favorites come from ADR-045 sample data.",
            lastActionDisplay: "Last assignment: planner.annika • Mar 12, 2025 — Winter rotation review");

        var groups = new[]
        {
            new CropQuickSelectGroupViewModel(
                "Common rotations",
                "Suggested crops based on the last five seasons on North 80.",
                new[]
                {
                    viewModel.CreateOption(
                        optionId: "rotation-corn-2025",
                        crop: "Corn",
                        variety: "P1185Q™",
                        seasonWindow: "2025 Planned",
                        rotationSummary: "Follows 2024 Soybeans • Maintains corn/soy rotation cadence",
                        notes: "Links to nitrogen program 'South Farm Corn 2025'",
                        tagLabel: "Rotation",
                        accentColor: "#FF2D5D9F"),
                    viewModel.CreateOption(
                        optionId: "rotation-soy-2025",
                        crop: "Soybeans",
                        variety: "Enlist E3 2.7",
                        seasonWindow: "2025 Planned",
                        rotationSummary: "Follows 2024 Corn • Targets 135k population",
                        notes: "Associates planter preset 'Soybeans VR 2025'",
                        tagLabel: "Rotation",
                        accentColor: "#FF4C9566"),
                    viewModel.CreateOption(
                        optionId: "rotation-cover-rye",
                        crop: "Cereal Rye",
                        variety: "Aroostook",
                        seasonWindow: "2024 Fall Cover",
                        rotationSummary: "Interseed after soybean harvest for winter erosion control",
                        notes: "Auto-triggers drill configuration 'Cover Crop 12ft'",
                        tagLabel: "Cover",
                        accentColor: "#FF7A5EA8"),
                }),
            new CropQuickSelectGroupViewModel(
                "Favorites",
                "Pinned crops shared by the planner team across South Farm.",
                new[]
                {
                    viewModel.CreateOption(
                        optionId: "favorite-sweetcorn",
                        crop: "Sweet Corn",
                        variety: "Attribute II 2787",
                        seasonWindow: "2025 Specialty Block",
                        rotationSummary: "CSA deliveries block • Requires daily scouting",
                        notes: "Include traceability tags before committing",
                        tagLabel: "Favorite",
                        accentColor: "#FFE27D60"),
                    viewModel.CreateOption(
                        optionId: "favorite-alfalfa",
                        crop: "Alfalfa",
                        variety: "Hi-Gest 360",
                        seasonWindow: "2025-2027 Perennial",
                        rotationSummary: "Three-year stand for dairy feed contract",
                        notes: "Allocates irrigation preset 'Hay East Pivot'",
                        tagLabel: "Favorite",
                        accentColor: "#FF9BC53D"),
                }),
            new CropQuickSelectGroupViewModel(
                "Recent assignments",
                "Latest crop selections from nearby envelopes.",
                new[]
                {
                    viewModel.CreateOption(
                        optionId: "recent-wheat",
                        crop: "Winter Wheat",
                        variety: "WB4401",
                        seasonWindow: "2024 Actual",
                        rotationSummary: "Harvested 2024 • Ready for double-crop soy trial",
                        notes: "Yield goal 85 bu/ac",
                        tagLabel: "Recent",
                        accentColor: "#FFB8860B"),
                    viewModel.CreateOption(
                        optionId: "recent-canola",
                        crop: "Canola",
                        variety: "InVigor L340PC",
                        seasonWindow: "2024 Actual",
                        rotationSummary: "Assigned to East Pivot scouting envelope",
                        notes: "Keeps Sclerotinia rotation restrictions satisfied",
                        tagLabel: "Recent",
                        accentColor: "#FF3BAFDA"),
                }),
        };

        viewModel.ApplyGroups(groups);
        viewModel.ApplyInitialSelection("rotation-soy-2025");
        return viewModel;
    }

    /// <summary>
    /// Applies a new set of groups to the quick-select panel.
    /// </summary>
    /// <param name="groups">Groups to surface.</param>
    public void ApplyGroups(IEnumerable<CropQuickSelectGroupViewModel> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        _groups.Clear();
        foreach (var group in groups)
        {
            if (group is null)
            {
                throw new ArgumentException("Groups cannot contain null entries.", nameof(groups));
            }

            _groups.Add(group);
        }

        OnPropertyChanged(nameof(Groups));
    }

    /// <summary>
    /// Applies an initial selection without triggering user-facing callbacks.
    /// </summary>
    /// <param name="optionId">Identifier of the option to select.</param>
    public void ApplyInitialSelection(string optionId)
    {
        if (string.IsNullOrWhiteSpace(optionId))
        {
            throw new ArgumentException("Option identifier is required.", nameof(optionId));
        }

        var target = _groups.SelectMany(group => group.Options)
            .FirstOrDefault(option => string.Equals(option.OptionId, optionId, StringComparison.OrdinalIgnoreCase));

        if (target is null)
        {
            return;
        }

        _selectedOption = target;
        target.SetSelected(true, suppressCallback: true);
        UpdateSelectionStatus(target, triggeredByUser: false);
        OnPropertyChanged(nameof(HasSelection));
    }

    private CropQuickSelectOptionViewModel CreateOption(
        string optionId,
        string crop,
        string? variety,
        string seasonWindow,
        string rotationSummary,
        string? notes,
        string? tagLabel,
        string accentColor)
    {
        return new CropQuickSelectOptionViewModel(
            optionId,
            crop,
            variety,
            seasonWindow,
            rotationSummary,
            notes,
            tagLabel,
            accentColor,
            OnOptionSelected);
    }

    private void OnOptionSelected(CropQuickSelectOptionViewModel option)
    {
        ArgumentNullException.ThrowIfNull(option);

        if (_selectedOption == option)
        {
            UpdateSelectionStatus(option, triggeredByUser: true);
            return;
        }

        if (_selectedOption is not null)
        {
            _selectedOption.SetSelected(false, suppressCallback: true);
        }

        _selectedOption = option;
        option.SetSelected(true, suppressCallback: true);
        UpdateSelectionStatus(option, triggeredByUser: true);
        OnPropertyChanged(nameof(HasSelection));
    }

    private void UpdateSelectionStatus(CropQuickSelectOptionViewModel option, bool triggeredByUser)
    {
        SelectionStatus = triggeredByUser
            ? $"Queued {option.DisplayName} for {ActiveFieldDisplay}."
            : $"Current plan: {option.DisplayName} for {ActiveFieldDisplay}.";

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(option.SeasonWindow))
        {
            details.Add(option.SeasonWindow);
        }

        if (!string.IsNullOrWhiteSpace(option.RotationSummary))
        {
            details.Add(option.RotationSummary);
        }

        if (!string.IsNullOrWhiteSpace(option.Notes))
        {
            details.Add(option.Notes);
        }

        SelectionDetails = details.Count > 0
            ? string.Join(" • ", details)
            : "Selection metadata pending from crop plugin.";
    }
}
