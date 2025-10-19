using System.Collections.Generic;
using Aog.UI.Avalonia.Mapping.Core;

namespace Aog.UI.Avalonia.Mapping.Vector;

/// <summary>
/// Placeholder for 2D simplification helpers (Ramer-Douglas-Peucker). Currently returns source geometry unchanged.
/// </summary>
public static class Simplify2D
{
    public static IReadOnlyList<Double3> Rdp(IReadOnlyList<Double3> vertices, double toleranceMeters)
        => vertices;
}
