using System;
using Avalonia;

namespace Aog.UI.Avalonia.Models;

/// <summary>
/// Represents a square coverage sample rendered on the map overlay.
/// </summary>
/// <param name="Center">The center of the cell in world coordinates (meters).</param>
/// <param name="SizeMeters">Length of one side of the square cell in meters.</param>
/// <param name="CoverageFraction">Fraction of coverage completed within the cell (0–1).</param>
public sealed record CoverageCell(Point Center, double SizeMeters, double CoverageFraction)
{
    /// <summary>Gets a clamped coverage fraction in the inclusive range [0, 1].</summary>
    public double ClampedCoverage => Math.Clamp(CoverageFraction, 0, 1);
}
