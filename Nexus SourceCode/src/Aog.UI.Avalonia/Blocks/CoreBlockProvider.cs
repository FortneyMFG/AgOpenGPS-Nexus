using System.Collections.Generic;

namespace Aog.UI.Avalonia.Blocks;

/// <summary>
/// Provides the built-in block definitions that ship with the core shell.
/// </summary>
public sealed class CoreBlockProvider : IBlockProvider
{
    public IEnumerable<BlockDefinition> GetBlocks()
    {
        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Menu.FieldSettings"),
            Kind = BlockKind.Container,
            Label = "Field Settings",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "FieldSettings",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.FieldBoundaries"),
            Kind = BlockKind.CommandButton,
            Label = "Field Boundaries",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "FieldSettings",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.HeadlandSetup"),
            Kind = BlockKind.CommandButton,
            Label = "Headland Setup",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "FieldSettings",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.ABLinesMenu"),
            Kind = BlockKind.CommandButton,
            Label = "AB Lines",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "FieldSettings",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.ContourMode"),
            Kind = BlockKind.CommandButton,
            Label = "Contour Mode",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "FieldSettings",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.AutoSteerToggle"),
            Kind = BlockKind.CommandButton,
            Label = "Autosteer",
            IconKey = "autosteer",
            PreferredDock = BlockRegion.Overlay,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.ToggleAutosteer",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.ABLineCycle"),
            Kind = BlockKind.CommandButton,
            Label = "Cycle AB Line",
            IconKey = "ab-lines",
            PreferredDock = BlockRegion.Overlay,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.CycleAbLine",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.UTurnToggle"),
            Kind = BlockKind.CommandButton,
            Label = "U-Turn",
            IconKey = "uturn",
            PreferredDock = BlockRegion.Overlay,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.ToggleUTurn",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.SectionMaster"),
            Kind = BlockKind.CommandButton,
            Label = "Sections",
            IconKey = "sections",
            PreferredDock = BlockRegion.Overlay,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.ToggleSections",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.Start"),
            Kind = BlockKind.CommandButton,
            Label = "Start",
            IconKey = "start",
            PreferredDock = BlockRegion.Overlay,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.StartGuidance",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.Pause"),
            Kind = BlockKind.CommandButton,
            Label = "Pause",
            IconKey = "pause",
            PreferredDock = BlockRegion.Overlay,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.PauseGuidance",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.Stop"),
            Kind = BlockKind.CommandButton,
            Label = "Stop",
            IconKey = "stop",
            PreferredDock = BlockRegion.Overlay,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.StopGuidance",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.NudgeLeft"),
            Kind = BlockKind.CommandButton,
            Label = "Nudge L",
            IconKey = "nudge-left",
            PreferredDock = BlockRegion.Overlay,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.NudgeLeft",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.NudgeRight"),
            Kind = BlockKind.CommandButton,
            Label = "Nudge R",
            IconKey = "nudge-right",
            PreferredDock = BlockRegion.Overlay,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.NudgeRight",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Info.Speed"),
            Kind = BlockKind.Telemetry,
            Label = "Speed",
            PreferredDock = BlockRegion.Left,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Info.Heading"),
            Kind = BlockKind.Telemetry,
            Label = "Heading",
            PreferredDock = BlockRegion.Left,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Info.Altitude"),
            Kind = BlockKind.Telemetry,
            Label = "Altitude",
            PreferredDock = BlockRegion.Left,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Info.Satellites"),
            Kind = BlockKind.Telemetry,
            Label = "Satellites",
            PreferredDock = BlockRegion.Left,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Info.Accuracy"),
            Kind = BlockKind.Telemetry,
            Label = "Accuracy",
            PreferredDock = BlockRegion.Left,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Info.Steering"),
            Kind = BlockKind.Telemetry,
            Label = "Steering",
            PreferredDock = BlockRegion.Right,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Info.Section1"),
            Kind = BlockKind.Telemetry,
            Label = "Section 1",
            PreferredDock = BlockRegion.Right,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Info.Section2"),
            Kind = BlockKind.Telemetry,
            Label = "Section 2",
            PreferredDock = BlockRegion.Right,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Info.Boom"),
            Kind = BlockKind.Telemetry,
            Label = "Boom",
            PreferredDock = BlockRegion.Right,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Info.Rate"),
            Kind = BlockKind.Telemetry,
            Label = "Rate",
            PreferredDock = BlockRegion.Right,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.FieldSettings"),
            Kind = BlockKind.CommandButton,
            Label = "Field Settings",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.Free,
            CommandKey = "Layout.ToggleFieldDock",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.MapTools"),
            Kind = BlockKind.CommandButton,
            Label = "Map Tools",
            IconKey = "map-tools",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.Free,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.Guidance"),
            Kind = BlockKind.CommandButton,
            Label = "Guidance",
            IconKey = "guidance",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.Free,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.Equipment"),
            Kind = BlockKind.CommandButton,
            Label = "Equipment",
            IconKey = "equipment",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.Free,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.Coverage"),
            Kind = BlockKind.CommandButton,
            Label = "Coverage",
            IconKey = "coverage",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.Free,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.Hydraulics"),
            Kind = BlockKind.CommandButton,
            Label = "Hydraulics",
            IconKey = "hydraulics",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.Free,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.AbLines"),
            Kind = BlockKind.CommandButton,
            Label = "AB Lines",
            IconKey = "ab-lines",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.Free,
        };
    }
}
