using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Metadata payload emitted by the field health ingest pipeline.
/// </summary>
public sealed class FieldHealthLayerMetadata
{
    public FieldHealthLayerMetadata(
        string kind,
        string? schemaRef,
        string? notes,
        IReadOnlyList<string> tags,
        IReadOnlyList<FieldHealthObservation> observations,
        FieldHealthLayerStatistics statistics)
    {
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        SchemaRef = schemaRef;
        Notes = notes;
        Tags = tags?.ToArray() ?? throw new ArgumentNullException(nameof(tags));
        Observations = observations?.ToArray() ?? throw new ArgumentNullException(nameof(observations));
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
    }

    /// <summary>
    /// Layer kind (`risk.flood`, `risk.compaction`, `risk.weeds`, `risk.other`).
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Optional schema reference to embed in the metadata block.
    /// </summary>
    public string? SchemaRef { get; }

    /// <summary>
    /// Layer level notes captured during scouting.
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Free-form tags associated with the layer.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Canonical observation list sorted deterministically.
    /// </summary>
    public IReadOnlyList<FieldHealthObservation> Observations { get; }

    /// <summary>
    /// Aggregated statistics derived from the observations.
    /// </summary>
    public FieldHealthLayerStatistics Statistics { get; }
}
