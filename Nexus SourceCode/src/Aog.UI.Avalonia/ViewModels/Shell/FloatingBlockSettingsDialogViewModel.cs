using System;
using ReactiveUI;

namespace Aog.UI.Avalonia.ViewModels.Shell;

public sealed class FloatingBlockSettingsDialogViewModel : ReactiveObject
{
    private readonly BlockLayoutViewModel _layout;
    private readonly FloatingBlockViewModel _block;
    private double _width;
    private double _height;

    public FloatingBlockSettingsDialogViewModel(BlockLayoutViewModel layout, FloatingBlockViewModel block)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _block = block ?? throw new ArgumentNullException(nameof(block));
        var bounds = block.Bounds;
        _width = bounds.Width;
        _height = bounds.Height;
    }

    public string Label => _block.Label;

    public string DefinitionId => _block.Definition.Id.Value;

    public double Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    public double Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    public bool CanEdit => !_layout.IsLocked && !_block.IsLocked;

    public void Apply()
    {
        if (_layout.IsLocked)
        {
            return;
        }

        var sanitizedWidth = double.IsFinite(_width) ? _width : _block.Bounds.Width;
        var sanitizedHeight = double.IsFinite(_height) ? _height : _block.Bounds.Height;
        _layout.ApplyFloatingBlockSettings(_block, sanitizedWidth, sanitizedHeight);
    }
}
