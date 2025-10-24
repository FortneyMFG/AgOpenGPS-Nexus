using System;
using System.Collections.ObjectModel;
using Aog.UI.Avalonia.Blocks;

namespace Aog.UI.Avalonia.ViewModels.Shell;

/// <summary>
/// Represents a grouping of launcher entries displayed within the field settings dock.
/// </summary>
public sealed class BlockLauncherCategoryViewModel
{
    public BlockLauncherCategoryViewModel(
        string id,
        string title,
        BlockInstance containerInstance,
        BlockLayoutViewModel owner)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = string.IsNullOrWhiteSpace(title) ? id : title;
        ContainerInstance = containerInstance ?? throw new ArgumentNullException(nameof(containerInstance));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Blocks = new ObservableCollection<BlockLauncherItemViewModel>();
    }

    /// <summary>Gets the identifier shared by launcher entries that belong to the category.</summary>
    public string Id { get; }

    /// <summary>Gets the user-visible category title.</summary>
    public string Title { get; }

    /// <summary>Gets the canonical block instance representing the category container.</summary>
    public BlockInstance ContainerInstance { get; }

    /// <summary>Gets the owning layout view-model.</summary>
    internal BlockLayoutViewModel Owner { get; }

    /// <summary>Gets the launcher entries contained in the category.</summary>
    public ObservableCollection<BlockLauncherItemViewModel> Blocks { get; }

    /// <summary>Gets a value indicating whether the category contains any launcher entries.</summary>
    public bool HasBlocks => Blocks.Count > 0;

    internal void RefreshStates()
    {
        foreach (var block in Blocks)
        {
            block.RefreshStates();
        }
    }
}

