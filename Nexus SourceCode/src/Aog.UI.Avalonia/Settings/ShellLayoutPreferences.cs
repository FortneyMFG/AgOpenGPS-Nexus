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

    /// <summary>Gets or sets whether the right sidebar panels are visible.</summary>
    public bool ShowRightSidebar { get; set; } = true;

    /// <summary>Gets or sets the last active workspace identifier.</summary>
    public string ActiveWorkspaceId { get; set; } = "workspace.main";

    /// <summary>Gets or sets the persisted block layout instances.</summary>
    public List<BlockInstance> Instances { get; set; } = new();

    /// <summary>Gets or sets the layout metrics for the left sidebar.</summary>
    public SidebarLayoutSettings LeftSidebar { get; set; } = SidebarLayoutSettings.CreateVerticalDefaults();

    /// <summary>Gets or sets the layout metrics for the right sidebar.</summary>
    public SidebarLayoutSettings RightSidebar { get; set; } = SidebarLayoutSettings.CreateVerticalDefaults();

    /// <summary>Gets or sets the layout metrics for the bottom toolbar.</summary>
    public SidebarLayoutSettings BottomSidebar { get; set; } = SidebarLayoutSettings.CreateBottomDefaults();

    /// <summary>Gets or sets the layout metrics for the top telemetry strip.</summary>
    public SidebarLayoutSettings TopSidebar { get; set; } = SidebarLayoutSettings.CreateTopDefaults();

    /// <summary>Gets or sets the layout metrics for the central workspace grid.</summary>
    public SidebarLayoutSettings WorkspaceGrid { get; set; } = SidebarLayoutSettings.CreateWorkspaceDefaults();

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

    /// <summary>Creates a deep copy of the layout preferences.</summary>
    public ShellLayoutPreferences Clone()
    {
        var clone = new ShellLayoutPreferences
        {
            ShowTopToolbar = ShowTopToolbar,
            ShowRightSidebar = ShowRightSidebar,
            ActiveWorkspaceId = ActiveWorkspaceId,
            LeftSidebar = (LeftSidebar ?? SidebarLayoutSettings.CreateVerticalDefaults()).Clone(),
            RightSidebar = (RightSidebar ?? SidebarLayoutSettings.CreateVerticalDefaults()).Clone(),
            BottomSidebar = (BottomSidebar ?? SidebarLayoutSettings.CreateBottomDefaults()).Clone(),
            TopSidebar = (TopSidebar ?? SidebarLayoutSettings.CreateTopDefaults()).Clone(),
            WorkspaceGrid = (WorkspaceGrid ?? SidebarLayoutSettings.CreateWorkspaceDefaults()).Clone(),
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
}
