using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Core.Jobs;

/// <summary>
/// Represents the lifecycle state of a job.
/// </summary>
public enum JobLifecycleState
{
    Inactive,
    Active,
    Completed
}

/// <summary>
/// Represents the lifecycle state of a job session.
/// </summary>
public enum JobSessionState
{
    Planned,
    Active,
    Paused,
    Completed
}

/// <summary>
/// Describes a persisted job session instance.
/// </summary>
public sealed record JobSessionMetadata(
    string Id,
    string Name,
    JobSessionState State,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt)
{
    /// <summary>
    /// Gets a value indicating whether the session has reached a terminal state.
    /// </summary>
    public bool IsTerminal => State == JobSessionState.Completed;
}

/// <summary>
/// Summarises the persisted information for a job.
/// </summary>
public sealed record JobHandle(
    string Id,
    string DisplayName,
    string DirectoryName,
    JobLifecycleState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<JobSessionMetadata> Sessions)
{
    /// <summary>
    /// Gets the active or paused session when one exists.
    /// </summary>
    public JobSessionMetadata? ActiveSession => Sessions.LastOrDefault(session =>
        session.State is JobSessionState.Active or JobSessionState.Paused);
}

/// <summary>
/// Lifecycle change categories used by the orchestrator.
/// </summary>
public enum JobLifecycleChangeType
{
    Activated,
    Deactivated,
    Updated
}

/// <summary>
/// Event payload emitted when the active job changes.
/// </summary>
public sealed class JobLifecycleEventArgs : EventArgs
{
    public JobLifecycleEventArgs(JobHandle job, JobLifecycleChangeType changeType)
    {
        Job = job ?? throw new ArgumentNullException(nameof(job));
        ChangeType = changeType;
    }

    public JobHandle Job { get; }

    public JobLifecycleChangeType ChangeType { get; }
}

/// <summary>
/// Parameters used to create a new job.
/// </summary>
public sealed record JobCreationRequest(string DisplayName, string? InitialSessionName = null);
