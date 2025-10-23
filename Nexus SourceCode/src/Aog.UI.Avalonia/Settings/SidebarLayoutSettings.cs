using System;

namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Describes the sizing policy applied to a sidebar grid.
/// </summary>
public enum LayoutDimensionMode
{
    Dynamic,
    Fixed,
}

/// <summary>
/// Stores layout preferences for a sidebar block surface.
/// </summary>
public sealed class SidebarLayoutSettings
{
    /// <summary>Gets or sets the mode controlling the sidebar width.</summary>
    public LayoutDimensionMode WidthMode { get; set; } = LayoutDimensionMode.Fixed;

    /// <summary>Gets or sets the number of block columns when the width is fixed.</summary>
    public double BlockColumns { get; set; } = 1d;

    /// <summary>Gets or sets the mode controlling the sidebar height.</summary>
    public LayoutDimensionMode HeightMode { get; set; } = LayoutDimensionMode.Dynamic;

    /// <summary>Gets or sets the number of block rows when the height is fixed.</summary>
    public double BlockRows { get; set; } = 0d;

    /// <summary>Gets or sets the base size of a full block tile in pixels.</summary>
    public double BlockSize { get; set; } = 112d;

    /// <summary>Gets or sets the spacing between tiles in pixels.</summary>
    public double Spacing { get; set; } = 8d;

    /// <summary>
    /// Creates a deep copy of the settings instance.
    /// </summary>
    public SidebarLayoutSettings Clone()
    {
        return new SidebarLayoutSettings
        {
            WidthMode = WidthMode,
            BlockColumns = BlockColumns,
            HeightMode = HeightMode,
            BlockRows = BlockRows,
            BlockSize = BlockSize,
            Spacing = Spacing,
        };
    }

    /// <summary>Creates default settings for vertical sidebars.</summary>
    public static SidebarLayoutSettings CreateVerticalDefaults()
    {
        return new SidebarLayoutSettings
        {
            WidthMode = LayoutDimensionMode.Fixed,
            BlockColumns = 1d,
            HeightMode = LayoutDimensionMode.Dynamic,
            BlockRows = 10d,
            BlockSize = 112d,
            Spacing = 8d,
        };
    }

    /// <summary>Creates default settings for the bottom toolbar.</summary>
    public static SidebarLayoutSettings CreateBottomDefaults()
    {
        return new SidebarLayoutSettings
        {
            WidthMode = LayoutDimensionMode.Dynamic,
            BlockColumns = 8d,
            HeightMode = LayoutDimensionMode.Fixed,
            BlockRows = 1d,
            BlockSize = 112d,
            Spacing = 8d,
        };
    }

    /// <summary>Creates default settings for the top telemetry strip.</summary>
    public static SidebarLayoutSettings CreateTopDefaults()
    {
        return new SidebarLayoutSettings
        {
            WidthMode = LayoutDimensionMode.Dynamic,
            BlockColumns = 6d,
            HeightMode = LayoutDimensionMode.Fixed,
            BlockRows = 0.5d,
            BlockSize = 112d,
            Spacing = 8d,
        };
    }

    /// <summary>Creates default settings for the central workspace grid.</summary>
    public static SidebarLayoutSettings CreateWorkspaceDefaults()
    {
        return new SidebarLayoutSettings
        {
            WidthMode = LayoutDimensionMode.Dynamic,
            BlockColumns = 6d,
            HeightMode = LayoutDimensionMode.Dynamic,
            BlockRows = 6d,
            BlockSize = 112d,
            Spacing = 8d,
        };
    }
}
