using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Configuration for the field health ingest pipeline.
/// </summary>
public sealed class FieldHealthIngestOptions
{
    private static readonly Regex KindPattern = new("^risk\\.(flood|compaction|weeds|other)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex TagPattern = new("^[A-Za-z0-9_.:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Kind of field health layer to build (defaults to <c>risk.other</c>).
    /// </summary>
    public string Kind { get; init; } = "risk.other";

    /// <summary>
    /// Optional schema reference embedded into the metadata block.
    /// </summary>
    public string? SchemaRef { get; init; } = "https://agopengps.org/schemas/FieldHealthRiskLayer.v1.json";

    /// <summary>
    /// Initial layer level notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Tags applied to the layer for filtering or analytics.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Validates option values.
    /// </summary>
    public void Validate()
    {
        if (!KindPattern.IsMatch(Kind ?? string.Empty))
        {
            throw new ArgumentException("Kind must match risk.(flood|compaction|weeds|other).", nameof(Kind));
        }

        if (SchemaRef is not null && !Uri.IsWellFormedUriString(SchemaRef, UriKind.Absolute))
        {
            throw new ArgumentException("SchemaRef must be an absolute URI when provided.", nameof(SchemaRef));
        }

        if (Tags is null)
        {
            throw new ArgumentException("Tags collection cannot be null.", nameof(Tags));
        }

        foreach (var tag in Tags)
        {
            ValidateTag(tag);
        }
    }

    internal static void ValidateTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tags cannot contain null, empty, or whitespace values.");
        }

        if (!TagPattern.IsMatch(tag))
        {
            throw new ArgumentException("Tags must match ^[A-Za-z0-9_.:-]+$.");
        }
    }
}
