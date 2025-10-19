using System.Threading;
using System.Threading.Tasks;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Provides coordinate reference system conversions for plugins.
/// </summary>
public interface ICrsService
{
    /// <summary>
    /// Converts a coordinate from the provided CRS into WGS84.
    /// </summary>
    ValueTask<GeoCoordinate> ToWgs84Async(GeoCoordinate coordinate, string sourceCrs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a coordinate from WGS84 into the provided CRS.
    /// </summary>
    ValueTask<GeoCoordinate> FromWgs84Async(GeoCoordinate coordinate, string targetCrs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects a WGS84 coordinate into ENU coordinates relative to an origin.
    /// </summary>
    ValueTask<EnuCoordinate> ToEnuAsync(GeoCoordinate coordinate, GeoCoordinate origin, CancellationToken cancellationToken = default);
}
