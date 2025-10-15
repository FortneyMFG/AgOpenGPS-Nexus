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
}
