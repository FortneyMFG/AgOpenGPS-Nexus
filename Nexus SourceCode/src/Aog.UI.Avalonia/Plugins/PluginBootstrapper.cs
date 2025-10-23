using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.Sdk.Core;
using Nexus.Sdk.UI;

namespace Aog.UI.Avalonia.Plugins;

internal sealed class PluginBootstrapper : IAsyncDisposable
{
    private readonly PluginRegistry _registry;
    private readonly PluginPathProvider _pathProvider;
    private readonly PluginHost _host;
    private readonly IServiceProvider _services;
    private readonly ILogger<PluginBootstrapper> _logger;
    private readonly Version _sdkVersion = SdkVersion.Current;

    public PluginBootstrapper(
        PluginRegistry registry,
        PluginPathProvider pathProvider,
        PluginHost host,
        IServiceProvider services,
        ILogger<PluginBootstrapper> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        foreach (var path in _pathProvider.Paths)
        {
            await _registry.ScanAsync(path, cancellationToken).ConfigureAwait(false);
        }

        foreach (var descriptor in _registry.GetDescriptors())
        {
            try
            {
                var handle = await _host.LoadAsync(descriptor.RootPath, cancellationToken).ConfigureAwait(false);
                var environment = new PluginEnvironmentInfo(descriptor, _sdkVersion);
                var paths = new PluginPathsInfo(descriptor.RootPath);
                var uiServices = new UiHostServices(descriptor);
                var features = new Dictionary<Type, object>
                {
                    [typeof(IUiHostServices)] = uiServices
                };

                var loggerFactory = _services.GetRequiredService<ILoggerFactory>();
                var hostServices = new PluginHostServices(descriptor, _services, loggerFactory, paths, environment, features);
                await _host.InitializeAsync(handle, hostServices, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin {PluginId} from {PluginPath}.", descriptor.Id, descriptor.RootPath);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        return _host.UnloadAllAsync();
    }
}
