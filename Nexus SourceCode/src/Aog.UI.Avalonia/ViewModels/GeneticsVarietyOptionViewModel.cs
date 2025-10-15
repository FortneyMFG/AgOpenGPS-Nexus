using System;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a single genetics variety surfaced in the picker UI with metadata for display and search.
/// </summary>
public sealed class GeneticsVarietyOptionViewModel : ObservableObject
{
    private readonly Action<GeneticsVarietyOptionViewModel> _onSelected;
    private readonly string _searchContent;
    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsVarietyOptionViewModel"/> class.
    /// </summary>
    public GeneticsVarietyOptionViewModel(
        string optionId,
        string brand,
        string product,
        string? traitStack,
        string? lot,
        string? treatment,
        string? source,
        string? barcode,
        string? notes,
        string? tagLabel,
        string accentColor,
        string? usageSummary,
        DateTimeOffset? lastUsedAt,
        bool isFavorite,
        Action<GeneticsVarietyOptionViewModel> onSelected)
    {
        if (string.IsNullOrWhiteSpace(optionId))
        {
            throw new ArgumentException("Option identifier is required.", nameof(optionId));
        }

        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new ArgumentException("Brand is required.", nameof(brand));
        }

        if (string.IsNullOrWhiteSpace(product))
        {
            throw new ArgumentException("Product is required.", nameof(product));
        }

        ArgumentNullException.ThrowIfNull(onSelected);

        OptionId = optionId.Trim();
        Brand = brand.Trim();
        Product = product.Trim();
        TraitStack = string.IsNullOrWhiteSpace(traitStack) ? null : traitStack.Trim();
        Lot = string.IsNullOrWhiteSpace(lot) ? null : lot.Trim();
        Treatment = string.IsNullOrWhiteSpace(treatment) ? null : treatment.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? string.Empty : notes.Trim();
        TagLabel = string.IsNullOrWhiteSpace(tagLabel) ? null : tagLabel.Trim();
        UsageSummary = string.IsNullOrWhiteSpace(usageSummary) ? string.Empty : usageSummary.Trim();
        LastUsedAt = lastUsedAt;
        IsFavorite = isFavorite;

        var parsedColor = Color.Parse(string.IsNullOrWhiteSpace(accentColor) ? "#FF2D5D9F" : accentColor.Trim());
        AccentBrush = new SolidColorBrush(parsedColor);
        AccentBackgroundBrush = new SolidColorBrush(parsedColor) { Opacity = 0.12 };

        _searchContent = string.Join(' ', new[]
        {
            Brand,
            Product,
            TraitStack,
            Lot,
            Treatment,
            Source,
            Barcode,
            Notes,
            TagLabel,
            UsageSummary,
        }.Where(part => !string.IsNullOrWhiteSpace(part))).ToLower(CultureInfo.InvariantCulture);

        _onSelected = onSelected;
        SelectCommand = new DelegateCommand(_ => _onSelected(this));
    }

    /// <summary>Gets the unique identifier for the option.</summary>
    public string OptionId { get; }

    /// <summary>Gets the seed brand.</summary>
    public string Brand { get; }

    /// <summary>Gets the product/hybrid string.</summary>
    public string Product { get; }

    /// <summary>Gets the optional trait stack description.</summary>
    public string? TraitStack { get; }

    /// <summary>Gets a value indicating whether a trait stack is present.</summary>
    public bool HasTraitStack => !string.IsNullOrEmpty(TraitStack);

    /// <summary>Gets the optional lot identifier.</summary>
    public string? Lot { get; }

    /// <summary>Gets the lot display string.</summary>
    public string LotDisplay => Lot ?? "—";

    /// <summary>Gets the optional seed treatment.</summary>
    public string? Treatment { get; }

    /// <summary>Gets the treatment display string.</summary>
    public string TreatmentDisplay => Treatment ?? "—";

    /// <summary>Gets the optional source description.</summary>
    public string? Source { get; }

    /// <summary>Gets the optional barcode payload.</summary>
    public string? Barcode { get; }

    /// <summary>Gets a value indicating whether a barcode is available.</summary>
    public bool HasBarcode => !string.IsNullOrEmpty(Barcode);

    /// <summary>Gets additional notes.</summary>
    public string Notes { get; }

    /// <summary>Gets the optional tag label (Favorite, Recent, etc.).</summary>
    public string? TagLabel { get; }

    /// <summary>Gets a value indicating whether the option exposes a tag.</summary>
    public bool HasTag => !string.IsNullOrEmpty(TagLabel);

    /// <summary>Gets the optional usage summary text.</summary>
    public string UsageSummary { get; }

    /// <summary>Gets the last used timestamp, if any.</summary>
    public DateTimeOffset? LastUsedAt { get; }

    /// <summary>Gets a display string describing when the option was last used.</summary>
    public string LastUsedDisplay => LastUsedAt?.ToLocalTime().ToString("MMM d • HH:mm") ?? "—";

    /// <summary>Gets a value indicating whether the option is marked as a favorite.</summary>
    public bool IsFavorite { get; }

    /// <summary>Gets a value indicating whether the option is considered recent.</summary>
    public bool IsRecent => LastUsedAt is not null;

    /// <summary>Gets a formatted display name used in status messages.</summary>
    public string DisplayName => string.IsNullOrEmpty(TraitStack)
        ? $"{Brand} {Product}"
        : $"{Brand} {Product} ({TraitStack})";

    /// <summary>Gets the accent brush used for outlines.</summary>
    public IBrush AccentBrush { get; }

    /// <summary>Gets the accent background brush.</summary>
    public IBrush AccentBackgroundBrush { get; }

    /// <summary>Gets the command invoked when the option is selected.</summary>
    public ICommand SelectCommand { get; }

    /// <summary>Gets or sets whether the option is currently selected.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetSelected(value, suppressCallback: false);
    }

    /// <summary>
    /// Determines whether the option matches the provided search token.
    /// </summary>
    internal bool MatchesToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        var normalized = token.ToLower(CultureInfo.InvariantCulture);
        return _searchContent.Contains(normalized, StringComparison.Ordinal);
    }

    /// <summary>
    /// Updates the selection state while optionally suppressing callbacks.
    /// </summary>
    internal void SetSelected(bool value, bool suppressCallback)
    {
        if (!SetProperty(ref _isSelected, value))
        {
            return;
        }

        if (!suppressCallback && value)
        {
            _onSelected(this);
        }
    }
}
