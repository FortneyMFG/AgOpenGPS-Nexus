namespace Aog.Plugins.Mapping.Raster;

/// <summary>
/// Identifies a raster tile using XYZ addressing.
/// </summary>
public readonly record struct TileId(int Zoom, int X, int Y)
{
    public override string ToString() => $"{Zoom}/{X}/{Y}";
}
