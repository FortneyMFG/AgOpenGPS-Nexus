using System.Collections.Generic;
using System.Text.Json;
using Aog.Core.Jobs;

namespace Aog.Plugins.JobTasks;

/// <summary>
/// Captures the persisted state for a job, combining metadata, filesystem layout, and optional enrichment.
/// </summary>
/// <param name="Metadata">In-memory metadata maintained by the lifecycle orchestrator.</param>
/// <param name="Layout">Filesystem layout describing where job artefacts live.</param>
/// <param name="Sessions">Lightweight descriptors for sessions contained in the job.</param>
/// <param name="Spatial">Optional spatial hints linked to the job.</param>
/// <param name="Assets">Optional asset catalogue references.</param>
/// <param name="Stats">Aggregated statistics derived from session history.</param>
/// <param name="Extensions">Plugin-defined extensions stored alongside the job.</param>
/// <param name="Equipment">Equipment bindings captured for the job.</param>
public sealed record class JobSnapshot(
    JobMetadata Metadata,
    JobStoreLayout Layout,
    IReadOnlyList<JobSessionSnapshot> Sessions,
    JobSpatialSnapshot? Spatial = null,
    JobAssetSnapshot? Assets = null,
    JobStatisticsSnapshot? Stats = null,
    IReadOnlyDictionary<string, JsonElement>? Extensions = null,
    JobEquipmentSnapshot? Equipment = null);

/// <summary>
/// Spatial hints captured for a job.
/// </summary>
/// <param name="Home">Primary reference coordinate.</param>
/// <param name="Envelope">Bounding envelope covering the mounted fields.</param>
/// <param name="PrimaryBoundaryId">Identifier of the boundary treated as canonical.</param>
/// <param name="BoundaryReferences">Additional boundary identifiers linked to the job.</param>
/// <param name="CoordinateReferenceSystem">Optional CRS identifier when not WGS84.</param>
public sealed record class JobSpatialSnapshot(
    JobLatLonSnapshot? Home,
    JobEnvelopeSnapshot? Envelope,
    string? PrimaryBoundaryId = null,
    IReadOnlyList<string>? BoundaryReferences = null,
    string? CoordinateReferenceSystem = null);

/// <summary>
/// Latitude/longitude coordinate with optional elevation metadata.
/// </summary>
/// <param name="Latitude">Latitude component in decimal degrees.</param>
/// <param name="Longitude">Longitude component in decimal degrees.</param>
/// <param name="ElevationMeters">Optional elevation in meters.</param>
public sealed record class JobLatLonSnapshot(
    double Latitude,
    double Longitude,
    double? ElevationMeters = null);

/// <summary>
/// Bounding envelope describing the mounted field extents.
/// </summary>
/// <param name="MinLatitude">Minimum latitude.</param>
/// <param name="MinLongitude">Minimum longitude.</param>
/// <param name="MaxLatitude">Maximum latitude.</param>
/// <param name="MaxLongitude">Maximum longitude.</param>
public sealed record class JobEnvelopeSnapshot(
    double MinLatitude,
    double MinLongitude,
    double MaxLatitude,
    double MaxLongitude);

/// <summary>
/// Asset references linked to a job.
/// </summary>
/// <param name="BoundaryLayers">Boundary layer identifiers.</param>
/// <param name="CoverageLayers">Coverage layer identifiers.</param>
/// <param name="GuidanceSets">Guidance set identifiers.</param>
/// <param name="Prescriptions">Prescription layer identifiers.</param>
/// <param name="Attachments">Attachment descriptors stored with the job.</param>
public sealed record class JobAssetSnapshot(
    IReadOnlyList<string>? BoundaryLayers = null,
    IReadOnlyList<string>? CoverageLayers = null,
    IReadOnlyList<string>? GuidanceSets = null,
    IReadOnlyList<string>? Prescriptions = null,
    IReadOnlyList<JobAttachmentSnapshot>? Attachments = null);

/// <summary>
/// Equipment, preset, and layout bindings captured when orchestrating a job.
/// </summary>
/// <param name="VehicleId">Identifier of the vehicle or power unit assigned to the job.</param>
/// <param name="ImplementId">Identifier of the implement paired with the vehicle.</param>
/// <param name="PresetId">Preset applied when the job was orchestrated.</param>
/// <param name="LayoutId">Layout snapshot linked to the job.</param>
public sealed record class JobEquipmentSnapshot(
    string? VehicleId = null,
    string? ImplementId = null,
    string? PresetId = null,
    string? LayoutId = null);

/// <summary>
/// Attachment metadata stored with a job.
/// </summary>
/// <param name="Id">Stable attachment identifier.</param>
/// <param name="Path">Path to the attachment within the job bundle.</param>
/// <param name="MediaType">Optional media type for the attachment.</param>
/// <param name="SizeBytes">Optional file size in bytes.</param>
public sealed record class JobAttachmentSnapshot(
    string Id,
    string Path,
    string? MediaType = null,
    long? SizeBytes = null);

/// <summary>
/// Aggregated statistics for a job derived from session history.
/// </summary>
/// <param name="TotalAreaHectares">Total area covered by the job.</param>
/// <param name="TotalDistanceKilometers">Total distance travelled.</param>
/// <param name="ActiveDurationSeconds">Total active session duration in seconds.</param>
/// <param name="CompletedSessionCount">Number of sessions marked as completed.</param>
public sealed record class JobStatisticsSnapshot(
    double? TotalAreaHectares = null,
    double? TotalDistanceKilometers = null,
    double? ActiveDurationSeconds = null,
    int? CompletedSessionCount = null);
