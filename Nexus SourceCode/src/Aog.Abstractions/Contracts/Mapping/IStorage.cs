using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Provides access to plugin-scoped persistent storage.
/// </summary>
public interface IStorage
{
    /// <summary>
    /// Gets the root directory reserved for the plugin.
    /// </summary>
    string RootPath { get; }

    /// <summary>
    /// Opens a stream for reading the specified resource if it exists.
    /// </summary>
    ValueTask<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a stream for writing the specified resource, replacing existing data.
    /// </summary>
    ValueTask<Stream> OpenWriteAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified resource if present.
    /// </summary>
    ValueTask DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the maximum number of bytes the plugin may persist, when enforced.
    /// </summary>
    long? QuotaBytes { get; }
}
