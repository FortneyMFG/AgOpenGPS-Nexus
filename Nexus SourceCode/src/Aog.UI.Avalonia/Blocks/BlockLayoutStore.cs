using System;
using System.Collections.Generic;
using System.Linq;
using Aog.UI.Avalonia.Settings;

namespace Aog.UI.Avalonia.Blocks;

/// <summary>
/// Provides persistence helpers for the block layout stored in <see cref="ShellLayoutPreferences"/>.
/// </summary>
public interface IBlockLayoutStore
{
    /// <summary>Loads the persisted block instances, seeding defaults when required.</summary>
    IReadOnlyList<BlockInstance> Load();

    /// <summary>Persists the supplied instances replacing the current layout.</summary>
    void Save(IEnumerable<BlockInstance> instances);
}

/// <summary>
/// Coordinates block layout persistence using <see cref="IUiPreferencesService"/>.
/// </summary>
public sealed class BlockLayoutStore : IBlockLayoutStore
{
    private readonly IUiPreferencesService _preferencesService;
    private readonly IBlockCatalog _catalog;

    public BlockLayoutStore(IUiPreferencesService preferencesService, IBlockCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(preferencesService);
        ArgumentNullException.ThrowIfNull(catalog);
        _preferencesService = preferencesService;
        _catalog = catalog;
    }

    public IReadOnlyList<BlockInstance> Load()
    {
        var preferences = _preferencesService.GetPreferences();
        var layout = preferences.ShellLayout;
        EnsureInstanceList(layout);

        if (layout.Instances.Count == 0)
        {
            SeedDefaultLayout(layout);
            _preferencesService.UpdateShellLayout(layout);
        }

        return layout.Instances.Select(instance => instance.Clone()).ToArray();
    }

    public void Save(IEnumerable<BlockInstance> instances)
    {
        ArgumentNullException.ThrowIfNull(instances);

        var layout = _preferencesService.GetPreferences().ShellLayout;
        EnsureInstanceList(layout);

        layout.Instances.Clear();
        layout.Instances.AddRange(instances.Select(instance => instance.Clone()));
        _preferencesService.UpdateShellLayout(layout);
    }

    private static void EnsureInstanceList(ShellLayoutPreferences layout)
    {
        if (layout.Instances is null)
        {
            layout.Instances = new List<BlockInstance>();
        }
    }

    private void SeedDefaultLayout(ShellLayoutPreferences layout)
    {
        var order = 0;
        foreach (var definition in _catalog.All())
        {
            if (definition.Placement != PlacementPolicy.MenuScoped || string.IsNullOrWhiteSpace(definition.ContainerId))
            {
                continue;
            }

            if (layout.Instances.Any(i => i.DefinitionId == definition.Id && i.Origin == BlockOrigin.Canonical))
            {
                continue;
            }

            layout.Instances.Add(new BlockInstance
            {
                DefinitionId = definition.Id,
                Region = BlockRegion.Floating,
                Order = order++,
                Origin = BlockOrigin.Canonical,
                ContainerId = definition.ContainerId,
            });
        }

        SeedClone(layout, "Cmd.AutoSteerToggle", BlockRegion.Right, 0);
        SeedClone(layout, "Cmd.UTurnToggle", BlockRegion.Right, 1);
        SeedClone(layout, "Cmd.SectionMaster", BlockRegion.Right, 2);
        SeedClone(layout, "Cmd.Start", BlockRegion.Bottom, 0);
        SeedClone(layout, "Cmd.Pause", BlockRegion.Bottom, 1);
        SeedClone(layout, "Cmd.Stop", BlockRegion.Bottom, 2);
        SeedClone(layout, "Cmd.NudgeLeft", BlockRegion.Bottom, 3);
        SeedClone(layout, "Cmd.NudgeRight", BlockRegion.Bottom, 4);
        SeedClone(layout, "Tel.Speed", BlockRegion.Top, 0);
    }

    private void SeedClone(ShellLayoutPreferences layout, string definitionId, BlockRegion region, int order)
    {
        var id = new BlockDefinitionId(definitionId);
        if (_catalog.Get(id) is null)
        {
            return;
        }

        if (layout.Instances.Any(i => i.DefinitionId == id && i.Origin == BlockOrigin.Clone && i.Region == region))
        {
            return;
        }

        layout.Instances.Add(new BlockInstance
        {
            DefinitionId = id,
            Region = region,
            Order = order,
            Origin = BlockOrigin.Clone,
        });
    }
}
