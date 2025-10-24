using System.Collections.Generic;
using System.Text.Json.Serialization;
using Aog.UI.Avalonia.Blocks;

namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Stores persisted layout preferences for the Nexus shell.
/// </summary>
public sealed class ShellLayoutPreferences
{
    /// <summary>Gets or sets whether the top toolbar is visible.</summary>
    public bool ShowTopToolbar { get; set; } = true;

    /// <summary>Gets or sets whether the left sidebar is visible.</summary>
    public bool ShowLeftSidebar { get; set; } = true;

    /// <summary>Gets or sets whether the right sidebar panels are visible.</summary>
    public bool ShowRightSidebar { get; set; } = true;

    /// <summary>Gets or sets whether the top sidebar strip is visible.</summary>
    public bool ShowTopSidebar { get; set; } = true;

    /// <summary>Gets or sets whether the bottom sidebar strip is visible.</summary>
    public bool ShowBottomSidebar { get; set; } = true;

    /// <summary>Gets or sets whether the shell layout is locked for modifications.</summary>
    public bool IsLayoutLocked { get; set; } = true;

    /// <summary>Gets or sets the last active workspace identifier.</summary>
    public string ActiveWorkspaceId { get; set; } = "workspace.main";

    /// <summary>Gets or sets the persisted block layout instances.</summary>
    public List<BlockInstance> Instances { get; set; } = new();

    /// <summary>Gets or sets the global grid layout definition.</summary>
    public Layout.ShellGridLayout Grid { get; set; } = new();

    /// <summary>Gets or sets the layout settings for the left sidebar.</summary>
    public SidebarLayoutSettings LeftSidebar { get; set; } = SidebarLayoutSettings.CreateVerticalDefaults();

    /// <summary>Gets or sets the layout settings for the right sidebar.</summary>
    public SidebarLayoutSettings RightSidebar { get; set; } = SidebarLayoutSettings.CreateVerticalDefaults();

    /// <summary>Gets or sets the layout settings for the top sidebar strip.</summary>
    public SidebarLayoutSettings TopSidebar { get; set; } = SidebarLayoutSettings.CreateTopDefaults();

    /// <summary>Gets or sets the layout settings for the bottom sidebar strip.</summary>
    public SidebarLayoutSettings BottomSidebar { get; set; } = SidebarLayoutSettings.CreateBottomDefaults();

    /// <summary>Gets or sets the layout settings for the central workspace.</summary>
    public SidebarLayoutSettings Workspace { get; set; } = SidebarLayoutSettings.CreateWorkspaceDefaults();

    /// <summary>
    /// Legacy alias maintained for compatibility with existing bindings.
    /// </summary>
    [JsonIgnore]
    public bool IsTopToolbarVisible
    {
        get => ShowTopToolbar;
        set => ShowTopToolbar = value;
    }

    /// <summary>
    /// Legacy alias maintained for compatibility with existing bindings.
    /// </summary>
    [JsonIgnore]
    public bool IsRightSidebarVisible
    {
        get => ShowRightSidebar;
        set => ShowRightSidebar = value;
    }

    /// <summary>
    /// Legacy alias maintained for compatibility with existing bindings.
    /// </summary>
    [JsonIgnore]
    public bool IsLeftSidebarVisible
    {
        get => ShowLeftSidebar;
        set => ShowLeftSidebar = value;
    }

    /// <summary>
    /// Legacy alias maintained for compatibility with existing bindings.
    /// </summary>
    [JsonIgnore]
    public bool IsTopSidebarVisible
    {
        get => ShowTopSidebar;
        set => ShowTopSidebar = value;
    }

    /// <summary>
    /// Legacy alias maintained for compatibility with existing bindings.
    /// </summary>
    [JsonIgnore]
    public bool IsBottomSidebarVisible
    {
        get => ShowBottomSidebar;
        set => ShowBottomSidebar = value;
    }

