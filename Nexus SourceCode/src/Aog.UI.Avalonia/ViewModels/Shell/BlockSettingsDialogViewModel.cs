using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;

namespace Aog.UI.Avalonia.ViewModels.Shell;

public sealed class BlockSettingsDialogViewModel : ReactiveObject
{
    private readonly BlockLayoutViewModel _layout;
    private readonly BlockItemViewModel _block;
    private string _title;
    private string _value;
    private double _widthUnits;
    private double _heightUnits;
    private double _backgroundOpacity;
    private BlockColorOption _selectedColor;

    public BlockSettingsDialogViewModel(BlockLayoutViewModel layout, BlockItemViewModel block)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _block = block ?? throw new ArgumentNullException(nameof(block));

        _title = block.Instance.TitleOverride ?? block.Label;
        _value = block.Value ?? string.Empty;
        _widthUnits = block.WidthUnits;
        _heightUnits = block.HeightUnits;
        _backgroundOpacity = Math.Round(block.BackgroundOpacity * 100d, 0);
        ColorOptions = BlockLayoutViewModel.ValueColorPalette
            .Select(option => new BlockColorOption(option.Name, option.Color))
            .ToArray();
        var currentColor = block.ValueColor;
        var match = ColorOptions.FirstOrDefault(option =>
            string.Equals(option.Color, currentColor, StringComparison.OrdinalIgnoreCase));
        _selectedColor = string.IsNullOrWhiteSpace(match.Color)
            ? ColorOptions.First()
            : match;
    }

    public string BlockLabel => _block.Label;

    public string DefinitionId => _block.Definition.Id.Value;

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    public double WidthUnits
    {
        get => _widthUnits;
        set => this.RaiseAndSetIfChanged(ref _widthUnits, value);
    }

    public double HeightUnits
    {
        get => _heightUnits;
        set => this.RaiseAndSetIfChanged(ref _heightUnits, value);
    }

    public double BackgroundOpacity
    {
        get => _backgroundOpacity;
        set => this.RaiseAndSetIfChanged(ref _backgroundOpacity, value);
    }

    public IReadOnlyList<BlockColorOption> ColorOptions { get; }

    public BlockColorOption SelectedColor
    {
        get => _selectedColor;
        set => this.RaiseAndSetIfChanged(ref _selectedColor, value);
    }

    public bool CanEdit => !_layout.IsLocked;

    public double MinUnits => 0.5d;

    public double MaxUnits => 2d;

    public double UnitStep => 0.5d;

    public double MinOpacity => 0d;

    public double MaxOpacity => 100d;

    public void Apply()
    {
        if (!CanEdit)
        {
            return;
        }

        var sanitizedWidth = double.IsFinite(_widthUnits) ? _widthUnits : _block.WidthUnits;
        var sanitizedHeight = double.IsFinite(_heightUnits) ? _heightUnits : _block.HeightUnits;
        var sanitizedOpacity = double.IsFinite(_backgroundOpacity) ? _backgroundOpacity : _block.BackgroundOpacity * 100d;
        var color = SelectedColor.Color;

        _layout.ApplyBlockSettings(
            _block,
            _title,
            _value,
            sanitizedWidth,
            sanitizedHeight,
            sanitizedOpacity,
            color);
    }

    public readonly record struct BlockColorOption(string Name, string Color)
    {
        public override string ToString() => Name;
    }
}
