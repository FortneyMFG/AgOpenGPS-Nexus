using System;
using System.Collections.Generic;

namespace Aog.UI.Avalonia.Blocks;

/// <summary>
/// Defines the regions in which a block instance may appear.
/// </summary>
public enum BlockRegion
{
    Left,
    LeftSub,
    Right,
    Bottom,
    Top,
    Overlay,
    Floating,
}

/// <summary>
/// Defines the supported sizing presets for block tiles.
/// </summary>
public enum BlockSize
{
    Tile1x1,
    Tile1x2,
    Tile2x1,
    Tile2x2,
    TileHalfx1,
    Tile1xHalf,
    Tile2xHalf,
    TileHalfx2,
}

/// <summary>
/// Classifies the visual or behavioral category of a block.
/// </summary>
public enum BlockKind
{
    CommandButton,
    Telemetry,
    Gauge,
    Menu,
    Container,
    PromptLauncher,
}

/// <summary>
/// Indicates the placement policy enforced for a block definition.
/// </summary>
public enum PlacementPolicy
{
    Free,
    MenuScoped,
}

/// <summary>
/// Describes whether an instance is the canonical entry or a clone.
/// </summary>
public enum BlockOrigin
{
    Canonical,
    Clone,
}

/// <summary>
/// Strongly typed identifier for a block definition.
/// </summary>
/// <param name="Value">The unique identifier string.</param>
public sealed record BlockDefinitionId(string Value)
{
    public override string ToString() => Value;
}

/// <summary>
/// Describes a reusable block definition that can be instantiated across the shell.
/// </summary>
public sealed class BlockDefinition
{
    public BlockDefinitionId Id { get; init; } = new(string.Empty);

    public BlockKind Kind { get; init; } = BlockKind.CommandButton;

    public string Label { get; init; } = string.Empty;

    public string? IconKey { get; init; }
        = null;

    public string? ViewKey { get; init; }
        = null;

    public BlockSize PreferredSize { get; init; }
        = BlockSize.Tile1x1;

    public BlockRegion PreferredDock { get; init; }
        = BlockRegion.Left;

    public PlacementPolicy Placement { get; init; }
        = PlacementPolicy.Free;

    public string? ContainerId { get; init; }
        = null;

    public bool SupportsHalfHeight { get; init; }
        = false;

    public bool SupportsFullHeight { get; init; }
        = true;

    public IReadOnlyList<BlockDefinitionId> Children { get; init; }
        = Array.Empty<BlockDefinitionId>();

    public string? TelemetryLargeViewKey { get; init; }
        = null;

    public string? TelemetrySmallViewKey { get; init; }
        = null;

    public string? CommandKey { get; init; }
        = null;
}

/// <summary>
/// Strongly typed identifier for block instances persisted in the layout.
/// </summary>
public sealed class BlockInstanceId
{
    public BlockInstanceId()
        : this(Guid.NewGuid())
    {
    }

    public BlockInstanceId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Represents a persisted instance of a block definition.
/// </summary>
public sealed class BlockInstance
{
    public BlockInstanceId InstanceId { get; init; } = new();

    public BlockDefinitionId DefinitionId { get; init; } = new(string.Empty);

    public BlockRegion Region { get; set; } = BlockRegion.Left;

    public int Order { get; set; }
        = 0;

    public BlockSize? SizeOverride { get; set; }
        = null;

    public double? OpacityOverride { get; set; }
        = null;

    public BlockOrigin Origin { get; set; }
        = BlockOrigin.Canonical;

    public string? ContainerId { get; set; }
        = null;

    public string? GroupKey { get; set; }
        = null;

    /// <summary>
    /// Creates a deep copy of the instance preserving the identifier.
    /// </summary>
    public BlockInstance Clone()
    {
        return new BlockInstance
        {
            InstanceId = new BlockInstanceId(InstanceId.Value),
            DefinitionId = DefinitionId,
            Region = Region,
            Order = Order,
            SizeOverride = SizeOverride,
            OpacityOverride = OpacityOverride,
            Origin = Origin,
            ContainerId = ContainerId,
            GroupKey = GroupKey,
        };
    }
}
