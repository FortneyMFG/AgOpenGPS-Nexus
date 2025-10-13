namespace Aog.Core.Routing;

/// <summary>
/// Published when a stream's routing changes.
/// </summary>
/// <param name="Stream">Logical stream identifier affected by the change.</param>
/// <param name="Previous">Previous route if one existed.</param>
/// <param name="Current">Current route if one is configured.</param>
public sealed record class StreamRouteChangedEvent(string Stream, StreamRoute? Previous, StreamRoute? Current);
