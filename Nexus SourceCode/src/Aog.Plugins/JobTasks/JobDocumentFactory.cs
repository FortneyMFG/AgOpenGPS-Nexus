using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aog.Core.Jobs;

namespace Aog.Plugins.JobTasks;

internal static class JobDocumentFactory
{
    private const string CurrentSchemaVersion = "1.0.0";

    public static JobDocument Create(JobSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(snapshot.Metadata);
        ArgumentNullException.ThrowIfNull(snapshot.Layout);

        if (string.IsNullOrWhiteSpace(snapshot.Layout.JobRoot))
        {
            throw new InvalidOperationException("Job snapshot layout must include a job root path.");
        }

        if (string.IsNullOrWhiteSpace(snapshot.Layout.DataDirectory))
        {
            throw new InvalidOperationException("Job snapshot layout must include a data directory path.");
        }

        if (string.IsNullOrWhiteSpace(snapshot.Layout.ResumeFile))
        {
            throw new InvalidOperationException("Job snapshot layout must include a resume file path.");
        }

        var metadata = snapshot.Metadata;
        var sessions = snapshot.Sessions ?? Array.Empty<JobSessionSnapshot>();

        var document = new JobDocument
        {
            SchemaVersion = CurrentSchemaVersion,
            JobId = metadata.JobId ?? throw new InvalidDataException("Job metadata is missing an identifier."),
            DisplayName = metadata.DisplayName ?? throw new InvalidDataException("Job metadata is missing a display name."),
            Slug = metadata.Slug ?? throw new InvalidDataException("Job metadata is missing a slug."),
            State = metadata.State.ToSchemaValue(),
            CreatedAt = metadata.CreatedAt,
            UpdatedAt = metadata.UpdatedAt,
            ActiveSessionId = metadata.ActiveSessionId,
            Context = CreateContext(metadata.Context),
            Tags = metadata.Tags is { Count: > 0 } ? new List<string>(metadata.Tags) : null,
            Paths = new JobPathsDocument
            {
                JobRoot = snapshot.Layout.JobRoot,
                DataDirectory = snapshot.Layout.DataDirectory,
                ResumeFile = snapshot.Layout.ResumeFile,
                AttachmentsDirectory = snapshot.Layout.AttachmentsDirectory
            },
            Spatial = CreateSpatial(snapshot.Spatial),
            Assets = CreateAssets(snapshot.Assets),
            Stats = CreateStatistics(snapshot.Stats),
            Sessions = sessions.Count > 0 ? sessions.Select(CreateSession).ToList() : new List<JobSessionDocument>(),
            Extensions = snapshot.Extensions is null ? null : new Dictionary<string, JsonElement>(snapshot.Extensions),
            Equipment = CreateEquipment(snapshot.Equipment)
        };

        if (metadata.State is JobLifecycleState.Active or JobLifecycleState.Paused or JobLifecycleState.Mounted)
        {
            document.StartedAt = metadata.CreatedAt;
        }

        if (metadata.State is JobLifecycleState.Completed or JobLifecycleState.Closed)
        {
            document.EndedAt = metadata.UpdatedAt;
        }

        return document;
    }

    public static JobSnapshot ToSnapshot(JobDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Context is null)
        {
            throw new InvalidDataException("Job document was missing required context metadata.");
        }

        if (document.Context.FieldIds is null || document.Context.FieldIds.Count == 0)
        {
            throw new InvalidDataException("Job document must specify at least one field identifier.");
        }

        var tags = document.Tags is { Count: > 0 } ? document.Tags.ToArray() : Array.Empty<string>();

        var metadata = new JobMetadata(
            document.JobId ?? throw new InvalidDataException("Job document missing job identifier."),
            document.Slug ?? throw new InvalidDataException("Job document missing slug."),
            document.DisplayName ?? throw new InvalidDataException("Job document missing display name."),
            JobLifecycleStateExtensions.ToLifecycleState(document.State ?? throw new InvalidDataException("Job document missing state.")),
            document.CreatedAt,
            document.UpdatedAt,
            document.ActiveSessionId,
            new JobContext(
                document.Context.FarmId ?? throw new InvalidDataException("Job context missing farm identifier."),
                document.Context.FieldIds.ToArray(),
                document.Context.SeasonId,
                document.Context.WorkOrderId,
                document.Context.Notes),
            tags);

        if (document.Paths is null)
        {
            throw new InvalidDataException("Job document missing filesystem path metadata.");
        }

        var layout = new JobStoreLayout(
            document.Paths.JobRoot ?? string.Empty,
            document.Paths.DataDirectory ?? string.Empty,
            document.Paths.ResumeFile ?? string.Empty,
            document.Paths.AttachmentsDirectory);

        var sessions = document.Sessions is { Count: > 0 }
            ? document.Sessions.Select(ToSessionSnapshot).ToArray()
            : Array.Empty<JobSessionSnapshot>();

