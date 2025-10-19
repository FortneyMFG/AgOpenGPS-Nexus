using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexus.Sdk.Core;

namespace Aog.UI.Avalonia.Plugins;

/// <summary>
/// Coordinates discovery, activation, and teardown of plugin load contexts.
/// </summary>
public sealed class PluginHost
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PluginHost> _logger;
    private readonly Dictionary<string, PluginHandle> _handles = new(StringComparer.OrdinalIgnoreCase);

    public PluginHost(IServiceProvider serviceProvider, ILogger<PluginHost> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads the plugin assemblies located under <paramref name="pluginRoot"/>.
    /// </summary>
    public async ValueTask<PluginHandle> LoadAsync(string pluginRoot, CancellationToken cancellationToken = default)
    {
        var descriptor = await PluginDescriptor.LoadAsync(pluginRoot, cancellationToken).ConfigureAwait(false);
        if (_handles.ContainsKey(descriptor.Id))
        {
            throw new InvalidOperationException($"Plugin '{descriptor.Id}' is already loaded.");
        }

        var loadContext = new PluginLoadContext(descriptor);
        var entrypoints = PluginEntrypointFactory.CreateEntrypoints(descriptor, loadContext, _serviceProvider);
        var handle = new PluginHandle(descriptor, loadContext, entrypoints);
        _handles[descriptor.Id] = handle;
        _logger.LogInformation("Loaded plugin {PluginId} ({Version}) with {EntrypointCount} entry point(s).",
            descriptor.Id, descriptor.Version, entrypoints.Count);
        return handle;
    }

    /// <summary>
    /// Initializes every entry point for the specified plugin.
    /// </summary>
    public ValueTask InitializeAsync(PluginHandle handle, IHostServices services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(services);

        foreach (var entrypoint in handle.Entrypoints)
        {
            entrypoint.Initialize(services);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Shuts down the plugin and unloads its assemblies.
    /// </summary>
    public async ValueTask UnloadAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_handles.TryGetValue(pluginId, out var handle))
        {
            return;
        }

        _handles.Remove(pluginId);

        foreach (var entrypoint in handle.Entrypoints)
        {
            try
            {
                await entrypoint.ShutdownAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Plugin entry point shutdown failed for {PluginId}.", pluginId);
            }
        }

        handle.Dispose();
        _logger.LogInformation("Unloaded plugin {PluginId}.", pluginId);
    }

    /// <summary>
    /// Gets a snapshot of loaded plugins.
    /// </summary>
    public IReadOnlyCollection<PluginHandle> GetLoadedPlugins() => _handles.Values.ToArray();

    public async ValueTask UnloadAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var pluginId in _handles.Keys.ToArray())
        {
            await UnloadAsync(pluginId, cancellationToken).ConfigureAwait(false);
        }
    }
}
