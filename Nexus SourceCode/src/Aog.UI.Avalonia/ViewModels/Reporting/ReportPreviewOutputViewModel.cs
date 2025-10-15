using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels.Reporting;

/// <summary>
/// Represents the state of a report output format for preview purposes.
/// </summary>
public sealed class ReportPreviewOutputViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReportPreviewOutputViewModel"/> class.
    /// </summary>
    /// <param name="format">Output format identifier.</param>
    /// <param name="isRequested">Indicates whether the output was requested.</param>
    /// <param name="artifactCount">Number of artifacts produced for the format.</param>
    /// <param name="artifactNames">Names of the produced artifacts.</param>
    /// <param name="declaredCount">Optional count recorded in metadata.</param>
    public ReportPreviewOutputViewModel(
        string format,
        bool isRequested,
        int artifactCount,
        IEnumerable<string> artifactNames,
        int? declaredCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        Format = format;
        IsRequested = isRequested;
        ArtifactCount = artifactCount;
        DeclaredCount = declaredCount;
        ArtifactNames = artifactNames?.Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
    }

    /// <summary>Gets the output format identifier.</summary>
    public string Format { get; }

    /// <summary>Gets a value indicating whether the format was requested.</summary>
    public bool IsRequested { get; }

    /// <summary>Gets the number of artifacts produced for the format.</summary>
    public int ArtifactCount { get; }

    /// <summary>Gets the number of artifacts declared via metadata, when available.</summary>
    public int? DeclaredCount { get; }

    /// <summary>Gets the names of the produced artifacts.</summary>
    public IReadOnlyList<string> ArtifactNames { get; }

    /// <summary>Gets a value indicating whether the requested format produced no artifacts.</summary>
    public bool IsMissing => IsRequested && ArtifactCount == 0;

    /// <summary>
    /// Gets a value indicating whether fewer artifacts were produced than declared in metadata.
    /// </summary>
    public bool IsPartial => DeclaredCount is { } expected && expected > ArtifactCount;
}
