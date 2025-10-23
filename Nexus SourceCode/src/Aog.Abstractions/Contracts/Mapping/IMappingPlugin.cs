using Avalonia.Controls;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Contract implemented by mapping plugins to supply the shell-integrated map surface.
/// </summary>
public interface IMappingPlugin
{
    /// <summary>
    /// Creates the plugin-provided map view.
    /// </summary>
    /// <param name="hostServices">Host services supplied by the shell.</param>
    /// <returns>An Avalonia control ready to embed in the shell.</returns>
    Control CreateMapView(IMapHostServices hostServices);
}
