using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Nexus.Sdk.UI;

namespace Aog.UI.Avalonia.Plugins;

internal sealed class UiHostServices : IUiHostServices, IAssetLocator, IBlockRegistry, IWindowRegistry, IToolRegistry, ILayerRegistry
{
    public UiHostServices(PluginDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    public PluginDescriptor Descriptor { get; }

    public IBlockRegistry BlockRegistry => this;

    public IWindowRegistry WindowRegistry => this;

    public IToolRegistry ToolRegistry => this;

    public ILayerRegistry LayerRegistry => this;

    public IAssetLocator Assets => this;

    public Uri? TryResolveAssetUri(string assetKey)
    {
        if (string.IsNullOrWhiteSpace(assetKey))
        {
            return null;
        }

        var fullPath = Path.Combine(Descriptor.AssetsDirectory, assetKey);
        return File.Exists(fullPath) ? new Uri(fullPath) : null;
    }

    public void Register(IBlockProvider provider)
    {
        // Registration no-op for now. Future work will integrate with block layout.
    }

    public void Unregister(IBlockProvider provider)
    {
    }

    public void Register(IWindowProvider provider)
    {
    }

    public void Unregister(IWindowProvider provider)
    {
    }

    public void Register(IToolProvider provider)
    {
    }

    public void Unregister(IToolProvider provider)
    {
    }

    public void Register(ILayerProvider provider)
    {
    }

    public void Unregister(ILayerProvider provider)
    {
    }
}

