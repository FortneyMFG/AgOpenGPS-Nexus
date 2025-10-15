using System;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Aggregated statistics derived from field health observations.
/// </summary>
public sealed record FieldHealthLayerStatistics(double TotalAreaHa, FieldHealthSeverityCounts SeverityCounts, DateTimeOffset? LastSurveyedAt);
