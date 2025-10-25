using System;
using System.Collections.Generic;
using System.Linq;
using Aog.UI.Avalonia.Layout;
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
        EnsureFloatingCollections(layout);

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
        EnsureFloatingCollections(layout);

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

    private static void EnsureFloatingCollections(ShellLayoutPreferences layout)
    {
        layout.Grid ??= new ShellGridLayout();
        layout.Grid.FloatingBlocks ??= new List<FloatingBlockSpec>();
        layout.Grid.FloatingPanels ??= new List<FloatingPanelSpec>();
    }

    private bool RemoveMissingDefinitions(ShellLayoutPreferences layout)
    {
        var removed = layout.Instances.RemoveAll(instance =>
            instance is null
            || instance.DefinitionId is null
            || string.IsNullOrWhiteSpace(instance.DefinitionId.Value)
            || _catalog.Get(instance.DefinitionId) is null);

        if (removed > 0)
        {
            var existing = new HashSet<Guid>(layout.Instances.Select(i => i.InstanceId.Value));
            layout.Grid?.FloatingBlocks?.RemoveAll(block => !existing.Contains(block.InstanceId));
        }

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
        changed |= EnsureClone(layout, "Cmd.FieldSettings", BlockRegion.Left, 5, BlockSize.Tile1x1);
        changed |= EnsureClone(layout, "Info.Speed", BlockRegion.Left, 0, null, "12.5 km/h");
        changed |= EnsureClone(layout, "Info.Heading", BlockRegion.Left, 1, null, "N 45°");
        changed |= EnsureClone(layout, "Info.Altitude", BlockRegion.Left, 2, null, "342 m");
        changed |= EnsureClone(layout, "Info.Satellites", BlockRegion.Left, 3, null, "12/16");
        changed |= EnsureClone(layout, "Info.Accuracy", BlockRegion.Left, 4, null, "±2 cm");
        changed |= EnsureClone(layout, "Info.Steering", BlockRegion.Right, 0, null, "Auto");
        changed |= EnsureClone(layout, "Info.Section1", BlockRegion.Right, 1, null, "ON");
        changed |= EnsureClone(layout, "Info.Section2", BlockRegion.Right, 2, null, "OFF");
        changed |= EnsureClone(layout, "Info.Boom", BlockRegion.Right, 3, null, "Up");
        changed |= EnsureClone(layout, "Info.Rate", BlockRegion.Right, 4, null, "150 L/Ha");
        changed |= EnsureClone(layout, "Cmd.AutoSteerToggle", BlockRegion.Bottom, 0);
        changed |= EnsureClone(layout, "Cmd.ABLineCycle", BlockRegion.Bottom, 1);
        changed |= EnsureClone(layout, "Cmd.UTurnToggle", BlockRegion.Bottom, 2);
        changed |= EnsureClone(layout, "Cmd.SectionMaster", BlockRegion.Bottom, 3);
        changed |= EnsureClone(layout, "Cmd.Start", BlockRegion.Bottom, 4);
        changed |= EnsureClone(layout, "Cmd.Pause", BlockRegion.Bottom, 5);
        changed |= EnsureClone(layout, "Cmd.Stop", BlockRegion.Bottom, 6);
        changed |= EnsureClone(layout, "Cmd.NudgeLeft", BlockRegion.Bottom, 7);
        changed |= EnsureClone(layout, "Cmd.NudgeRight", BlockRegion.Bottom, 8);
        changed |= RemoveFloatingSpec(layout, "Cmd.AutoSteerToggle");
        changed |= RemoveFloatingSpec(layout, "Cmd.ABLineCycle");
        changed |= RemoveFloatingSpec(layout, "Cmd.UTurnToggle");
        changed |= RemoveFloatingSpec(layout, "Cmd.SectionMaster");
        changed |= RemoveFloatingSpec(layout, "Cmd.Start");
        changed |= RemoveFloatingSpec(layout, "Cmd.Pause");
        changed |= RemoveFloatingSpec(layout, "Cmd.Stop");
        changed |= RemoveFloatingSpec(layout, "Cmd.NudgeLeft");
        changed |= RemoveFloatingSpec(layout, "Cmd.NudgeRight");
        return changed;
    }

    private bool EnsureClone(
        ShellLayoutPreferences layout,
        string definitionId,
        BlockRegion region,
        int order,
        BlockSize? sizeOverride = null,
        string? groupKey = null)
    {
        var id = new BlockDefinitionId(definitionId);
        if (_catalog.Get(id) is null)
        {
            return false;
        }

        var existing = layout.Instances.FirstOrDefault(i => i.DefinitionId == id && i.Origin == BlockOrigin.Clone);
        if (existing is not null)
        {
            var updated = false;
            if (existing.Region != region)
            {
                existing.Region = region;
                updated = true;
            }

            if (existing.Order != order)
            {
                existing.Order = order;
                updated = true;
            }

            if (existing.SizeOverride != sizeOverride)
            {
                existing.SizeOverride = sizeOverride;
                updated = true;
            }

            if (!string.Equals(existing.GroupKey, groupKey, StringComparison.Ordinal))
            {
                existing.GroupKey = groupKey;
                updated = true;
            }

            return updated;
        }

        layout.Instances.Add(new BlockInstance
        {
            DefinitionId = id,
            Region = region,
            Order = order,
            SizeOverride = sizeOverride,
            Origin = BlockOrigin.Clone,
            GroupKey = groupKey,
        });
        return true;
    }

    private bool RemoveFloatingSpec(ShellLayoutPreferences layout, string definitionId)
    {
        var id = new BlockDefinitionId(definitionId);
        var instance = layout.Instances.FirstOrDefault(i => i.DefinitionId == id && i.Origin == BlockOrigin.Clone);
        if (instance is null)
        {
            return false;
        }

        if (layout.Grid?.FloatingBlocks is not { Count: > 0 } specs)
        {
            return false;
        }

        var removed = specs.RemoveAll(spec => spec.InstanceId == instance.InstanceId.Value);
        return removed > 0;
    }

}

