namespace Nexus.Sdk.Core;

/// <summary>
/// Provides a durable key-value store scoped to a plugin.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Reads a value from the store.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Key to read.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a value to the store.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Key to write.</param>
    /// <param name="value">Value to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the store if it exists.
    /// </summary>
    /// <param name="key">Key to remove.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}
