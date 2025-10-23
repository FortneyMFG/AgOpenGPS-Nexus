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
            Id = new BlockDefinitionId("Menu.SystemSettings"),
            Kind = BlockKind.Container,
            Label = "System Settings",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "SystemSettings",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Menu.FarmSettings"),
            Kind = BlockKind.Container,
            Label = "Farm Settings",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "FarmSettings",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Menu.EquipmentControls"),
            Kind = BlockKind.Container,
            Label = "Equipment Controls",
            PreferredDock = BlockRegion.Left,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "EquipmentControls",
            Children = new[]
            {
                new BlockDefinitionId("Container.AutosteerControls"),
                new BlockDefinitionId("Container.SectionControls"),
                new BlockDefinitionId("Container.RateControls"),
            },
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Container.AutosteerControls"),
            Kind = BlockKind.Container,
            Label = "Autosteer Controls",
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "AutosteerControls",
            Children = new[]
            {
                new BlockDefinitionId("Cmd.AutoSteerToggle"),
                new BlockDefinitionId("Cmd.ABLineCycle"),
                new BlockDefinitionId("Cmd.UTurnToggle"),
            },
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Container.SectionControls"),
            Kind = BlockKind.Container,
            Label = "Section Controls",
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "SectionControls",
            Children = new[]
            {
                new BlockDefinitionId("Cmd.SectionMaster"),
            },
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Container.RateControls"),
            Kind = BlockKind.Container,
            Label = "Rate Controls",
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "RateControls",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.AutoSteerToggle"),
            Kind = BlockKind.CommandButton,
            Label = "Autosteer",
            IconKey = "autosteer",
            PreferredDock = BlockRegion.Right,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "AutosteerControls",
            CommandKey = "Host.ToggleAutosteer",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.ABLineCycle"),
            Kind = BlockKind.CommandButton,
            Label = "Cycle AB Line",
            IconKey = "ab-lines",
            PreferredDock = BlockRegion.Right,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "AutosteerControls",
            CommandKey = "Host.CycleAbLine",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.UTurnToggle"),
            Kind = BlockKind.CommandButton,
            Label = "U-Turn",
            IconKey = "uturn",
            PreferredDock = BlockRegion.Right,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "AutosteerControls",
            CommandKey = "Host.ToggleUTurn",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.SectionMaster"),
            Kind = BlockKind.CommandButton,
            Label = "Sections",
            IconKey = "sections",
            PreferredDock = BlockRegion.Right,
            Placement = PlacementPolicy.MenuScoped,
            ContainerId = "SectionControls",
            CommandKey = "Host.ToggleSections",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.Start"),
            Kind = BlockKind.CommandButton,
            Label = "Start",
            IconKey = "start",
            PreferredDock = BlockRegion.Bottom,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.StartGuidance",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.Pause"),
            Kind = BlockKind.CommandButton,
            Label = "Pause",
            IconKey = "pause",
            PreferredDock = BlockRegion.Bottom,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.PauseGuidance",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.Stop"),
            Kind = BlockKind.CommandButton,
            Label = "Stop",
            IconKey = "stop",
            PreferredDock = BlockRegion.Bottom,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.StopGuidance",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.NudgeLeft"),
            Kind = BlockKind.CommandButton,
            Label = "Nudge L",
            IconKey = "nudge-left",
            PreferredDock = BlockRegion.Bottom,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.NudgeLeft",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Cmd.NudgeRight"),
            Kind = BlockKind.CommandButton,
            Label = "Nudge R",
            IconKey = "nudge-right",
            PreferredDock = BlockRegion.Bottom,
            Placement = PlacementPolicy.Free,
            CommandKey = "Host.NudgeRight",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Tel.Speed"),
            Kind = BlockKind.Telemetry,
            Label = "Speed",
            IconKey = "speed",
            PreferredDock = BlockRegion.Top,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
            TelemetrySmallViewKey = "Telemetry/SpeedSmall",
            TelemetryLargeViewKey = "Telemetry/SpeedLarge",
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Tel.Gps"),
            Kind = BlockKind.Telemetry,
            Label = "GPS",
            IconKey = "guidance",
            PreferredDock = BlockRegion.Top,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Tel.Sections"),
            Kind = BlockKind.Telemetry,
            Label = "Sections",
            IconKey = "sections",
            PreferredDock = BlockRegion.Top,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
        };

        yield return new BlockDefinition
        {
            Id = new BlockDefinitionId("Tel.Radio"),
            Kind = BlockKind.Telemetry,
            Label = "Radio",
            IconKey = "coverage",
            PreferredDock = BlockRegion.Top,
            PreferredSize = BlockSize.Tile1xHalf,
            Placement = PlacementPolicy.Free,
            SupportsHalfHeight = true,
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