        var spatial = document.Spatial is null ? null : ToSpatial(document.Spatial);
        var assets = document.Assets is null ? null : ToAssets(document.Assets);
        var stats = document.Stats is null ? null : ToStatistics(document.Stats);
        var extensions = document.Extensions is null ? null : new Dictionary<string, JsonElement>(document.Extensions);
        var equipment = document.Equipment is null ? null : ToEquipment(document.Equipment);

        return new JobSnapshot(metadata, layout, sessions, spatial, assets, stats, extensions, equipment);
    }

    private static JobContextDocument CreateContext(JobContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.FarmId))
        {
            throw new InvalidDataException("Job context must include a farm identifier.");
        }

        if (context.FieldIds is null || context.FieldIds.Count == 0)
        {
            throw new InvalidDataException("Job context must include at least one field identifier.");
        }

        return new JobContextDocument
        {
            FarmId = context.FarmId,
            FieldIds = new List<string>(context.FieldIds),
            SeasonId = context.SeasonId,
            WorkOrderId = context.WorkOrderId,
            Notes = context.Notes
        };
    }

    private static JobSessionDocument CreateSession(JobSessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (string.IsNullOrWhiteSpace(snapshot.SessionId))
        {
            throw new InvalidDataException("Session snapshot must include an identifier.");
        }

        return new JobSessionDocument
        {
            Id = snapshot.SessionId,
            State = snapshot.State.ToSchemaValue(),
            StartedAt = snapshot.StartedAt,
            LastModifiedAt = snapshot.LastModifiedAt,
            Name = snapshot.Name,
            EndedAt = snapshot.EndedAt,
            ActiveOperators = snapshot.ActiveOperators is { Count: > 0 } ? new List<string>(snapshot.ActiveOperators) : null,
            Stats = snapshot.Stats is null ? null : new JobSessionStatisticsDocument
            {
                AreaHectares = snapshot.Stats.AreaHectares,
                DistanceKilometers = snapshot.Stats.DistanceKilometers,
                DurationSeconds = snapshot.Stats.DurationSeconds,
                CoveragePercent = snapshot.Stats.CoveragePercent
            },
            Extensions = snapshot.Extensions is null ? null : new Dictionary<string, JsonElement>(snapshot.Extensions)
        };
    }

    private static JobSpatialDocument? CreateSpatial(JobSpatialSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        return new JobSpatialDocument
        {
            Home = snapshot.Home is null ? null : new JobLatLonDocument
            {
                Latitude = snapshot.Home.Latitude,
                Longitude = snapshot.Home.Longitude,
                ElevationMeters = snapshot.Home.ElevationMeters
            },
            Envelope = snapshot.Envelope is null ? null : new JobEnvelopeDocument
            {
                MinLatitude = snapshot.Envelope.MinLatitude,
                MinLongitude = snapshot.Envelope.MinLongitude,
                MaxLatitude = snapshot.Envelope.MaxLatitude,
                MaxLongitude = snapshot.Envelope.MaxLongitude
            },
            PrimaryBoundaryId = snapshot.PrimaryBoundaryId,
            BoundaryReferences = snapshot.BoundaryReferences is { Count: > 0 } ? new List<string>(snapshot.BoundaryReferences) : null,
            CoordinateReferenceSystem = snapshot.CoordinateReferenceSystem
        };
    }

    private static JobAssetsDocument? CreateAssets(JobAssetSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        return new JobAssetsDocument
        {
            BoundaryLayers = snapshot.BoundaryLayers is { Count: > 0 } ? new List<string>(snapshot.BoundaryLayers) : null,
            CoverageLayers = snapshot.CoverageLayers is { Count: > 0 } ? new List<string>(snapshot.CoverageLayers) : null,
            GuidanceSets = snapshot.GuidanceSets is { Count: > 0 } ? new List<string>(snapshot.GuidanceSets) : null,
            Prescriptions = snapshot.Prescriptions is { Count: > 0 } ? new List<string>(snapshot.Prescriptions) : null,
            Attachments = snapshot.Attachments is { Count: > 0 }
                ? snapshot.Attachments.Select(a =>
                {
                    if (string.IsNullOrWhiteSpace(a.Id))
                    {
                        throw new InvalidDataException("Attachment missing identifier.");
                    }

                    if (string.IsNullOrWhiteSpace(a.Path))
                    {
                        throw new InvalidDataException("Attachment missing path.");
                    }

                    return new JobAttachmentDocument
                    {
                        Id = a.Id,
                        Path = a.Path,
                        MediaType = a.MediaType,
                        SizeBytes = a.SizeBytes
                    };
                }).ToList()
                : null
        };
    }

    private static JobStatisticsDocument? CreateStatistics(JobStatisticsSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        return new JobStatisticsDocument
        {
            TotalAreaHectares = snapshot.TotalAreaHectares,
            TotalDistanceKilometers = snapshot.TotalDistanceKilometers,
            ActiveDurationSeconds = snapshot.ActiveDurationSeconds,
            CompletedSessionCount = snapshot.CompletedSessionCount
        };
    }

    private static JobEquipmentDocument? CreateEquipment(JobEquipmentSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        if (IsNullOrWhiteSpace(snapshot.VehicleId)
            && IsNullOrWhiteSpace(snapshot.ImplementId)
            && IsNullOrWhiteSpace(snapshot.PresetId)
            && IsNullOrWhiteSpace(snapshot.LayoutId))
        {
            return null;
        }

        return new JobEquipmentDocument
        {
            VehicleId = SanitizeEquipmentIdentifier(snapshot.VehicleId),
            ImplementId = SanitizeEquipmentIdentifier(snapshot.ImplementId),
            PresetId = SanitizeEquipmentIdentifier(snapshot.PresetId),
            LayoutId = SanitizeEquipmentIdentifier(snapshot.LayoutId)
        };
    }

    private static JobSessionSnapshot ToSessionSnapshot(JobSessionDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.Id))
        {
            throw new InvalidDataException("Job document contained a session without an identifier.");
        }

        var stats = document.Stats is null
            ? null
            : new JobSessionStatisticsSnapshot(
                document.Stats.AreaHectares,
                document.Stats.DistanceKilometers,
                document.Stats.DurationSeconds,
                document.Stats.CoveragePercent);

        IReadOnlyDictionary<string, JsonElement>? extensions = null;
        if (document.Extensions is not null)
        {
            extensions = new Dictionary<string, JsonElement>(document.Extensions);
        }

        IReadOnlyList<string>? operators = null;
        if (document.ActiveOperators is { Count: > 0 })
        {
            operators = document.ActiveOperators.ToArray();
        }

        return new JobSessionSnapshot(
            document.Id,
            JobSessionStateExtensions.ToSessionState(document.State ?? throw new InvalidDataException("Session missing state.")),
            document.StartedAt,
            document.LastModifiedAt,
            document.Name,
            document.EndedAt,
            operators,
            stats,
            extensions);
    }

    private static JobSpatialSnapshot? ToSpatial(JobSpatialDocument document)
    {
        JobLatLonSnapshot? home = null;
        if (document.Home is not null)
        {
            home = new JobLatLonSnapshot(document.Home.Latitude, document.Home.Longitude, document.Home.ElevationMeters);
        }

        JobEnvelopeSnapshot? envelope = null;
        if (document.Envelope is not null)
        {
            envelope = new JobEnvelopeSnapshot(document.Envelope.MinLatitude, document.Envelope.MinLongitude, document.Envelope.MaxLatitude, document.Envelope.MaxLongitude);
        }

        IReadOnlyList<string>? boundaryRefs = null;
        if (document.BoundaryReferences is { Count: > 0 })
        {
            boundaryRefs = document.BoundaryReferences.ToArray();
        }

        return new JobSpatialSnapshot(home, envelope, document.PrimaryBoundaryId, boundaryRefs, document.CoordinateReferenceSystem);
    }

    private static JobAssetSnapshot? ToAssets(JobAssetsDocument document)
    {
        IReadOnlyList<string>? boundaryLayers = null;
        if (document.BoundaryLayers is { Count: > 0 })
        {
            boundaryLayers = document.BoundaryLayers.ToArray();
        }

        IReadOnlyList<string>? coverageLayers = null;
        if (document.CoverageLayers is { Count: > 0 })
        {
            coverageLayers = document.CoverageLayers.ToArray();
        }

        IReadOnlyList<string>? guidanceSets = null;
        if (document.GuidanceSets is { Count: > 0 })
        {
            guidanceSets = document.GuidanceSets.ToArray();
        }

        IReadOnlyList<string>? prescriptions = null;
        if (document.Prescriptions is { Count: > 0 })
        {
            prescriptions = document.Prescriptions.ToArray();
        }

        IReadOnlyList<JobAttachmentSnapshot>? attachments = null;
        if (document.Attachments is { Count: > 0 })
        {
            attachments = document.Attachments.Select(a => new JobAttachmentSnapshot(
                a.Id,
                a.Path,
                a.MediaType,
                a.SizeBytes)).ToArray();
        }

        return new JobAssetSnapshot(boundaryLayers, coverageLayers, guidanceSets, prescriptions, attachments);
    }

    private static JobStatisticsSnapshot? ToStatistics(JobStatisticsDocument document)
    {
        return new JobStatisticsSnapshot(
            document.TotalAreaHectares,
            document.TotalDistanceKilometers,
            document.ActiveDurationSeconds,
            document.CompletedSessionCount);
    }

    private static JobEquipmentSnapshot? ToEquipment(JobEquipmentDocument document)
    {
        if (IsNullOrWhiteSpace(document.VehicleId)
            && IsNullOrWhiteSpace(document.ImplementId)
            && IsNullOrWhiteSpace(document.PresetId)
            && IsNullOrWhiteSpace(document.LayoutId))
        {
            return null;
        }

        return new JobEquipmentSnapshot(
            NormalizeEquipmentIdentifier(document.VehicleId),
            NormalizeEquipmentIdentifier(document.ImplementId),
            NormalizeEquipmentIdentifier(document.PresetId),
            NormalizeEquipmentIdentifier(document.LayoutId));
    }

    private static string? SanitizeEquipmentIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? NormalizeEquipmentIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static bool IsNullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);
}
