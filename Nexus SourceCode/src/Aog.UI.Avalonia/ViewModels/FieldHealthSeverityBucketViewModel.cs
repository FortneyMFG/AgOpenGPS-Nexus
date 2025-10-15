using System;
using System.Globalization;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a severity bucket within the field health panel.
/// </summary>
public sealed class FieldHealthSeverityBucketViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldHealthSeverityBucketViewModel"/> class.
    /// </summary>
    /// <param name="severity">Severity label.</param>
    /// <param name="count">Number of observations in the bucket.</param>
    /// <param name="areaHectares">Area covered by the bucket.</param>
    /// <param name="description">Optional description.</param>
    public FieldHealthSeverityBucketViewModel(string severity, int count, double areaHectares, string description)
    {
        if (string.IsNullOrWhiteSpace(severity))
        {
            throw new ArgumentException("Severity label is required.", nameof(severity));
        }

        Severity = severity.Trim();
        Count = Math.Max(0, count);
        AreaHectares = Math.Max(0, areaHectares);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SeverityBrush = ResolveSeverityBrush(Severity);
    }

    /// <summary>Gets the severity label.</summary>
    public string Severity { get; }

    /// <summary>Gets the observation count contained in the bucket.</summary>
    public int Count { get; }

    /// <summary>Gets the area covered by the bucket.</summary>
    public double AreaHectares { get; }

    /// <summary>Gets an optional description for the bucket.</summary>
    public string? Description { get; }

    /// <summary>Gets the brush used when rendering the severity pill.</summary>
    public IBrush SeverityBrush { get; }

    /// <summary>Gets the formatted area display.</summary>
    public string AreaDisplay => string.Format(CultureInfo.InvariantCulture, "{0:0.0} ha", AreaHectares);

    /// <summary>Gets the formatted count display.</summary>
    public string CountDisplay => string.Format(CultureInfo.InvariantCulture, "{0}", Count);

    /// <summary>Gets a value indicating whether a description is present.</summary>
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    private static IBrush ResolveSeverityBrush(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "none" => BrushCache.Create(Color.FromArgb(255, 128, 139, 150)),
            "low" => BrushCache.Create(Color.FromArgb(255, 52, 152, 219)),
            "moderate" => BrushCache.Create(Color.FromArgb(255, 241, 196, 15)),
            "high" => BrushCache.Create(Color.FromArgb(255, 230, 126, 34)),
            "critical" => BrushCache.Create(Color.FromArgb(255, 231, 76, 60)),
            _ => BrushCache.Create(Color.FromArgb(255, 149, 165, 166)),
        };
    }
}
