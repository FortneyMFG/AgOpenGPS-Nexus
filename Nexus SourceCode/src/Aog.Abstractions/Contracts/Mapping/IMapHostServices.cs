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
}
