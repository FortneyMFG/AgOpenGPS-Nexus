namespace Nexus.Sdk.Core;

/// <summary>
/// Provides telemetry primitives for plugins to emit structured data.
/// </summary>
public interface ITelemetry
{
    /// <summary>
    /// Tracks a metric with an optional set of dimensions.
    /// </summary>
    /// <param name="name">Metric name.</param>
    /// <param name="value">Metric value.</param>
    /// <param name="properties">Optional dimension set.</param>
    void TrackMetric(string name, double value, IReadOnlyDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks a discrete event.
    /// </summary>
    /// <param name="name">Event name.</param>
    /// <param name="properties">Optional dimension set.</param>
    void TrackEvent(string name, IReadOnlyDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks an exception.
    /// </summary>
    /// <param name="exception">Captured exception.</param>
    /// <param name="properties">Optional dimension set.</param>
    void TrackException(Exception exception, IReadOnlyDictionary<string, string>? properties = null);
}
