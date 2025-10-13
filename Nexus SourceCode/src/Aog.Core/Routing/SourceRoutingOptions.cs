using System.Collections.Generic;

namespace Aog.Core.Routing;

/// <summary>
/// Configures per-stream source routing preferences.
/// </summary>
/// <remarks>
/// Bind this type from configuration (e.g. <c>appsettings.json</c>) to declare the
/// desired provider for each logical stream. The options can then be applied to
/// <see cref="SourceRoutingMap"/> to update runtime routing decisions.
/// </remarks>
public sealed class SourceRoutingOptions
{
    /// <summary>
    /// Collection of per-stream routes.
    /// </summary>
    public IList<StreamRouteOptions> Routes { get; } = new List<StreamRouteOptions>();

    /// <summary>
    /// Converts the configured routes to immutable <see cref="StreamRoute"/> instances.
    /// </summary>
    public IEnumerable<StreamRoute> ToRoutes()
    {
        foreach (var route in Routes)
        {
            if (route is null)
            {
                continue;
            }

            yield return route.ToStreamRoute();
        }
    }
}

/// <summary>
/// Options representation of a stream route suitable for configuration binding.
/// </summary>
public sealed class StreamRouteOptions
{
    /// <summary>
    /// Logical stream identifier.
    /// </summary>
    public string? Stream { get; set; }

    /// <summary>
    /// Selected provider identifier for the stream.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Preferred routing mode.
    /// </summary>
    public RouteSourceMode Mode { get; set; } = RouteSourceMode.Simulation;

    internal StreamRoute ToStreamRoute()
    {
        return new StreamRoute(Stream ?? string.Empty, Source ?? string.Empty, Mode);
    }
}
