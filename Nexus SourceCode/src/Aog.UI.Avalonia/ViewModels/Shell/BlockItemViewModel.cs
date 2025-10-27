using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.Layout;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.ViewModels.Shell;

/// <summary>
/// View-model representing a rendered block instance within the shell.
/// </summary>
public sealed class BlockItemViewModel : INotifyPropertyChanged
{
    private readonly BlockLayoutViewModel _owner;
    private readonly DelegateCommand _invokeCommand;
    private readonly DelegateCommand _deleteCommand;
    private readonly DelegateCommand _openSettingsCommand;
    private readonly TileSpec _tile;

    public BlockItemViewModel(BlockInstance instance, BlockDefinition definition, BlockLayoutViewModel owner, TileSpec tile)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _tile = tile ?? throw new ArgumentNullException(nameof(tile));

        _invokeCommand = new DelegateCommand(_ => _owner.Invoke(this), _ => _owner.CanInteract(this));
        _deleteCommand = new DelegateCommand(_ => _owner.Delete(this), _ => _owner.CanDelete(this));
        _openSettingsCommand = new DelegateCommand(_ => _owner.RequestBlockSettings(this), _ => _owner.CanEditBlock(this));
        SizeOptions = _owner.CreateSizeOptions(this);
    }

    /// <summary>Gets the underlying block instance for this tile.</summary>
    public BlockInstance Instance { get; }

    /// <summary>Gets the block definition metadata driving rendering and behavior.</summary>
    public BlockDefinition Definition { get; }

    /// <summary>Gets the user visible label for the block.</summary>
    public string Label
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Instance.TitleOverride))
            {
                return Instance.TitleOverride!;
            }

            return string.IsNullOrWhiteSpace(Definition.Label) ? Definition.Id.Value : Definition.Label;
        }
    }

    /// <summary>Gets the optional value presented by the block instance.</summary>
    public string? Value => Instance.GroupKey;

    /// <summary>Gets the optional icon asset key.</summary>
    public string? IconKey => Definition.IconKey;

    /// <summary>Gets the effective block sizing configuration for layout calculations.</summary>
    public BlockSize EffectiveSize => Instance.SizeOverride ?? Definition.PreferredSize;

    /// <summary>Gets the width of the block in block units.</summary>
    public double WidthUnits => EffectiveSize switch
    {
        BlockSize.Tile2x1 or BlockSize.Tile2x2 or BlockSize.Tile2xHalf => 2d,
        BlockSize.TileHalfx1 or BlockSize.TileHalfx2 => 0.5d,
        _ => 1d,
    };

    /// <summary>Gets the height of the block in block units.</summary>
    public double HeightUnits => EffectiveSize switch
    {
        BlockSize.Tile1x2 or BlockSize.Tile2x2 => 2d,
        BlockSize.Tile1xHalf or BlockSize.Tile2xHalf => 0.5d,
        BlockSize.TileHalfx2 => 2d,
        _ => 1d,
    };

    /// <summary>Gets the command triggered when the block is activated.</summary>
    public ICommand InvokeCommand => _invokeCommand;

    /// <summary>Gets the command that removes the block when permitted.</summary>
    public ICommand DeleteCommand => _deleteCommand;

    /// <summary>Gets the command that opens the advanced block configuration dialog.</summary>
    public ICommand OpenSettingsCommand => _openSettingsCommand;

    /// <summary>Gets a value indicating whether layout modifications are currently locked.</summary>
    public bool IsLayoutLocked => _owner.IsLocked;

    /// <summary>Gets the tile identifier associated with the block within the global grid.</summary>
    public string TileId => _tile.Id;

    /// <summary>Gets the tile metadata used to position the block on the global grid.</summary>
    public TileSpec Tile => _tile;

    /// <summary>Gets the owning layout view-model.</summary>
    internal BlockLayoutViewModel Owner => _owner;

    /// <summary>Gets a collection of size adjustment options for the block.</summary>
    public IReadOnlyList<BlockSizeOptionViewModel> SizeOptions { get; }

    /// <summary>Gets a value indicating whether the block exposes additional settings.</summary>
    public bool HasSettings => SizeOptions.Count > 0 || !_owner.IsLocked;

    /// <summary>Gets a value indicating whether resize presets are available.</summary>
    public bool HasSizeOptions => SizeOptions.Count > 0;

    /// <summary>Gets the effective block background opacity.</summary>
    public double BackgroundOpacity
    {
        get
        {
            var value = Instance.OpacityOverride ?? BlockLayoutViewModel.DefaultBlockOpacity;
            if (!double.IsFinite(value))
            {
                return BlockLayoutViewModel.DefaultBlockOpacity;
            }

            return Math.Clamp(value, 0d, 1d);
        }
    }

    /// <summary>Gets the foreground color applied to the block value text.</summary>
    public string ValueColor => string.IsNullOrWhiteSpace(Instance.ValueColorOverride)
        ? BlockLayoutViewModel.DefaultBlockValueColor
        : Instance.ValueColorOverride!;

    /// <summary>Gets a value indicating whether the block originates from a canonical definition.</summary>
    public bool IsCanonical => Instance.Origin == BlockOrigin.Canonical;

    /// <summary>Gets a value indicating whether the block originates as a clone.</summary>
    public bool IsClone => Instance.Origin == BlockOrigin.Clone;

    /// <summary>Gets a value indicating whether the block represents a menu or container block.</summary>
    public bool IsMenu => Definition.Kind == BlockKind.Menu || Definition.Kind == BlockKind.Container;

    /// <summary>Gets a value indicating whether the block represents telemetry.</summary>
    public bool IsTelemetry => Definition.Kind == BlockKind.Telemetry;

    /// <summary>
    /// Re-evaluates command availability when layout state changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void RefreshCommandStates()
    {
        _invokeCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
        _openSettingsCommand.RaiseCanExecuteChanged();
    }

    internal void RefreshSettingsState()
    {
        foreach (var option in SizeOptions)
        {
            option.Refresh();
        }

        OnPropertyChanged(nameof(HasSettings));
        OnPropertyChanged(nameof(HasSizeOptions));
        OnPropertyChanged(nameof(IsLayoutLocked));
        _openSettingsCommand.RaiseCanExecuteChanged();
    }

    internal void RefreshPresentation()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(ValueColor));
        OnPropertyChanged(nameof(BackgroundOpacity));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
