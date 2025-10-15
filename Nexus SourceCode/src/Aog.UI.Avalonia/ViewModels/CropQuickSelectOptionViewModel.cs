using System;
using System.Windows.Input;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a single quick-select crop option surfaced in the crop plugin UI.
/// </summary>
public sealed class CropQuickSelectOptionViewModel : ObservableObject
{
    private readonly Action<CropQuickSelectOptionViewModel> _onSelected;
    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="CropQuickSelectOptionViewModel"/> class.
    /// </summary>
    /// <param name="optionId">Stable identifier for the option used when tracking selections.</param>
    /// <param name="crop">Primary crop label.</param>
    /// <param name="variety">Optional variety or hybrid designation.</param>
    /// <param name="seasonWindow">Season or timing window for the option.</param>
    /// <param name="rotationSummary">Summary describing how the option fits into the rotation.</param>
    /// <param name="notes">Additional context or operational notes.</param>
    /// <param name="tagLabel">Optional tag label (e.g., "Rotation" or "Favorite").</param>
    /// <param name="accentColor">Accent color used for tag and selection visuals.</param>
    /// <param name="onSelected">Callback invoked when the option is selected.</param>
    public CropQuickSelectOptionViewModel(
        string optionId,
        string crop,
        string? variety,
        string seasonWindow,
        string rotationSummary,
        string? notes,
        string? tagLabel,
        string accentColor,
        Action<CropQuickSelectOptionViewModel> onSelected)
    {
        if (string.IsNullOrWhiteSpace(optionId))
        {
            throw new ArgumentException("Option identifier is required.", nameof(optionId));
        }

        if (string.IsNullOrWhiteSpace(crop))
        {
            throw new ArgumentException("Crop label is required.", nameof(crop));
        }

        if (string.IsNullOrWhiteSpace(seasonWindow))
        {
            throw new ArgumentException("Season window is required.", nameof(seasonWindow));
        }

        ArgumentNullException.ThrowIfNull(onSelected);

        OptionId = optionId.Trim();
        Crop = crop.Trim();
        Variety = string.IsNullOrWhiteSpace(variety) ? null : variety.Trim();
        SeasonWindow = seasonWindow.Trim();
        RotationSummary = string.IsNullOrWhiteSpace(rotationSummary) ? string.Empty : rotationSummary.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? string.Empty : notes.Trim();
        TagLabel = string.IsNullOrWhiteSpace(tagLabel) ? null : tagLabel.Trim();

        var parsedColor = Color.Parse(string.IsNullOrWhiteSpace(accentColor) ? "#FF2D5D9F" : accentColor.Trim());
        AccentBrush = new SolidColorBrush(parsedColor);
        AccentBackgroundBrush = new SolidColorBrush(parsedColor) { Opacity = 0.14 };

        _onSelected = onSelected;
        SelectCommand = new DelegateCommand(_ => _onSelected(this));
    }

    /// <summary>Gets the unique identifier associated with the option.</summary>
    public string OptionId { get; }

    /// <summary>Gets the primary crop label.</summary>
    public string Crop { get; }

    /// <summary>Gets the optional variety or hybrid designation.</summary>
    public string? Variety { get; }

    /// <summary>Gets a flag indicating whether a variety string is present.</summary>
    public bool HasVariety => !string.IsNullOrEmpty(Variety);

    /// <summary>Gets the formatted display name used in status messages.</summary>
    public string DisplayName => HasVariety ? $"{Crop} — {Variety}" : Crop;

    /// <summary>Gets the season window associated with the option.</summary>
    public string SeasonWindow { get; }

    /// <summary>Gets a rotation summary describing how the option fits the farm plan.</summary>
    public string RotationSummary { get; }

    /// <summary>Gets additional notes or operational guidance.</summary>
    public string Notes { get; }

    /// <summary>Gets the optional tag label shown alongside the crop name.</summary>
    public string? TagLabel { get; }

    /// <summary>Gets a value indicating whether the option exposes a tag label.</summary>
    public bool HasTag => !string.IsNullOrEmpty(TagLabel);

    /// <summary>Gets the accent brush used for borders and selected backgrounds.</summary>
    public IBrush AccentBrush { get; }

    /// <summary>Gets a muted version of the accent brush used when not selected.</summary>
    public IBrush AccentBackgroundBrush { get; }

    /// <summary>Gets the command invoked when the option is selected in the UI.</summary>
    public ICommand SelectCommand { get; }

    /// <summary>Gets or sets whether the option is currently selected.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetSelected(value, suppressCallback: false);
    }

    /// <summary>
    /// Updates the selection state for the option while optionally suppressing callback invocation.
    /// </summary>
    /// <param name="value">New selection state.</param>
    /// <param name="suppressCallback">Whether to suppress callback invocation.</param>
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
