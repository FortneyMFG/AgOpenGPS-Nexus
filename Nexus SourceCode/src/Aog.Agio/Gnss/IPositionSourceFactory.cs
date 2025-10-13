using System.Threading;
using System.Threading.Tasks;

namespace Aog.Agio.Gnss;

/// <summary>
/// Factory that produces concrete GNSS position sources.
/// </summary>
public interface IPositionSourceFactory
{
    /// <summary>
    /// Gets a diagnostic name describing the factory.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the kind of position source created by this factory.
    /// </summary>
    PositionSourceKind Kind { get; }

    /// <summary>
    /// Attempts to create a position source instance.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the creation attempt.</param>
    /// <returns>The created source or <c>null</c> if the source is unavailable.</returns>
    ValueTask<IPositionSource?> TryCreateAsync(CancellationToken cancellationToken);
}
