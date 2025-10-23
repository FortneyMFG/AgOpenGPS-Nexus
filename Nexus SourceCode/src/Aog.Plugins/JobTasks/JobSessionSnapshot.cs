using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Aog.Plugins.JobTasks;

/// <summary>
/// Describes the lifecycle of a job session as stored in <c>job.json</c>.
/// </summary>
/// <param name="SessionId">Identifier of the session.</param>
/// <param name="State">Lifecycle state of the session.</param>
/// <param name="StartedAt">UTC timestamp when the session began.</param>
/// <param name="LastModifiedAt">UTC timestamp when the session last changed.</param>
/// <param name="Name">Optional display name for the session.</param>
/// <param name="EndedAt">Optional UTC timestamp when the session ended.</param>
/// <param name="ActiveOperators">Operators that participated in the session.</param>
/// <param name="Stats">Summary statistics captured for the session.</param>
/// <param name="Extensions">Plugin-defined metadata scoped to the session.</param>
public sealed record class JobSessionSnapshot(
    string SessionId,
    JobSessionState State,
    DateTimeOffset StartedAt,
    DateTimeOffset LastModifiedAt,
    string? Name = null,
    DateTimeOffset? EndedAt = null,
    IReadOnlyList<string>? ActiveOperators = null,
    JobSessionStatisticsSnapshot? Stats = null,
    IReadOnlyDictionary<string, JsonElement>? Extensions = null);

/// <summary>
/// Aggregated telemetry describing a session.
/// </summary>
/// <param name="AreaHectares">Area covered during the session.</param>
/// <param name="DistanceKilometers">Distance travelled during the session.</param>
/// <param name="DurationSeconds">Duration of the session in seconds.</param>
/// <param name="CoveragePercent">Coverage percentage reported for the session.</param>
public sealed record class JobSessionStatisticsSnapshot(
    double? AreaHectares = null,
    double? DistanceKilometers = null,
    double? DurationSeconds = null,
    double? CoveragePercent = null);
