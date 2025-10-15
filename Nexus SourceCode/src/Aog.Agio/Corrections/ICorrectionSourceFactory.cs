using System.Threading;
using System.Threading.Tasks;

namespace Aog.Agio.Corrections;

/// <summary>
/// Factory that produces GNSS correction sources.
/// </summary>
public interface ICorrectionSourceFactory
{
    /// <summary>
    /// Gets a diagnostic name describing the factory.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the kind of correction source created by this factory.
    /// </summary>
    CorrectionSourceKind Kind { get; }

    /// <summary>
    /// Attempts to create a correction source instance.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the creation attempt.</param>
    /// <returns>The created source or <c>null</c> if the source is unavailable.</returns>
    ValueTask<ICorrectionSource?> TryCreateAsync(CancellationToken cancellationToken);
}
