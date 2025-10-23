using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aog.Plugins.Genetics;

/// <summary>
/// Canonical representation of a genetics plan feature aligned with <c>GeneticsPlan.v1</c>.
/// </summary>
public record GeneticsPlanFeature
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsPlanFeature"/> record.
    /// </summary>
    public GeneticsPlanFeature(
        string zoneId,
        string featureId,
        string layerId,
        string? jobId,
        DateTimeOffset createdAt,
        string createdBy,
        DateTimeOffset? lastModifiedAt,
        string brand,
        string product,
        string? traitStack,
        string? lot,
        string? treatment,
        string? source,
        string? notes,
        GeneticsZoneGeometry geometry)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            throw new ArgumentException("Zone identifier is required.", nameof(zoneId));
        }

        if (string.IsNullOrWhiteSpace(featureId))
        {
            throw new ArgumentException("Feature identifier is required.", nameof(featureId));
        }

        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new ArgumentException("Created by is required.", nameof(createdBy));
        }

        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new ArgumentException("Brand is required.", nameof(brand));
        }

        if (string.IsNullOrWhiteSpace(product))
        {
            throw new ArgumentException("Product is required.", nameof(product));
        }

        ZoneId = zoneId;
        FeatureId = featureId;
        LayerId = layerId;
        JobId = string.IsNullOrWhiteSpace(jobId) ? null : jobId;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        LastModifiedAt = lastModifiedAt;
        Brand = brand;
        Product = product;
        TraitStack = string.IsNullOrWhiteSpace(traitStack) ? null : traitStack;
        Lot = string.IsNullOrWhiteSpace(lot) ? null : lot;
        Treatment = string.IsNullOrWhiteSpace(treatment) ? null : treatment;
        Source = string.IsNullOrWhiteSpace(source) ? null : source;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
    }

    /// <summary>Gets the caller supplied zone identifier.</summary>
    public string ZoneId { get; }

    /// <summary>Gets the feature identifier that matches the genetics schema pattern.</summary>
    public string FeatureId { get; }

    /// <summary>Gets the layer identifier.</summary>
    public string LayerId { get; }

    /// <summary>Gets the optional job association.</summary>
    public string? JobId { get; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets the actor responsible for the feature.</summary>
    public string CreatedBy { get; }

    /// <summary>Gets the last modification timestamp when available.</summary>
    public DateTimeOffset? LastModifiedAt { get; }

    /// <summary>Gets the seed brand.</summary>
    public string Brand { get; }

    /// <summary>Gets the product identifier.</summary>
    public string Product { get; }

    /// <summary>Gets the optional trait stack.</summary>
    public string? TraitStack { get; }

    /// <summary>Gets the optional lot identifier.</summary>
    public string? Lot { get; }

    /// <summary>Gets the optional treatment description.</summary>
    public string? Treatment { get; }

    /// <summary>Gets the source of the record.</summary>
    public string? Source { get; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; }

    /// <summary>Gets the polygon geometry for the feature.</summary>
    public GeneticsZoneGeometry Geometry { get; }

    /// <summary>Gets the inclusive area of the polygon in square metres.</summary>
    public double AreaSquareMeters => Geometry.AreaSquareMeters;
}

/// <summary>
/// Change log entry describing an in-session mutation to a genetics variety record.
/// </summary>
public sealed class GeneticsChangeLogEntry
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "brand",
        "product",
        "traitStack",
        "lot",
        "treatment",
        "barcode",
        "notes",
        "source"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsChangeLogEntry"/> class.
    /// </summary>
    public GeneticsChangeLogEntry(DateTimeOffset changedAt, string actor, string field, string? previous, string? current)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor is required.", nameof(actor));
        }

        if (string.IsNullOrWhiteSpace(field))
        {
            throw new ArgumentException("Field is required.", nameof(field));
        }

        if (!AllowedFields.Contains(field.Trim()))
        {
            throw new ArgumentOutOfRangeException(nameof(field), field, "Field must be one of brand, product, traitStack, lot, treatment, barcode, notes, or source.");
        }

        ChangedAt = changedAt;
        Actor = actor.Trim();
        Field = field.Trim().ToLowerInvariant();
        Previous = string.IsNullOrWhiteSpace(previous) ? null : previous.Trim();
        Current = string.IsNullOrWhiteSpace(current) ? null : current.Trim();
    }

    /// <summary>Gets the change timestamp.</summary>
    public DateTimeOffset ChangedAt { get; }

    /// <summary>Gets the actor performing the change.</summary>
    public string Actor { get; }

    /// <summary>Gets the changed field name.</summary>
    public string Field { get; }

    /// <summary>Gets the previous value.</summary>
    public string? Previous { get; }

    /// <summary>Gets the new value.</summary>
    public string? Current { get; }
}

/// <summary>
/// Genetics variety feature aligned with <c>GeneticsVariety.v1</c>.
/// </summary>
public sealed record GeneticsVarietyFeature : GeneticsPlanFeature
{
    private static readonly IReadOnlyList<GeneticsChangeLogEntry> EmptyLog = Array.Empty<GeneticsChangeLogEntry>();

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsVarietyFeature"/> record.
    /// </summary>
    public GeneticsVarietyFeature(
        string zoneId,
        string featureId,
        string layerId,
        string jobId,
        string sessionId,
        DateTimeOffset createdAt,
        string createdBy,
        DateTimeOffset? lastModifiedAt,
        DateTimeOffset appliedAt,
        string brand,
        string product,
        string? traitStack,
        string? lot,
        string? treatment,
        string? source,
        string? barcode,
        IReadOnlyList<GeneticsChangeLogEntry>? changeLog,
        string? notes,
        GeneticsZoneGeometry geometry)
        : base(zoneId, featureId, layerId, jobId, createdAt, createdBy, lastModifiedAt, brand, product, traitStack, lot, treatment, source, notes, geometry)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier is required.", nameof(jobId));
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session identifier is required.", nameof(sessionId));
        }

        SessionId = sessionId;
        AppliedAt = appliedAt;
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        ChangeLog = changeLog is null or { Count: 0 }
            ? EmptyLog
            : new ReadOnlyCollection<GeneticsChangeLogEntry>(changeLog is List<GeneticsChangeLogEntry> list ? list : new List<GeneticsChangeLogEntry>(changeLog));
    }

    /// <summary>Gets the owning session identifier.</summary>
    public string SessionId { get; }

    /// <summary>Gets the time the variety was applied.</summary>
    public DateTimeOffset AppliedAt { get; }

    /// <summary>Gets the optional barcode payload.</summary>
    public string? Barcode { get; }

    /// <summary>Gets the ordered change log entries.</summary>
    public IReadOnlyList<GeneticsChangeLogEntry> ChangeLog { get; }
}
