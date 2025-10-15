using System;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Represents an optional filter applied when querying yield analytics.
/// </summary>
public sealed class YieldScopeFilter
{
    /// <summary>
    /// Gets or sets the farm identifier filter.
    /// </summary>
    public string? FarmId { get; set; }

    /// <summary>
    /// Gets or sets the season identifier filter.
    /// </summary>
    public string? SeasonId { get; set; }

    /// <summary>
    /// Gets or sets the field identifier filter.
    /// </summary>
    public string? FieldId { get; set; }

    /// <summary>
    /// Gets or sets the job identifier filter.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Gets or sets the session identifier filter.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Normalizes the filter by trimming whitespace and converting empty values to <c>null</c>.
    /// </summary>
    public void Normalize()
    {
        FarmId = NormalizeToken(FarmId);
        SeasonId = NormalizeToken(SeasonId);
        FieldId = NormalizeToken(FieldId);
        JobId = NormalizeToken(JobId);
        SessionId = NormalizeToken(SessionId);
    }

    private static string? NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
