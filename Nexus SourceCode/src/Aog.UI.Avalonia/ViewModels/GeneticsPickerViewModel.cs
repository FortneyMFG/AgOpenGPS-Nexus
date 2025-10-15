using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Coordinates the genetics variety picker UI including favorites, recents, search, and barcode scans per ADR-046.
/// </summary>
public sealed class GeneticsPickerViewModel : ObservableObject
{
    private readonly Dictionary<string, GeneticsVarietyOptionViewModel> _catalog;
    private readonly ObservableCollection<GeneticsVarietyOptionViewModel> _favorites;
    private readonly ObservableCollection<GeneticsVarietyOptionViewModel> _recents;
    private readonly ObservableCollection<GeneticsVarietyOptionViewModel> _searchResults;
    private GeneticsVarietyOptionViewModel? _selectedOption;
    private string _statusMessage;
    private string _detailMessage;
    private string _searchQuery = string.Empty;
    private string _searchSummary;
    private string _barcodeStatus;
    private bool _hasBarcodeError;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsPickerViewModel"/> class.
    /// </summary>
    public GeneticsPickerViewModel(
        string jobDisplay,
        string activeSessionDisplay,
        string panelDescription,
        string barcodeHint)
    {
        if (string.IsNullOrWhiteSpace(jobDisplay))
        {
            throw new ArgumentException("Job display is required.", nameof(jobDisplay));
        }

        if (string.IsNullOrWhiteSpace(activeSessionDisplay))
        {
            throw new ArgumentException("Active session display is required.", nameof(activeSessionDisplay));
        }

        JobDisplay = jobDisplay.Trim();
        ActiveSessionDisplay = activeSessionDisplay.Trim();
        PanelDescription = string.IsNullOrWhiteSpace(panelDescription) ? string.Empty : panelDescription.Trim();
        BarcodeHint = string.IsNullOrWhiteSpace(barcodeHint) ? string.Empty : barcodeHint.Trim();

        _catalog = new Dictionary<string, GeneticsVarietyOptionViewModel>(StringComparer.OrdinalIgnoreCase);
        _favorites = new ObservableCollection<GeneticsVarietyOptionViewModel>();
        _recents = new ObservableCollection<GeneticsVarietyOptionViewModel>();
        _searchResults = new ObservableCollection<GeneticsVarietyOptionViewModel>();

        _statusMessage = "Scan a barcode or choose a variety for the active session.";
        _detailMessage = "Favorites and recents are curated from genetics analytics.";
        _searchSummary = "No catalog loaded.";
        _barcodeStatus = "Waiting for barcode input.";
    }

    /// <summary>Gets the job display string (job + farm context).</summary>
    public string JobDisplay { get; }

    /// <summary>Gets the active session display string.</summary>
    public string ActiveSessionDisplay { get; }

    /// <summary>Gets the descriptive text shown at the top of the panel.</summary>
    public string PanelDescription { get; }

    /// <summary>Gets the hint text describing barcode usage.</summary>
    public string BarcodeHint { get; }

    /// <summary>Gets the favorites surfaced in the picker.</summary>
    public IReadOnlyList<GeneticsVarietyOptionViewModel> FavoriteLots => _favorites;

    /// <summary>Gets the recently used lots.</summary>
    public IReadOnlyList<GeneticsVarietyOptionViewModel> RecentLots => _recents;

    /// <summary>Gets the search results.</summary>
    public IReadOnlyList<GeneticsVarietyOptionViewModel> SearchResults => _searchResults;

    /// <summary>Gets the status message describing the current selection state.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets additional metadata describing the current selection.</summary>
    public string DetailMessage
    {
        get => _detailMessage;
        private set => SetProperty(ref _detailMessage, value);
    }

