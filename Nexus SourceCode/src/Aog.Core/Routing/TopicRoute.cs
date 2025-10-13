namespace Aog.Core.Routing;

/// <summary>
/// Describes the currently active source for a routed topic.
/// </summary>
/// <param name="Topic">The topic identifier.</param>
/// <param name="SourceId">The identifier for the selected producer.</param>
/// <param name="Kind">The kind of producer that is currently active.</param>
public sealed record TopicRoute(string Topic, string SourceId, SourceKind Kind);
