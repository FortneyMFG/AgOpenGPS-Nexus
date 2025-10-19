using System.Threading;
using System.Threading.Tasks;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Provides strongly typed access to the host-managed configuration namespace for the plugin.
/// </summary>
public interface IConfig
{
    /// <summary>
    /// Retrieves the configuration value stored at the specified path.
    /// </summary>
    ValueTask<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the supplied value to the specified configuration path.
    /// </summary>
    ValueTask SetAsync<T>(string path, T? value, CancellationToken cancellationToken = default);
}