    /// <summary>Gets or sets the search query string.</summary>
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _searchQuery, normalized))
            {
                RefreshSearchResults();
            }
        }
    }

    /// <summary>Gets the search summary text.</summary>
    public string SearchSummary
    {
        get => _searchSummary;
        private set => SetProperty(ref _searchSummary, value);
    }

    /// <summary>Gets the latest barcode status message.</summary>
    public string BarcodeStatus
    {
        get => _barcodeStatus;
        private set => SetProperty(ref _barcodeStatus, value);
    }

    /// <summary>Gets a value indicating whether the barcode status represents an error.</summary>
    public bool HasBarcodeError
    {
        get => _hasBarcodeError;
        private set => SetProperty(ref _hasBarcodeError, value);
    }

    /// <summary>Gets the currently selected option.</summary>
    public GeneticsVarietyOptionViewModel? SelectedOption
    {
        get => _selectedOption;
        private set
        {
            if (SetProperty(ref _selectedOption, value))
            {
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }

    /// <summary>Gets a value indicating whether a selection is active.</summary>
    public bool HasSelection => SelectedOption is not null;

    /// <summary>
    /// Applies a catalog of genetics varieties to the picker.
    /// </summary>
    public void ApplyCatalog(IEnumerable<GeneticsVarietyCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        _catalog.Clear();
        _favorites.Clear();
        _recents.Clear();
        _searchResults.Clear();
        SelectedOption = null;

        foreach (var entry in entries)
        {
            if (entry is null)
            {
                throw new ArgumentException("Catalog entries cannot contain null values.", nameof(entries));
            }

            if (_catalog.ContainsKey(entry.OptionId))
            {
                throw new ArgumentException($"Duplicate option identifier '{entry.OptionId}'.", nameof(entries));
            }

            var option = CreateOption(entry);
            _catalog.Add(option.OptionId, option);
        }

        foreach (var option in _catalog.Values.Where(option => option.IsFavorite)
            .OrderBy(option => option.Brand, StringComparer.OrdinalIgnoreCase)
            .ThenBy(option => option.Product, StringComparer.OrdinalIgnoreCase))
        {
            _favorites.Add(option);
        }

        foreach (var option in _catalog.Values.Where(option => option.IsRecent)
            .OrderByDescending(option => option.LastUsedAt))
        {
            _recents.Add(option);
        }

        RefreshSearchResults();

        StatusMessage = "Scan a barcode or choose a variety for the active session.";
        DetailMessage = "Favorites and recents are curated from genetics analytics.";
        BarcodeStatus = "Waiting for barcode input.";
        HasBarcodeError = false;

        OnPropertyChanged(nameof(FavoriteLots));
        OnPropertyChanged(nameof(RecentLots));
        OnPropertyChanged(nameof(SearchResults));
    }

    /// <summary>
    /// Applies an initial selection without raising user callbacks.
    /// </summary>
    public void ApplyInitialSelection(string optionId)
    {
        if (string.IsNullOrWhiteSpace(optionId))
        {
            throw new ArgumentException("Option identifier is required.", nameof(optionId));
        }

        if (!_catalog.TryGetValue(optionId, out var option))
        {
            return;
        }

        option.SetSelected(true, suppressCallback: true);
        SelectedOption = option;
        UpdateStatus(option, triggeredByUser: false);
    }

    /// <summary>
    /// Processes an incoming barcode scan, selecting the matching variety when found.
    /// </summary>
    public void ApplyBarcodeScan(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            BarcodeStatus = "Scan ignored — barcode payload was empty.";
            HasBarcodeError = true;
            return;
        }

        var normalized = barcode.Trim();
        var match = _catalog.Values.FirstOrDefault(option =>
            string.Equals(option.Barcode, normalized, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            BarcodeStatus = $"No genetics variety matched barcode '{normalized}'.";
            HasBarcodeError = true;
            return;
        }

        SelectOption(match, triggeredByUser: false);
        BarcodeStatus = $"Matched barcode '{normalized}' to {match.DisplayName}.";
        HasBarcodeError = false;
    }

    /// <summary>
    /// Creates the design-time sample used by the UI shell.
    /// </summary>
    public static GeneticsPickerViewModel CreateSample()
    {
        var viewModel = new GeneticsPickerViewModel(
            jobDisplay: "Planting — South Farm 2025",
            activeSessionDisplay: "Session 2 • Apr 18 Morning Planting",
            panelDescription: "Assign varieties using search, favorites, or barcode scans. Sample data aligns with ADR-046.",
            barcodeHint: "Scan bag barcodes or use quick picks when scouting lots.");

        var now = new DateTimeOffset(2025, 4, 18, 14, 30, 0, TimeSpan.Zero);
        var entries = new[]
        {
            new GeneticsVarietyCatalogEntry(
                OptionId: "favorite-p1185q",
                Brand: "Pioneer",
                Product: "P1185Q",
                TraitStack: "Qrome",
                Lot: "LOT-445",
                Treatment: "Lumisure + Insect",
                Source: "Planner recommendation",
                Barcode: "PNR-P1185Q-445",
                Notes: "Default planter preset + singulation tuning.",
                TagLabel: "Favorite",
                AccentColor: "#FF2D5D9F",
                UsageSummary: "Queued for 3 South Farm sessions.",
                LastUsedAt: now.AddDays(-10),
                IsFavorite: true),
            new GeneticsVarietyCatalogEntry(
                OptionId: "favorite-dkc6435",
                Brand: "DEKALB",
                Product: "DKC64-35RIB",
                TraitStack: "SmartStax® RIB Complete",
                Lot: "LOT-882",
                Treatment: "Acceleron Elite",
                Source: "Preset import",
                Barcode: "DKC6435-LOT882",
                Notes: "High-yield block for irrigated pivot.",
                TagLabel: "Favorite",
                AccentColor: "#FFE27D60",
                UsageSummary: "2024 yield leader • +11 bu/ac over farm avg.",
                LastUsedAt: now.AddDays(-42),
                IsFavorite: true),
            new GeneticsVarietyCatalogEntry(
                OptionId: "recent-xtendflex",
                Brand: "Asgrow",
                Product: "AG20X9",
                TraitStack: "XtendFlex",
                Lot: "LOT-BQX-2025",
                Treatment: "ILeVO + Saltro",
                Source: "Barcode scan",
                Barcode: "ASG-LOT-BQX-2025",
                Notes: "Assigned to South Farm east half — variety trial.",
                TagLabel: "Recent",
                AccentColor: "#FF3BAFDA",
                UsageSummary: "Scanned during Session 1 two days ago.",
                LastUsedAt: now.AddDays(-2),
                IsFavorite: false),
            new GeneticsVarietyCatalogEntry(
                OptionId: "recent-enliste3",
                Brand: "Corteva",
                Product: "P16A50E3",
                TraitStack: "Enlist E3",
                Lot: "LOT-ENL-410",
                Treatment: "Fungicide + Insect",
                Source: "Manual override",
                Barcode: "P16A50-LOT410",
                Notes: "Rotation swap for field edge lodging mitigation.",
                TagLabel: "Recent",
                AccentColor: "#FF9BC53D",
                UsageSummary: "Applied to 42 acres last session.",
                LastUsedAt: now.AddDays(-5),
                IsFavorite: false),
            new GeneticsVarietyCatalogEntry(
                OptionId: "catalog-specialty",
                Brand: "Specialty Hybrids",
                Product: "62A12",
                TraitStack: "Agrisure Artesian",
                Lot: "LOT-SPC-1337",
                Treatment: "CruiserMaxx + Avicta",
                Source: "Catalog import",
                Barcode: "SPC62A12-LOT1337",
                Notes: "Drought guard test plot candidate.",
                TagLabel: null,
                AccentColor: "#FF7A5EA8",
                UsageSummary: "Flagged for west hilltop dryland block.",
                LastUsedAt: null,
                IsFavorite: false),
        };

        viewModel.ApplyCatalog(entries);
        viewModel.ApplyInitialSelection("favorite-p1185q");
        return viewModel;
    }

    private GeneticsVarietyOptionViewModel CreateOption(GeneticsVarietyCatalogEntry entry)
    {
        return new GeneticsVarietyOptionViewModel(
            entry.OptionId,
            entry.Brand,
            entry.Product,
            entry.TraitStack,
            entry.Lot,
            entry.Treatment,
            entry.Source,
            entry.Barcode,
            entry.Notes,
            entry.TagLabel,
            entry.AccentColor ?? "#FF2D5D9F",
            entry.UsageSummary,
            entry.LastUsedAt,
            entry.IsFavorite,
            OnOptionSelected);
    }

    private void OnOptionSelected(GeneticsVarietyOptionViewModel option)
    {
        SelectOption(option, triggeredByUser: true);
        BarcodeStatus = "Manual selection applied.";
        HasBarcodeError = false;
    }

    private void SelectOption(GeneticsVarietyOptionViewModel option, bool triggeredByUser)
    {
        ArgumentNullException.ThrowIfNull(option);

        if (SelectedOption == option)
        {
            UpdateStatus(option, triggeredByUser);
            return;
        }

        if (SelectedOption is not null)
        {
            SelectedOption.SetSelected(false, suppressCallback: true);
        }

        option.SetSelected(true, suppressCallback: true);
        SelectedOption = option;
        UpdateStatus(option, triggeredByUser);
    }

    private void UpdateStatus(GeneticsVarietyOptionViewModel option, bool triggeredByUser)
    {
        StatusMessage = triggeredByUser
            ? $"Queued {option.DisplayName} for {ActiveSessionDisplay}."
            : $"Active variety: {option.DisplayName} for {ActiveSessionDisplay}.";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(option.TraitStack))
        {
            parts.Add($"Trait: {option.TraitStack}");
        }

        if (!string.IsNullOrWhiteSpace(option.Lot))
        {
            parts.Add($"Lot {option.Lot}");
        }

        if (!string.IsNullOrWhiteSpace(option.Treatment))
        {
            parts.Add(option.Treatment);
        }

        if (!string.IsNullOrWhiteSpace(option.Source))
        {
            parts.Add(option.Source);
        }

        if (!string.IsNullOrWhiteSpace(option.UsageSummary))
        {
            parts.Add(option.UsageSummary);
        }

        DetailMessage = parts.Count > 0
            ? string.Join(" • ", parts)
            : "Variety metadata provided by genetics plugin.";
    }

    private void RefreshSearchResults()
    {
        var tokens = _searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IEnumerable<GeneticsVarietyOptionViewModel> query;

        if (tokens.Length == 0)
        {
            query = _catalog.Values
                .OrderBy(option => option.Brand, StringComparer.OrdinalIgnoreCase)
                .ThenBy(option => option.Product, StringComparer.OrdinalIgnoreCase);
            SearchSummary = _catalog.Count == 0
                ? "No catalog loaded."
                : $"Showing {_catalog.Count} catalog varieties.";
        }
        else
        {
            query = _catalog.Values
                .Where(option => tokens.All(token => option.MatchesToken(token)))
                .OrderBy(option => option.Brand, StringComparer.OrdinalIgnoreCase)
                .ThenBy(option => option.Product, StringComparer.OrdinalIgnoreCase);

            var matches = query.ToList();
            SearchSummary = matches.Count == 0
                ? $"No varieties match \"{_searchQuery}\"."
                : $"Showing {matches.Count} match(es) for \"{_searchQuery}\".";
            query = matches;
        }

        _searchResults.Clear();
        foreach (var option in query.Take(12))
        {
            _searchResults.Add(option);
        }

        OnPropertyChanged(nameof(SearchResults));
    }
}
