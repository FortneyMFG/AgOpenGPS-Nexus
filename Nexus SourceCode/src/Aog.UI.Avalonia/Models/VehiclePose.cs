namespace Aog.UI.Avalonia.Models;

/// <summary>
/// Represents the world-space pose of the vehicle displayed on the map.
/// </summary>
/// <param name="X">East/west offset in meters. Positive values move east.</param>
/// <param name="Y">North/south offset in meters. Positive values move north.</param>
/// <param name="HeadingDegrees">Heading in degrees where zero points east and increases counter-clockwise.</param>
public readonly record struct VehiclePose(double X, double Y, double HeadingDegrees)
{
    /// <summary>
    /// Gets a pose anchored at the origin facing east.
    /// </summary>
    public static VehiclePose Origin { get; } = new(0, 0, 0);
}
