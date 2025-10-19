using System.Collections.Generic;
using Aog.Plugins.Mapping.Core;

namespace Aog.Plugins.Mapping.Vector;

/// <summary>
/// Placeholder for 2D simplification helpers (Ramer-Douglas-Peucker). Currently returns source geometry unchanged.
/// </summary>
public static class Simplify2D
{
    public static IReadOnlyList<Double3> Rdp(IReadOnlyList<Double3> vertices, double toleranceMeters)
        => vertices;
}
