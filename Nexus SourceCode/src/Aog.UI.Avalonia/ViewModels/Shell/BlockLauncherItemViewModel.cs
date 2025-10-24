using System;
using System.Windows.Input;
using Aog.UI.Avalonia.Blocks;

namespace Aog.UI.Avalonia.ViewModels.Shell;

/// <summary>
/// View-model describing an individual launcher entry that can be activated or reordered.
/// </summary>
public sealed class BlockLauncherItemViewModel
{
    private readonly BlockLayoutViewModel _owner;
    private readonly DelegateCommand _launchCommand;
    private readonly DelegateCommand _moveEarlierCommand;
    private readonly DelegateCommand _moveLaterCommand;

    public BlockLauncherItemViewModel(
        BlockInstance instance,
        BlockDefinition definition,
        string containerId,
        BlockLayoutViewModel owner)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ContainerId = containerId ?? throw new ArgumentNullException(nameof(containerId));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        _launchCommand = new DelegateCommand(_ => _owner.Launch(this), _ => _owner.CanLaunch(this));
        _moveEarlierCommand = new DelegateCommand(_ => _owner.MoveLauncherItem(this, -1), _ => _owner.CanMoveLauncherItem(this, -1));
        _moveLaterCommand = new DelegateCommand(_ => _owner.MoveLauncherItem(this, 1), _ => _owner.CanMoveLauncherItem(this, 1));
    }

    /// <summary>Gets the canonical block instance represented by the launcher.</summary>
    public BlockInstance Instance { get; }

    /// <summary>Gets the block definition providing launcher metadata.</summary>
    public BlockDefinition Definition { get; }

    /// <summary>Gets the container identifier that groups the launcher.</summary>
    public string ContainerId { get; }

    /// <summary>Gets the display label for the launcher entry.</summary>
    public string Label => string.IsNullOrWhiteSpace(Definition.Label) ? Definition.Id.Value : Definition.Label;

    /// <summary>Gets the optional icon resource key.</summary>
    public string? IconKey => Definition.IconKey;

    /// <summary>Gets the command that launches a new block instance from the canonical definition.</summary>
    public ICommand LaunchCommand => _launchCommand;

    /// <summary>Gets the command that moves the launcher earlier in its category ordering.</summary>
    public ICommand MoveEarlierCommand => _moveEarlierCommand;

    /// <summary>Gets the command that moves the launcher later in its category ordering.</summary>
    public ICommand MoveLaterCommand => _moveLaterCommand;

    internal void RefreshStates()
    {
        _launchCommand.RaiseCanExecuteChanged();
        _moveEarlierCommand.RaiseCanExecuteChanged();
        _moveLaterCommand.RaiseCanExecuteChanged();
    }
}

