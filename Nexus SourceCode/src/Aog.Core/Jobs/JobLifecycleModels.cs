using System;
using System.Collections.Generic;

namespace Aog.Core.Jobs;

/// <summary>
/// Enumerates the lifecycle states for a job. Values align with ADR-030/ADR-041 terminology
/// so Core, UI, and plugins can exchange consistent lifecycle snapshots.
/// </summary>
public enum JobLifecycleState
{
    Planned = 0,
    Mounted = 1,
    Active = 2,
    Paused = 3,
    Closed = 4,
    Completed = 5,
}

/// <summary>
/// Event types emitted by the lifecycle orchestrator when jobs transition between states
/// or when their metadata changes.
/// </summary>
public enum JobLifecycleEventType
{
    Created,
    Mounted,
    Activated,
    Paused,
    Resumed,
    MetadataChanged,
    Closed,
    Completed,
}

/// <summary>
/// Captures farm, field, and orchestration context for a job. The orchestrator normalises
/// identifiers to the canonical <c>entity:type</c> format described in ADR-030.
/// </summary>
/// <param name="FarmId">Identifier of the farm that owns the job.</param>
/// <param name="FieldIds">Field identifiers mounted by the job.</param>
/// <param name="SeasonId">Optional season organiser associated with the job.</param>
/// <param name="WorkOrderId">Optional TaskService work order that created the job.</param>
/// <param name="Notes">Operator supplied notes summarising the job context.</param>
public sealed record JobContext(
    string FarmId,
    IReadOnlyList<string> FieldIds,
    string? SeasonId,
    string? WorkOrderId,
    string? Notes);

/// <summary>
/// Describes the immutable metadata tracked for a job. Properties mirror the <c>aog.job.v1</c>
/// schema while staying lightweight enough for in-memory orchestration.
/// </summary>
/// <param name="JobId">Stable identifier for the job.</param>
/// <param name="Slug">Filesystem friendly slug derived from the display name.</param>
/// <param name="DisplayName">Operator friendly name shown in UI shells.</param>
/// <param name="State">Current lifecycle state.</param>
/// <param name="CreatedAt">UTC timestamp when the job was created.</param>
/// <param name="UpdatedAt">UTC timestamp when metadata last changed.</param>
/// <param name="ActiveSessionId">Identifier of the session currently mounted, if any.</param>
/// <param name="Context">Farm/field context associated with the job.</param>
/// <param name="Tags">Optional user defined tags.</param>
public sealed record JobMetadata(
    string JobId,
    string Slug,
    string DisplayName,
    JobLifecycleState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ActiveSessionId,
    JobContext Context,
    IReadOnlyList<string> Tags);

/// <summary>
/// Parameters supplied when creating a job.
/// </summary>
/// <param name="DisplayName">Operator friendly name of the job.</param>
/// <param name="Context">Farm/field context.</param>
/// <param name="Tags">Optional user supplied tags.</param>
/// <param name="MountImmediately">Whether the job should become the active job immediately.</param>
public sealed record JobCreationRequest(
    string DisplayName,
    JobContext Context,
    IReadOnlyList<string>? Tags = null,
    bool MountImmediately = true);

/// <summary>
/// Lifecycle event emitted to watchers when a job transitions or updates.
/// </summary>
/// <param name="EventType">Event classification.</param>
/// <param name="Job">Snapshot of the job metadata after the transition.</param>
/// <param name="Reason">Optional reason (close reason, pause cause, etc.).</param>
public sealed record JobLifecycleEvent(
    JobLifecycleEventType EventType,
    JobMetadata Job,
    string? Reason = null);
