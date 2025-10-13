using System;
using System.Collections.Generic;

namespace Aog.Core.Routing;

/// <summary>
/// Configuration that influences the default routing decisions for topics.
/// </summary>
public sealed class RoutingOptions
{
    /// <summary>
    /// Gets a map of topic identifiers to preferred source identifiers.
    /// When the specified source is registered it will be selected regardless
    /// of automatic priority ordering.
    /// </summary>
    public IDictionary<string, string> PreferredSources { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
