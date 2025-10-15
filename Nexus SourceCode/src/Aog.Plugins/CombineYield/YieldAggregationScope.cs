using System;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Identifies the agronomic scope represented by a yield aggregation entry.
/// </summary>
public sealed class YieldAggregationScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YieldAggregationScope"/> class.
    /// </summary>
    /// <param name="farmId">Identifier of the farm that owns the aggregation.</param>
    /// <param name="seasonId">Optional season identifier.</param>
    /// <param name="fieldId">Optional field identifier.</param>
    /// <param name="jobId">Optional job identifier.</param>
    /// <param name="sessionId">Optional session identifier.</param>
    public YieldAggregationScope(
        string farmId,
        string? seasonId = null,
        string? fieldId = null,
        string? jobId = null,
        string? sessionId = null)
    {
        if (string.IsNullOrWhiteSpace(farmId))
        {
            throw new ArgumentException("Farm identifier is required.", nameof(farmId));
        }

        FarmId = farmId.Trim();
        SeasonId = Normalize(seasonId);
        FieldId = Normalize(fieldId);
        JobId = Normalize(jobId);
        SessionId = Normalize(sessionId);
    }

    /// <summary>
    /// Gets the farm identifier.
    /// </summary>
    public string FarmId { get; }

    /// <summary>
    /// Gets the optional season identifier.
    /// </summary>
    public string? SeasonId { get; }

    /// <summary>
    /// Gets the optional field identifier.
    /// </summary>
    public string? FieldId { get; }

    /// <summary>
    /// Gets the optional job identifier.
    /// </summary>
    public string? JobId { get; }

    /// <summary>
    /// Gets the optional session identifier.
    /// </summary>
    public string? SessionId { get; }

    /// <summary>
    /// Determines whether this scope matches a filter.
    /// </summary>
    /// <param name="filter">Filter to evaluate.</param>
    public bool Matches(YieldScopeFilter filter)
    {
        if (filter is null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        if (filter.FarmId is not null && !string.Equals(filter.FarmId, FarmId, StringComparison.Ordinal))
        {
            return false;
        }

        if (filter.SeasonId is not null && !string.Equals(filter.SeasonId, SeasonId, StringComparison.Ordinal))
        {
            return false;
        }

        if (filter.FieldId is not null && !string.Equals(filter.FieldId, FieldId, StringComparison.Ordinal))
        {
            return false;
        }

        if (filter.JobId is not null && !string.Equals(filter.JobId, JobId, StringComparison.Ordinal))
        {
            return false;
        }

        if (filter.SessionId is not null && !string.Equals(filter.SessionId, SessionId, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
