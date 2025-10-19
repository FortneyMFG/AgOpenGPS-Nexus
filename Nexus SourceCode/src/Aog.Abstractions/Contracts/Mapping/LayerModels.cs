using System;
using System.Collections.Generic;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Describes a layer registered within the mapping kernel.
/// </summary>
/// <param name="Id">Stable identifier for the layer.</param>
/// <param name="LayerType">Layer type hint (boundary, coverage, raster, etc.).</param>
/// <param name="State">Current state of the layer.</param>
/// <param name="Visible">Whether the layer should be presented by default.</param>
/// <param name="ZIndexHint">Suggested draw order from the core runtime.</param>
/// <param name="StyleDefaults">Default style profile supplied by the core.</param>
/// <param name="Data">Optional data location hints.</param>
public sealed record LayerDescriptor(
    string Id,
    string LayerType,
    LayerState State,
    bool Visible,
    int ZIndexHint,
    LayerStyleProfile StyleDefaults,
    LayerDataLocation? Data);

/// <summary>
/// Represents the lifecycle state of a layer as reported by Core.
/// </summary>
public enum LayerState
{
    /// <summary>
    /// The state of the layer is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The layer is available for rendering and edits.
    /// </summary>
    Active,

    /// <summary>
    /// The layer exists but is not currently available (disabled, filtered, etc.).
    /// </summary>
    Inactive,

    /// <summary>
    /// The layer is present but has encountered an error.
    /// </summary>
    Faulted,
}

/// <summary>
/// Captures the visual defaults for a layer.
/// </summary>
/// <param name="Visible">Optional visibility override.</param>
/// <param name="ZIndex">Optional draw-order override.</param>
/// <param name="Opacity">Optional opacity suggestion (0-1).</param>
/// <param name="Parameters">Additional renderer-specific hints (hex colors, stroke widths, etc.).</param>
public sealed record LayerStyleProfile(
    bool? Visible,
    int? ZIndex,
    double? Opacity,
    IReadOnlyDictionary<string, string>? Parameters);

/// <summary>
/// Provides hints for where layer data can be fetched.
/// </summary>
/// <param name="Uri">URI pointing at the durable data for the layer.</param>
/// <param name="Format">Serialization format hint (GeoJSON, MBTiles, etc.).</param>
/// <param name="Properties">Arbitrary metadata supplied by Core.</param>
public sealed record LayerDataLocation(
    string Uri,
    string? Format,
    IReadOnlyDictionary<string, string>? Properties);

/// <summary>
/// Describes a change to a registered layer.
/// </summary>
/// <param name="LayerId">Identifier for the layer the change targets.</param>
/// <param name="Kind">Type of change.</param>
/// <param name="Descriptor">Updated descriptor (when applicable).</param>
/// <param name="Edits">Associated edit batch (when applicable).</param>
public sealed record LayerChange(
    string LayerId,
    LayerChangeKind Kind,
    LayerDescriptor? Descriptor,
    LayerEditBatch? Edits);

/// <summary>
/// Enumerates change kinds emitted by the layer registry.
/// </summary>
public enum LayerChangeKind
{
    /// <summary>
    /// Indicates no change.
    /// </summary>
    None = 0,

    /// <summary>
    /// A layer was added or updated.
    /// </summary>
    Upserted,

    /// <summary>
    /// A layer was removed from the registry.
    /// </summary>
    Removed,

    /// <summary>
    /// A journal edit batch was appended for the layer.
    /// </summary>
    EditsAppended,
}

/// <summary>
/// Represents a request to create a new layer.
/// </summary>
/// <param name="LayerType">Layer type being created.</param>
/// <param name="DisplayName">User-visible name.</param>
/// <param name="Properties">Additional metadata to associate with the layer.</param>
public sealed record LayerCreateRequest(
    string LayerType,
    string? DisplayName,
    IReadOnlyDictionary<string, string>? Properties);

/// <summary>
/// Represents metadata updates for an existing layer.
/// </summary>
/// <param name="DisplayName">Optional updated display name.</param>
/// <param name="Visible">Optional visibility flag.</param>
/// <param name="ZIndexHint">Optional draw-order hint.</param>
/// <param name="Properties">Additional metadata changes.</param>
public sealed record LayerMetadataUpdate(
    string? DisplayName,
    bool? Visible,
    int? ZIndexHint,
    IReadOnlyDictionary<string, string>? Properties);

/// <summary>
/// Represents a batch of edits to append to a layer journal.
/// </summary>
/// <param name="Operations">Set of operations to apply atomically.</param>
public sealed record LayerEditBatch(IReadOnlyList<LayerEditOperation> Operations);

/// <summary>
/// Represents a serialized edit operation.
/// </summary>
/// <param name="OperationType">Operation verb (upsert, delete, etc.).</param>
/// <param name="Attributes">Structured metadata for the operation.</param>
/// <param name="Payload">Opaque payload understood by the layer controller.</param>
public sealed record LayerEditOperation(
    string OperationType,
    IReadOnlyDictionary<string, string>? Attributes,
    ReadOnlyMemory<byte> Payload);

/// <summary>
/// Provides CRS-neutral geographic coordinates (latitude, longitude, altitude).
/// </summary>
public readonly record struct GeoCoordinate(double Latitude, double Longitude, double Altitude);

/// <summary>
/// Represents an ENU coordinate triple.
/// </summary>
public readonly record struct EnuCoordinate(double East, double North, double Up);

/// <summary>
/// Describes a tile source persisted by Core for a layer.
/// </summary>
/// <param name="LayerId">Layer identifier.</param>
/// <param name="Format">Tile format (MBTiles, GeoPackage, etc.).</param>
/// <param name="Path">Filesystem path to the tile source.</param>
/// <param name="MinZoom">Minimum zoom level provided.</param>
/// <param name="MaxZoom">Maximum zoom level provided.</param>
/// <param name="Properties">Additional metadata for consumers.</param>
public sealed record TileSource(
    string LayerId,
    string Format,
    string Path,
    int MinZoom,
    int MaxZoom,
    IReadOnlyDictionary<string, string>? Properties);
