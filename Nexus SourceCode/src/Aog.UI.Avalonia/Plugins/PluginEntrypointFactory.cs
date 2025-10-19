using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nexus.Sdk.Core;

namespace Aog.UI.Avalonia.Plugins;

/// <summary>
/// Discovers plugin entry points declared in managed assemblies.
/// </summary>
internal static class PluginEntrypointFactory
{
    public static IReadOnlyList<IPluginEntrypoint> CreateEntrypoints(
        PluginDescriptor descriptor,
        PluginLoadContext loadContext,
        IServiceProvider serviceProvider)
    {
        var assemblies = EnumerateAssemblies(loadContext, descriptor);
        var entrypoints = new List<IPluginEntrypoint>();
        foreach (var assembly in assemblies)
        {
            foreach (var type in EnumerateTypes(assembly))
            {
                if (!typeof(IPluginEntrypoint).IsAssignableFrom(type) || type.IsAbstract)
                {
                    continue;
                }

                if (Activator.CreateInstance(type) is IPluginEntrypoint entrypoint)
                {
                    entrypoints.Add(entrypoint);
                }
            }
        }

        return entrypoints;
    }

    private static IEnumerable<Assembly> EnumerateAssemblies(PluginLoadContext context, PluginDescriptor descriptor)
    {
        var assemblies = new List<Assembly>();
        if (Directory.Exists(descriptor.LibDirectory))
        {
            foreach (var path in Directory.EnumerateFiles(descriptor.LibDirectory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                assemblies.Add(context.LoadFromAssemblyPath(path));
            }
        }

        return assemblies;
    }

    private static IEnumerable<Type> EnumerateTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
