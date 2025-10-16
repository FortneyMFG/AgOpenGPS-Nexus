using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Aog.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Cli.Host.Runtime;
using Nexus.Plugin.Cli.Abstractions;
using Spectre.Console;

namespace Nexus.Cli.Host.Plugins;

/// <summary>
/// Discovers plugin manifests and loads CLI adapter assemblies that implement <see cref="ICommandModule"/>.
/// </summary>
public sealed class PluginCommandModuleLoader : IPluginCommandModuleLoader
{
    private static readonly string[] ManifestFileNames = ["plugin.json", "manifest.json"];

    private readonly INexusEnvironment _environment;
    private readonly IServiceProvider _hostServices;
    private readonly PluginManifestLoader _manifestLoader;
    private readonly IAnsiConsole _console;
    private readonly ConcurrentDictionary<string, bool> _loggedWarnings = new(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyList<ICommandModule>? _cachedModules;

    public PluginCommandModuleLoader(
        INexusEnvironment environment,
        IServiceProvider hostServices,
        PluginManifestLoader manifestLoader,
        IAnsiConsole console)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _hostServices = hostServices ?? throw new ArgumentNullException(nameof(hostServices));
        _manifestLoader = manifestLoader ?? throw new ArgumentNullException(nameof(manifestLoader));
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ICommandModule>> LoadModulesAsync(CancellationToken cancellationToken)
    {
        if (_cachedModules is { } cached)
        {
            return cached;
        }

        var modules = new List<ICommandModule>();
        foreach (var manifestPath in DiscoverManifestPaths())
        {
            cancellationToken.ThrowIfCancellationRequested();

            PluginManifest manifest;
            try
            {
                manifest = await _manifestLoader.LoadAsync(manifestPath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReportWarning($"Failed to read plugin manifest '{manifestPath}': {ex.Message}");
                continue;
            }

            var pluginDirectory = Path.GetDirectoryName(manifestPath)!;
            foreach (var assemblyPath in DiscoverCliAssemblies(pluginDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var descriptor = new PluginCommandModuleDescriptor(
                    manifest.Id,
                    manifest.Name,
                    manifest.Version,
                    pluginDirectory,
                    manifestPath,
                    assemblyPath);

                foreach (var module in LoadModulesFromAssembly(assemblyPath, descriptor, manifest))
                {
                    modules.Add(module);
                }
            }
        }

        _cachedModules = modules;
        return modules;
    }

    private IEnumerable<string> DiscoverManifestPaths()
    {
        var pluginRoot = _environment.PluginsDirectory;
        if (!Directory.Exists(pluginRoot))
        {
            yield break;
        }

        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pluginDirectory in SafeEnumerateDirectories(pluginRoot))
        {
            if (TryGetManifestPath(pluginDirectory, out var directManifest) && unique.Add(directManifest))
            {
                yield return directManifest;
            }

            var currentPath = Path.Combine(pluginDirectory, "current");
            if (TryGetManifestPath(currentPath, out var currentManifest) && unique.Add(currentManifest))
            {
                yield return currentManifest;
            }

            foreach (var versionDirectory in SafeEnumerateDirectories(pluginDirectory))
            {
                if (TryGetManifestPath(versionDirectory, out var manifestPath) && unique.Add(manifestPath))
                {
                    yield return manifestPath;
                }
            }
        }
    }

    private static bool TryGetManifestPath(string? directory, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        foreach (var fileName in ManifestFileNames)
        {
            var candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> DiscoverCliAssemblies(string directory)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(directory, "*.Cli.dll", SearchOption.AllDirectories))
        {
            if (unique.Add(path))
            {
                yield return path;
            }
        }
    }

    private IEnumerable<ICommandModule> LoadModulesFromAssembly(
        string assemblyPath,
        PluginCommandModuleDescriptor descriptor,
        PluginManifest manifest)
    {
        PluginAssemblyLoadContext? context = null;
        Assembly assembly;

        try
        {
            context = new PluginAssemblyLoadContext(assemblyPath);
            assembly = context.LoadFromAssemblyPath(assemblyPath);
        }
        catch (Exception ex)
        {
            context?.Dispose();
            ReportWarning($"Failed to load CLI adapter '{assemblyPath}': {ex.Message}");
            yield break;
        }

        var moduleTypes = GetModuleTypes(assembly);
        if (moduleTypes.Count == 0)
        {
            ReportWarning($"No ICommandModule implementations were found in '{assemblyPath}'.");
            context.Dispose();
            yield break;
        }

        foreach (var type in moduleTypes)
        {
            IServiceProvider? pluginProvider = null;
            ICommandModule? instance = null;

            try
            {
                pluginProvider = CreatePluginServiceProvider(descriptor, manifest);
                instance = (ICommandModule)ActivatorUtilities.CreateInstance(
                    new CompositeServiceProvider(_hostServices, pluginProvider),
                    type);
            }
            catch (Exception ex)
            {
                if (pluginProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                ReportWarning($"Failed to instantiate '{type.FullName}' from '{assemblyPath}': {ex.Message}");
                continue;
            }

            if (instance is not null)
            {
                yield return new PluginCommandModuleAdapter(instance, context, pluginProvider);
            }
        }
    }

    private static List<Type> GetModuleTypes(Assembly assembly)
    {
        try
        {
            return assembly
                .GetTypes()
                .Where(type => type is { IsAbstract: false, IsClass: true } && typeof(ICommandModule).IsAssignableFrom(type))
                .ToList();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types
                .Where(type => type is { IsAbstract: false, IsClass: true } && typeof(ICommandModule).IsAssignableFrom(type))
                .Cast<Type>()
                .ToList();
        }
    }

    private static IServiceProvider CreatePluginServiceProvider(PluginCommandModuleDescriptor descriptor, PluginManifest manifest)
    {
        var services = new ServiceCollection();
        services.AddSingleton(descriptor);
        services.AddSingleton(manifest);
        return services.BuildServiceProvider();
    }

    private void ReportWarning(string message)
    {
        if (_loggedWarnings.TryAdd(message, true))
        {
            _console.MarkupLineInterpolated($"[yellow]{Markup.Escape(message)}[/]");
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        if (!Directory.Exists(path))
        {
            yield break;
        }

        IEnumerator<string>? enumerator = null;
        try
        {
            enumerator = Directory.EnumerateDirectories(path).GetEnumerator();
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        while (true)
        {
            string? current;
            try
            {
                if (enumerator is null || !enumerator.MoveNext())
                {
                    yield break;
                }

                current = enumerator.Current;
            }
            catch (IOException)
            {
                yield break;
            }
            catch (UnauthorizedAccessException)
            {
                yield break;
            }

            if (current is not null)
            {
                yield return current;
            }
        }
    }

    private sealed class PluginCommandModuleAdapter : ICommandModule
    {
        private readonly ICommandModule _module;
        private readonly PluginAssemblyLoadContext _context;
        private readonly IServiceProvider _pluginProvider;

        public PluginCommandModuleAdapter(ICommandModule module, PluginAssemblyLoadContext context, IServiceProvider pluginProvider)
        {
            _module = module;
            _context = context;
            _pluginProvider = pluginProvider;
        }

        public void Configure(CommandModuleContext context)
        {
            var services = new CompositeServiceProvider(context.Services, _pluginProvider);
            var pluginContext = new CommandModuleContext(context.RootCommand, services, context.OutputOption);
            _module.Configure(pluginContext);
        }
    }

    private sealed class PluginAssemblyLoadContext : AssemblyLoadContext, IDisposable
    {
        private readonly AssemblyDependencyResolver _resolver;
        private bool _disposed;

        public PluginAssemblyLoadContext(string assemblyPath)
            : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(assemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == typeof(ICommandModule).Assembly.GetName().Name)
            {
                return Assembly.Load(assemblyName);
            }

            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            if (path is null)
            {
                return null;
            }

            return LoadFromAssemblyPath(path);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Unload();
        }
    }

    private sealed class CompositeServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _primary;
        private readonly IServiceProvider _fallback;

        public CompositeServiceProvider(IServiceProvider fallback, IServiceProvider primary)
        {
            _fallback = fallback;
            _primary = primary;
        }

        public object? GetService(Type serviceType)
        {
            return _primary.GetService(serviceType) ?? _fallback.GetService(serviceType);
        }
    }
}
