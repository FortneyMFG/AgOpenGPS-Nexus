using System;
using System.Windows.Input;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.ViewModels.Shell;

/// <summary>
/// View-model representing a rendered block instance within the shell.
/// </summary>
public sealed class BlockItemViewModel
{
    private readonly BlockLayoutViewModel _owner;
    private readonly DelegateCommand _invokeCommand;
    private readonly DelegateCommand _moveUpCommand;
    private readonly DelegateCommand _moveDownCommand;
    private readonly DelegateCommand _deleteCommand;
    private readonly DelegateCommand _moveToLeftCommand;
    private readonly DelegateCommand _moveToRightCommand;
    private readonly DelegateCommand _moveToBottomCommand;
    private readonly DelegateCommand _moveToTopCommand;

    public BlockItemViewModel(BlockInstance instance, BlockDefinition definition, BlockLayoutViewModel owner)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        _invokeCommand = new DelegateCommand(_ => _owner.Invoke(this), _ => _owner.CanInteract(this));
        _moveUpCommand = new DelegateCommand(_ => _owner.MoveWithinRegionByOffset(this, -1), _ => _owner.CanReorder(this));
        _moveDownCommand = new DelegateCommand(_ => _owner.MoveWithinRegionByOffset(this, +1), _ => _owner.CanReorder(this));
        _moveToLeftCommand = new DelegateCommand(_ => _owner.MoveToRegion(this, BlockRegion.Left), _ => _owner.CanMoveTo(this, BlockRegion.Left));
        _moveToRightCommand = new DelegateCommand(_ => _owner.MoveToRegion(this, BlockRegion.Right), _ => _owner.CanMoveTo(this, BlockRegion.Right));
        _moveToBottomCommand = new DelegateCommand(_ => _owner.MoveToRegion(this, BlockRegion.Bottom), _ => _owner.CanMoveTo(this, BlockRegion.Bottom));
        _moveToTopCommand = new DelegateCommand(_ => _owner.MoveToRegion(this, BlockRegion.Top), _ => _owner.CanMoveTo(this, BlockRegion.Top));
        _deleteCommand = new DelegateCommand(_ => _owner.Delete(this), _ => _owner.CanDelete(this));
    }

    /// <summary>Gets the underlying block instance for this tile.</summary>
    public BlockInstance Instance { get; }

    /// <summary>Gets the block definition metadata driving rendering and behavior.</summary>
    public BlockDefinition Definition { get; }

    /// <summary>Gets the user visible label for the block.</summary>
    public string Label => string.IsNullOrWhiteSpace(Definition.Label) ? Definition.Id.Value : Definition.Label;

    /// <summary>Gets the optional icon asset key.</summary>
    public string? IconKey => Definition.IconKey;

    /// <summary>Gets the command triggered when the block is activated.</summary>
    public ICommand InvokeCommand => _invokeCommand;

    /// <summary>Gets the command that moves the block earlier within its region.</summary>
    public ICommand MoveUpCommand => _moveUpCommand;

    /// <summary>Gets the command that moves the block later within its region.</summary>
    public ICommand MoveDownCommand => _moveDownCommand;

    /// <summary>Gets the command that removes the block when permitted.</summary>
    public ICommand DeleteCommand => _deleteCommand;

    /// <summary>Gets the command that moves the block into the left region.</summary>
    public ICommand MoveToLeftCommand => _moveToLeftCommand;

    /// <summary>Gets the command that moves the block into the right region.</summary>
    public ICommand MoveToRightCommand => _moveToRightCommand;

    /// <summary>Gets the command that moves the block into the bottom region.</summary>
    public ICommand MoveToBottomCommand => _moveToBottomCommand;

    /// <summary>Gets the command that moves the block into the top region.</summary>
    public ICommand MoveToTopCommand => _moveToTopCommand;

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
    internal void RefreshCommandStates()
    {
        _invokeCommand.RaiseCanExecuteChanged();
        _moveUpCommand.RaiseCanExecuteChanged();
        _moveDownCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
        _moveToLeftCommand.RaiseCanExecuteChanged();
        _moveToRightCommand.RaiseCanExecuteChanged();
        _moveToBottomCommand.RaiseCanExecuteChanged();
        _moveToTopCommand.RaiseCanExecuteChanged();
    }
}
