using System;
using System.Collections.Generic;

namespace Aog.Plugins.JobTasks;

/// <summary>
/// Enumerates the lifecycle states tracked for a job session.
/// </summary>
public enum JobSessionState
{
    /// <summary>
    /// Session is currently active and can ingest telemetry.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Session is paused; operators may resume it later without
    /// creating a new session.
    /// </summary>
    Paused = 1,

    /// <summary>
    /// Session completed and became immutable for audit purposes.
    /// </summary>
    Completed = 2,
}

/// <summary>
/// Event types emitted by the session orchestrator.
/// </summary>
public enum JobSessionEventType
{
    /// <summary>
    /// Session started.
    /// </summary>
    Started,

    /// <summary>
    /// Session paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Session resumed.
    /// </summary>
    Resumed,

    /// <summary>
    /// Session metadata changed.
    /// </summary>
    MetadataChanged,

    /// <summary>
    /// Session completed.
    /// </summary>
    Completed,
}

/// <summary>
/// Immutable snapshot describing a job session.
/// </summary>
/// <param name="SessionId">Stable identifier assigned to the session.</param>
/// <param name="Name">Operator friendly name of the session.</param>
/// <param name="State">Lifecycle state of the session.</param>
/// <param name="StartedAt">UTC timestamp when the session started.</param>
/// <param name="LastModifiedAt">UTC timestamp when the session last changed.</param>
/// <param name="EndedAt">UTC timestamp when the session completed, if applicable.</param>
/// <param name="WorkOrderId">Optional TaskService work order identifier.</param>
/// <param name="ActiveOperators">Operators currently associated with the session.</param>
/// <param name="Notes">Optional operator supplied notes.</param>
public sealed record JobSession(
    string SessionId,
    string Name,
    JobSessionState State,
    DateTimeOffset StartedAt,
    DateTimeOffset LastModifiedAt,
    DateTimeOffset? EndedAt,
    string? WorkOrderId,
    IReadOnlyList<string> ActiveOperators,
    string? Notes);

/// <summary>
/// Parameters supplied when starting a session.
/// </summary>
/// <param name="Name">Optional name for the new session.</param>
/// <param name="ActiveOperators">Operators participating in the session.</param>
/// <param name="WorkOrderId">Optional work order identifier.</param>
/// <param name="Notes">Optional notes captured at the start of the session.</param>
public sealed record JobSessionStartRequest(
    string? Name = null,
    IReadOnlyList<string>? ActiveOperators = null,
    string? WorkOrderId = null,
    string? Notes = null);

/// <summary>
/// Represents the editable metadata tracked for a session.
/// </summary>
/// <param name="Name">Session display name.</param>
/// <param name="ActiveOperators">Operators currently assigned.</param>
/// <param name="WorkOrderId">Optional work order identifier.</param>
/// <param name="Notes">Optional freeform notes.</param>
public sealed record JobSessionMetadata(
    string Name,
    IReadOnlyList<string> ActiveOperators,
    string? WorkOrderId,
    string? Notes);

/// <summary>
/// Event raised by the orchestrator whenever a session transitions.
/// </summary>
/// <param name="EventType">Type of lifecycle change.</param>
/// <param name="JobId">Identifier of the job associated with the session.</param>
/// <param name="SeasonId">Season identifier resolved for the job, when available.</param>
/// <param name="Session">Snapshot of the session after the transition.</param>
/// <param name="Reason">Optional reason supplied for the transition.</param>
public sealed record JobSessionEvent(
    JobSessionEventType EventType,
    string JobId,
    string? SeasonId,
    JobSession Session,
    string? Reason = null);
