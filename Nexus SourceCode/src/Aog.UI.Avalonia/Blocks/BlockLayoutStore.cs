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
        changed |= EnsureClone(layout, "Cmd.FieldSettings", BlockRegion.Left, 0, BlockSize.Tile1x2);
        changed |= EnsureFloatingShortcut(layout, "Cmd.AutoSteerToggle", 0, 48, 48);
        changed |= EnsureFloatingShortcut(layout, "Cmd.ABLineCycle", 1, 232, 48);
        changed |= EnsureFloatingShortcut(layout, "Cmd.UTurnToggle", 2, 416, 48);
        changed |= EnsureFloatingShortcut(layout, "Cmd.SectionMaster", 3, 600, 48);
        changed |= EnsureFloatingShortcut(layout, "Cmd.Start", 4, 48, 216);
        changed |= EnsureFloatingShortcut(layout, "Cmd.Pause", 5, 232, 216);
        changed |= EnsureFloatingShortcut(layout, "Cmd.Stop", 6, 416, 216);
        changed |= EnsureFloatingShortcut(layout, "Cmd.NudgeLeft", 7, 232, 384);
        changed |= EnsureFloatingShortcut(layout, "Cmd.NudgeRight", 8, 416, 384);
        changed |= EnsureClone(layout, "Tel.Speed", BlockRegion.Floating, 0, BlockSize.Tile1xHalf);
        changed |= EnsureClone(layout, "Tel.Gps", BlockRegion.Floating, 1, BlockSize.Tile1xHalf);
        changed |= EnsureClone(layout, "Tel.Sections", BlockRegion.Floating, 2, BlockSize.Tile1xHalf);
        changed |= EnsureClone(layout, "Tel.Radio", BlockRegion.Floating, 3, BlockSize.Tile1xHalf);
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

            return updated;
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

    private bool EnsureFloatingShortcut(
        ShellLayoutPreferences layout,
        string definitionId,
        int order,
        double x,
        double y)
    {
        var id = new BlockDefinitionId(definitionId);
        if (_catalog.Get(id) is null)
        {
            return false;
        }

        var changed = false;
        var instance = layout.Instances.FirstOrDefault(i =>
            i.DefinitionId == id && i.Origin == BlockOrigin.Clone);
        if (instance is null)
        {
            instance = new BlockInstance
            {
                DefinitionId = id,
                Region = BlockRegion.Overlay,
                Order = order,
                Origin = BlockOrigin.Clone,
            };
            layout.Instances.Add(instance);
            changed = true;
        }
        else
        {
            if (instance.Region != BlockRegion.Overlay)
            {
                instance.Region = BlockRegion.Overlay;
                changed = true;
            }

            if (instance.Order != order)
            {
                instance.Order = order;
                changed = true;
            }
        }

        var specs = layout.Grid?.FloatingBlocks;
        if (specs is null)
        {
            layout.Grid ??= new ShellGridLayout();
            specs = layout.Grid.FloatingBlocks ??= new List<FloatingBlockSpec>();
        }

        var spec = specs.FirstOrDefault(s => s.InstanceId == instance.InstanceId.Value);
        if (spec is null)
        {
            specs.Add(new FloatingBlockSpec
            {
                InstanceId = instance.InstanceId.Value,
                X = x,
                Y = y,
            });
            changed = true;
        }
        else
        {
            const double epsilon = 0.5d;
            if (Math.Abs(spec.X - x) > epsilon || Math.Abs(spec.Y - y) > epsilon)
            {
                spec.X = x;
                spec.Y = y;
                changed = true;
            }
        }

        return changed;
    }
}

