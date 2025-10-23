

namespace Aog.Plugins.Mapping;

internal static class PlanarPointExtensions
{
    public static Aog.Core.Coverage.PlanarPoint ToCoverage(this Aog.Core.Paths.PlanarPoint point)
        => new(point.Easting, point.Northing);

    public static Aog.Core.Paths.PlanarPoint ToPaths(this Aog.Core.Coverage.PlanarPoint point)
        => new(point.Easting, point.Northing);
}