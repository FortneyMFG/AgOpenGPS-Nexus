using System;

namespace Aog.Core.Routing;

/// <summary>
/// Represents the selected source for a logical stream.
/// </summary>
public sealed record class StreamRoute
{
    public StreamRoute(string stream, string source, RouteSourceMode mode)
    {
        if (string.IsNullOrWhiteSpace(stream))
        {
            throw new ArgumentException("Stream identifier is required.", nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source identifier is required.", nameof(source));
        }

        Stream = stream;
        Source = source;
        Mode = mode;
    }

    /// <summary>
    /// Logical stream identifier (e.g., pose, imu, sections).
    /// </summary>
    public string Stream { get; init; }

    /// <summary>
    /// Identifier for the provider currently selected for the stream.
    /// </summary>
    public string Source { get; init; }

    /// <summary>
    /// Preferred routing mode for the stream.
    /// </summary>
    public RouteSourceMode Mode { get; init; }

    public override string ToString() => $"{Stream} -> {Source} ({Mode})";
}
