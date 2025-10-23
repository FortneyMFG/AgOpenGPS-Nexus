using System;
using System.Collections.Generic;
using Nexus.Sdk.Core;

namespace Aog.UI.Avalonia.Plugins;

/// <summary>
/// Represents a loaded plugin and the resources tied to its assembly load context.
/// </summary>
public sealed class PluginHandle : IDisposable
{
    public PluginHandle(PluginDescriptor descriptor, PluginLoadContext loadContext, IReadOnlyList<IPluginEntrypoint> entrypoints)
    {
        Descriptor = descriptor;
        LoadContext = loadContext;
        Entrypoints = entrypoints;
    }

    public PluginDescriptor Descriptor { get; }

    public PluginLoadContext LoadContext { get; }

    public IReadOnlyList<IPluginEntrypoint> Entrypoints { get; }

    public void Dispose()
    {
        foreach (var entrypoint in Entrypoints)
        {
            (entrypoint as IDisposable)?.Dispose();
        }

        LoadContext.Unload();
    }
}