    /// <summary>Creates a deep copy of the layout preferences.</summary>
    public ShellLayoutPreferences Clone()
    {
        var clone = new ShellLayoutPreferences
        {
            ShowTopToolbar = ShowTopToolbar,
            ShowLeftSidebar = ShowLeftSidebar,
            ShowRightSidebar = ShowRightSidebar,
            ShowTopSidebar = ShowTopSidebar,
            ShowBottomSidebar = ShowBottomSidebar,
            IsLayoutLocked = IsLayoutLocked,
            ActiveWorkspaceId = ActiveWorkspaceId,
            Grid = new Layout.ShellGridLayout
            {
                CellPx = Grid?.CellPx ?? 56,
                GutterPx = Grid?.GutterPx ?? 8,
                Columns = Grid?.Columns ?? 1,
                Rows = Grid?.Rows ?? 1,
                RootPane = ClonePane(Grid?.RootPane),
                Tiles = Grid?.Tiles is { Count: > 0 } tiles ? CloneTiles(tiles) : new List<Layout.TileSpec>(),
                Panels = Grid?.Panels is { Count: > 0 } panels ? ClonePanels(panels) : new List<Layout.PanelSpec>(),
                FloatingPanels = Grid?.FloatingPanels is { Count: > 0 } floatingPanels ? CloneFloatingPanels(floatingPanels) : new List<Layout.FloatingPanelSpec>(),
                FloatingBlocks = Grid?.FloatingBlocks is { Count: > 0 } floatingBlocks ? CloneFloatingBlocks(floatingBlocks) : new List<Layout.FloatingBlockSpec>(),
            },
            LeftSidebar = (LeftSidebar ?? SidebarLayoutSettings.CreateVerticalDefaults()).Clone(),
            RightSidebar = (RightSidebar ?? SidebarLayoutSettings.CreateVerticalDefaults()).Clone(),
            TopSidebar = (TopSidebar ?? SidebarLayoutSettings.CreateTopDefaults()).Clone(),
            BottomSidebar = (BottomSidebar ?? SidebarLayoutSettings.CreateBottomDefaults()).Clone(),
            Workspace = (Workspace ?? SidebarLayoutSettings.CreateWorkspaceDefaults()).Clone(),
        };

        if (Instances.Count > 0)
        {
            foreach (var instance in Instances)
            {
                clone.Instances.Add(instance.Clone());
            }
        }

        return clone;
    }

    private static Layout.PaneNode? ClonePane(Layout.PaneNode? node)
    {
        return node switch
        {
            Layout.SplitPane split => new Layout.SplitPane
            {
                Id = split.Id,
                Orientation = split.Orientation,
                Ratios = split.Ratios is { Length: > 0 } ratios ? (double[])ratios.Clone() : new[] { 1.0 },
                Children = split.Children.ConvertAll(child => ClonePane(child) ?? new Layout.LeafPane()),
            },
            Layout.LeafPane leaf => new Layout.LeafPane
            {
                Id = leaf.Id,
                Kind = leaf.Kind,
                PluginId = leaf.PluginId,
            },
            _ => null,
        };
    }

    private static List<Layout.TileSpec> CloneTiles(IEnumerable<Layout.TileSpec> tiles)
    {
        var clone = new List<Layout.TileSpec>();
        foreach (var tile in tiles)
        {
            clone.Add(new Layout.TileSpec
            {
                Id = tile.Id,
                Row = tile.Row,
                Col = tile.Col,
                RowSpan = tile.RowSpan,
                ColSpan = tile.ColSpan,
                PaneAttached = tile.PaneAttached,
                PaneId = tile.PaneId,
                Anchor = tile.Anchor,
                Offset = tile.Offset,
            });
        }

        return clone;
    }

    private static List<Layout.PanelSpec> ClonePanels(IEnumerable<Layout.PanelSpec> panels)
    {
        var clone = new List<Layout.PanelSpec>();
        foreach (var panel in panels)
        {
            if (panel is null)
            {
                continue;
            }

            clone.Add(new Layout.PanelSpec
            {
                Id = panel.Id,
                Left = panel.Left,
                Bottom = panel.Bottom,
                Right = panel.Right,
                Top = panel.Top,
                LeftUsesGridSize = panel.LeftUsesGridSize,
                BottomUsesGridSize = panel.BottomUsesGridSize,
                RightUsesGridSize = panel.RightUsesGridSize,
                TopUsesGridSize = panel.TopUsesGridSize,
                Anchor = panel.Anchor,
                Offset = panel.Offset,
            });
        }

        return clone;
    }

    private static List<Layout.FloatingPanelSpec> CloneFloatingPanels(IEnumerable<Layout.FloatingPanelSpec> panels)
    {
        var clone = new List<Layout.FloatingPanelSpec>();
        foreach (var panel in panels)
        {
            if (panel is null)
            {
                continue;
            }

            clone.Add(new Layout.FloatingPanelSpec
            {
                Id = panel.Id,
                Title = panel.Title,
                ContentId = panel.ContentId,
                X = panel.X,
                Y = panel.Y,
                Width = panel.Width,
                Height = panel.Height,
                IsLocked = panel.IsLocked,
            });
        }

        return clone;
    }

    private static List<Layout.FloatingBlockSpec> CloneFloatingBlocks(IEnumerable<Layout.FloatingBlockSpec> blocks)
    {
        var clone = new List<Layout.FloatingBlockSpec>();
        foreach (var block in blocks)
        {
            if (block is null)
            {
                continue;
            }

            clone.Add(new Layout.FloatingBlockSpec
            {
                Id = block.Id,
                InstanceId = block.InstanceId,
                X = block.X,
                Y = block.Y,
                Width = block.Width,
                Height = block.Height,
            });
        }

        return clone;
    }
}
