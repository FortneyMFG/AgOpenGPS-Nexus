using System;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Counts of observations grouped by severity classification.
/// </summary>
public sealed record FieldHealthSeverityCounts(int None, int Low, int Moderate, int High, int Critical)
{
    /// <summary>
    /// Adds a severity to the counts and returns an updated instance.
    /// </summary>
    public FieldHealthSeverityCounts Add(FieldHealthSeverity severity) => severity switch
    {
        FieldHealthSeverity.None => this with { None = None + 1 },
        FieldHealthSeverity.Low => this with { Low = Low + 1 },
        FieldHealthSeverity.Moderate => this with { Moderate = Moderate + 1 },
        FieldHealthSeverity.High => this with { High = High + 1 },
        FieldHealthSeverity.Critical => this with { Critical = Critical + 1 },
        _ => this
    };

    /// <summary>
    /// Adds counts from another instance and returns a combined snapshot.
    /// </summary>
    public FieldHealthSeverityCounts Add(FieldHealthSeverityCounts other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return new FieldHealthSeverityCounts(
            None + other.None,
            Low + other.Low,
            Moderate + other.Moderate,
            High + other.High,
            Critical + other.Critical);
    }
}
