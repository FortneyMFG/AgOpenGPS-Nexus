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
        EnsureSidebarSettings(layout);
        EnsureInstanceList(layout);

        var changed = RemoveMissingDefinitions(layout);
        changed |= EnsureMenuScopedCanonicals(layout);
        changed |= EnsureDefaultClones(layout);

        if (changed)
        {
            _preferencesService.UpdateShellLayout(layout);
        }

        return layout.Instances.Select(instance => instance.Clone()).ToArray();
    }

    public void Save(IEnumerable<BlockInstance> instances)
    {
        ArgumentNullException.ThrowIfNull(instances);

        var layout = _preferencesService.GetPreferences().ShellLayout;
        EnsureSidebarSettings(layout);
        EnsureInstanceList(layout);

        layout.Instances.Clear();
        layout.Instances.AddRange(instances.Select(instance => instance.Clone()));
        _preferencesService.UpdateShellLayout(layout);
    }

    private static void EnsureSidebarSettings(ShellLayoutPreferences layout)
    {
        layout.LeftSidebar ??= SidebarLayoutSettings.CreateVerticalDefaults();
        layout.RightSidebar ??= SidebarLayoutSettings.CreateVerticalDefaults();
        layout.TopSidebar ??= SidebarLayoutSettings.CreateTopDefaults();
        layout.BottomSidebar ??= SidebarLayoutSettings.CreateBottomDefaults();
        layout.Workspace ??= SidebarLayoutSettings.CreateWorkspaceDefaults();
    }

    private static void EnsureInstanceList(ShellLayoutPreferences layout)
    {
        if (layout.Instances is null)
        {
            layout.Instances = new List<BlockInstance>();
        }
    }

    private bool RemoveMissingDefinitions(ShellLayoutPreferences layout)
    {
        var removed = layout.Instances.RemoveAll(instance =>
            instance is null
            || instance.DefinitionId is null
            || string.IsNullOrWhiteSpace(instance.DefinitionId.Value)
            || _catalog.Get(instance.DefinitionId) is null);

        return removed > 0;
    }

    private bool EnsureMenuScopedCanonicals(ShellLayoutPreferences layout)
    {
        var changed = false;
        var nextOrder = layout.Instances
            .Where(i => i.Origin == BlockOrigin.Canonical && i.Region == BlockRegion.Floating)
            .Select(i => i.Order)
            .DefaultIfEmpty(-1)
            .Max() + 1;

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
                Order = nextOrder++,
                Origin = BlockOrigin.Canonical,
                ContainerId = definition.ContainerId,
            });
            changed = true;
        }

        return changed;
    }

    private bool EnsureDefaultClones(ShellLayoutPreferences layout)
    {
        var changed = false;
        changed |= EnsureClone(layout, "Cmd.MapTools", BlockRegion.Left, 0);
        changed |= EnsureClone(layout, "Cmd.Guidance", BlockRegion.Left, 1);
        changed |= EnsureClone(layout, "Cmd.Equipment", BlockRegion.Left, 2);
        changed |= EnsureClone(layout, "Cmd.Coverage", BlockRegion.Left, 3);
        changed |= EnsureClone(layout, "Cmd.Hydraulics", BlockRegion.Left, 4);
        changed |= EnsureClone(layout, "Cmd.AbLines", BlockRegion.Left, 5);
        changed |= EnsureClone(layout, "Cmd.AutoSteerToggle", BlockRegion.Right, 0);
        changed |= EnsureClone(layout, "Cmd.UTurnToggle", BlockRegion.Right, 1);
        changed |= EnsureClone(layout, "Cmd.SectionMaster", BlockRegion.Right, 2);
        changed |= EnsureClone(layout, "Cmd.Start", BlockRegion.Bottom, 0);
        changed |= EnsureClone(layout, "Cmd.Pause", BlockRegion.Bottom, 1);
        changed |= EnsureClone(layout, "Cmd.Stop", BlockRegion.Bottom, 2);
        changed |= EnsureClone(layout, "Cmd.NudgeLeft", BlockRegion.Bottom, 3);
        changed |= EnsureClone(layout, "Cmd.NudgeRight", BlockRegion.Bottom, 4);
        changed |= EnsureClone(layout, "Tel.Speed", BlockRegion.Top, 0, BlockSize.Tile1xHalf);
        changed |= EnsureClone(layout, "Tel.Gps", BlockRegion.Top, 1, BlockSize.Tile1xHalf);
        changed |= EnsureClone(layout, "Tel.Sections", BlockRegion.Top, 2, BlockSize.Tile1xHalf);
        changed |= EnsureClone(layout, "Tel.Radio", BlockRegion.Top, 3, BlockSize.Tile1xHalf);
        return changed;
    }

    private bool EnsureClone(
        ShellLayoutPreferences layout,
        string definitionId,
        BlockRegion region,
        int order,
        BlockSize? sizeOverride = null)
    {
        var id = new BlockDefinitionId(definitionId);
        if (_catalog.Get(id) is null)
        {
            return false;
        }

        if (layout.Instances.Any(i => i.DefinitionId == id && i.Origin == BlockOrigin.Clone))
        {
            return false;
        }

        layout.Instances.Add(new BlockInstance
        {
            DefinitionId = id,
            Region = region,
            Order = order,
            Origin = BlockOrigin.Clone,
            SizeOverride = sizeOverride,
        });
        return true;
    }
}

