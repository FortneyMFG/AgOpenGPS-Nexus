using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aog.Plugins.JobTasks;

internal sealed class JobDocument
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("jobId")]
    public required string JobId { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("startedAt")]
    public DateTimeOffset? StartedAt { get; init; }

    [JsonPropertyName("endedAt")]
    public DateTimeOffset? EndedAt { get; init; }

    [JsonPropertyName("activeSessionId")]
    public string? ActiveSessionId { get; init; }

    [JsonPropertyName("context")]
    public required JobContextDocument Context { get; init; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    [JsonPropertyName("paths")]
    public required JobPathsDocument Paths { get; init; }

    [JsonPropertyName("spatial")]
    public JobSpatialDocument? Spatial { get; init; }

    [JsonPropertyName("assets")]
    public JobAssetsDocument? Assets { get; init; }

    [JsonPropertyName("stats")]
    public JobStatisticsDocument? Stats { get; init; }

    [JsonPropertyName("sessions")]
    public List<JobSessionDocument>? Sessions { get; init; }

    [JsonPropertyName("extensions")]
    public Dictionary<string, JsonElement>? Extensions { get; init; }

    [JsonPropertyName("equipment")]
    public JobEquipmentDocument? Equipment { get; init; }
}

internal sealed class JobContextDocument
{
    [JsonPropertyName("farmId")]
    public required string FarmId { get; init; }

    [JsonPropertyName("fieldIds")]
    public required List<string> FieldIds { get; init; }

    [JsonPropertyName("seasonId")]
    public string? SeasonId { get; init; }

    [JsonPropertyName("workOrderId")]
    public string? WorkOrderId { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

internal sealed class JobPathsDocument
{
    [JsonPropertyName("jobRoot")]
    public required string JobRoot { get; init; }

    [JsonPropertyName("dataDir")]
    public required string DataDirectory { get; init; }

    [JsonPropertyName("resumeFile")]
    public required string ResumeFile { get; init; }

    [JsonPropertyName("attachmentsDir")]
    public string? AttachmentsDirectory { get; init; }
}

internal sealed class JobSpatialDocument
{
    [JsonPropertyName("home")]
    public JobLatLonDocument? Home { get; init; }

    [JsonPropertyName("envelope")]
    public JobEnvelopeDocument? Envelope { get; init; }

    [JsonPropertyName("primaryBoundaryId")]
    public string? PrimaryBoundaryId { get; init; }

    [JsonPropertyName("boundaryRefs")]
    public List<string>? BoundaryReferences { get; init; }

    [JsonPropertyName("crs")]
    public string? CoordinateReferenceSystem { get; init; }
}

internal sealed class JobLatLonDocument
{
    [JsonPropertyName("lat")]
    public required double Latitude { get; init; }

    [JsonPropertyName("lon")]
    public required double Longitude { get; init; }

    [JsonPropertyName("elevationM")]
    public double? ElevationMeters { get; init; }
}

internal sealed class JobEnvelopeDocument
{
    [JsonPropertyName("minLat")]
    public required double MinLatitude { get; init; }

    [JsonPropertyName("minLon")]
    public required double MinLongitude { get; init; }

    [JsonPropertyName("maxLat")]
    public required double MaxLatitude { get; init; }

    [JsonPropertyName("maxLon")]
    public required double MaxLongitude { get; init; }
}

internal sealed class JobAssetsDocument
{
    [JsonPropertyName("boundaryLayers")]
    public List<string>? BoundaryLayers { get; init; }

    [JsonPropertyName("coverageLayers")]
    public List<string>? CoverageLayers { get; init; }

    [JsonPropertyName("guidanceSets")]
    public List<string>? GuidanceSets { get; init; }

    [JsonPropertyName("prescriptions")]
    public List<string>? Prescriptions { get; init; }

    [JsonPropertyName("attachments")]
    public List<JobAttachmentDocument>? Attachments { get; init; }
}

internal sealed class JobAttachmentDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("mediaType")]
    public string? MediaType { get; init; }

    [JsonPropertyName("sizeBytes")]
    public long? SizeBytes { get; init; }
}

internal sealed class JobStatisticsDocument
{
    [JsonPropertyName("totalAreaHa")]
    public double? TotalAreaHectares { get; init; }

    [JsonPropertyName("totalDistanceKm")]
    public double? TotalDistanceKilometers { get; init; }

    [JsonPropertyName("activeDurationSec")]
    public double? ActiveDurationSeconds { get; init; }

    [JsonPropertyName("completedSessionCount")]
    public int? CompletedSessionCount { get; init; }
}

internal sealed class JobEquipmentDocument
{
    [JsonPropertyName("vehicleId")]
    public string? VehicleId { get; init; }

    [JsonPropertyName("implementId")]
    public string? ImplementId { get; init; }

    [JsonPropertyName("presetId")]
    public string? PresetId { get; init; }

    [JsonPropertyName("layoutId")]
    public string? LayoutId { get; init; }
}

internal sealed class JobSessionDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("startedAt")]
    public required DateTimeOffset StartedAt { get; init; }

    [JsonPropertyName("lastModifiedAt")]
    public required DateTimeOffset LastModifiedAt { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("endedAt")]
    public DateTimeOffset? EndedAt { get; init; }

    [JsonPropertyName("activeOperators")]
    public List<string>? ActiveOperators { get; init; }

    [JsonPropertyName("stats")]
    public JobSessionStatisticsDocument? Stats { get; init; }

    [JsonPropertyName("extensions")]
    public Dictionary<string, JsonElement>? Extensions { get; init; }
}

internal sealed class JobSessionStatisticsDocument
{
    [JsonPropertyName("areaHa")]
    public double? AreaHectares { get; init; }

    [JsonPropertyName("distanceKm")]
    public double? DistanceKilometers { get; init; }

    [JsonPropertyName("durationSec")]
    public double? DurationSeconds { get; init; }

    [JsonPropertyName("coveragePct")]
    public double? CoveragePercent { get; init; }
}
