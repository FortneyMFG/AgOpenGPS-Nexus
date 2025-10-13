namespace Aog.Core.Routing;

/// <summary>
/// Event published when the active source for a topic changes.
/// </summary>
/// <param name="Topic">The affected topic.</param>
/// <param name="Previous">The previously active route, if any.</param>
/// <param name="Current">The newly selected route, if any.</param>
public sealed record TopicRouteChanged(string Topic, TopicRoute? Previous, TopicRoute? Current);
