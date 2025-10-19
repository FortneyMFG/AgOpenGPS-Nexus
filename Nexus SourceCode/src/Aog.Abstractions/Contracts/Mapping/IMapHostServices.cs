using System.Collections.Generic;
using System.Threading;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Provides host services that mapping plugins can rely on.
/// </summary>
public interface IMapHostServices
{
    /// <summary>
    /// Streams pose samples for the active vehicle in field coordinates.
    /// </summary>
    IAsyncEnumerable<PoseSample> PoseStream(CancellationToken cancellationToken);

    /// <summary>
    /// Observes changes to the active field context.
    /// </summary>
    IObservable<FieldContext> FieldContext { get; }

    /// <summary>
    /// Gets the plugin-scoped storage provider.
    /// </summary>
    IStorage Storage { get; }

    /// <summary>
    /// Gets the plugin configuration accessor.
    /// </summary>
    IConfig Config { get; }

    /// <summary>
    /// Gets the authoritative layer registry published by Core.
    /// </summary>
    ILayerRegistry LayerRegistry { get; }

    /// <summary>
    /// Gets the command surface for layer mutations.
    /// </summary>
    ILayerCommands LayerCommands { get; }

    /// <summary>
    /// Gets the CRS conversion service.
    /// </summary>
    ICrsService CrsService { get; }

    /// <summary>
    /// Gets the catalog of tile sources produced by Core.
    /// </summary>
    ITileCatalog TileCatalog { get; }

    /// <summary>
    /// Gets the store for user style profile overrides.
    /// </summary>
    IStyleProfileStore StyleProfiles { get; }
}
