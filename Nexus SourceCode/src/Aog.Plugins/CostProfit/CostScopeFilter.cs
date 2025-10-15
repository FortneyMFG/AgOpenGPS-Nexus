using System;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Represents an optional filter used when aggregating ledger information.
/// </summary>
public sealed class CostScopeFilter
{
    /// <summary>
    /// Gets or sets the farm identifier filter.
    /// </summary>
    public string? FarmId { get; set; }

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
    /// Normalizes whitespace and empty filters.
    /// </summary>
    public void Normalize()
    {
        FarmId = NormalizeToken(FarmId);
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
