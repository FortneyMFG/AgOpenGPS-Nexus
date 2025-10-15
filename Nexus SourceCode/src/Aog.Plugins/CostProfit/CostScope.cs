using System;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Identifies the agronomic scope that a cost record applies to.
/// </summary>
public sealed class CostScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CostScope"/> class.
    /// </summary>
    /// <param name="farmId">Identifier for the farm owning the record.</param>
    /// <param name="fieldId">Optional field identifier.</param>
    /// <param name="jobId">Optional job identifier.</param>
    /// <param name="sessionId">Optional session identifier.</param>
    public CostScope(string farmId, string? fieldId = null, string? jobId = null, string? sessionId = null)
    {
        if (string.IsNullOrWhiteSpace(farmId))
        {
            throw new ArgumentException("Farm identifier is required.", nameof(farmId));
        }

        FarmId = farmId.Trim();
        FieldId = string.IsNullOrWhiteSpace(fieldId) ? null : fieldId.Trim();
        JobId = string.IsNullOrWhiteSpace(jobId) ? null : jobId.Trim();
        SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId.Trim();
    }

    /// <summary>
    /// Gets the farm identifier.
    /// </summary>
    public string FarmId { get; }

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
    public bool Matches(CostScopeFilter filter)
    {
        if (filter is null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        if (filter.FarmId is not null && !string.Equals(filter.FarmId, FarmId, StringComparison.Ordinal))
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
}
