using System;
using System.Collections.Generic;
using System.IO;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Plugins;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.ViewModels.Shell;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Hosting;

public sealed class PluginSurfaceDescriptorTests
{
    private sealed class FakePreferencesService : IUiPreferencesService
    {
        private readonly UiPreferences _preferences;

        public FakePreferencesService(UiPreferences preferences)
        {
            _preferences = preferences;
        }

        public UiPreferences GetPreferences() => _preferences.Clone();

        public void UpdateTheme(UiTheme theme) { }

        public void UpdateWindowPlacement(WindowPlacement placement) { }

        public void UpdateTelemetryOptIn(bool isOptedIn) { }

        public void UpdateRunMode(AvaloniaRunMode mode) { }

        public void UpdateShellLayout(ShellLayoutPreferences layout)
        {
            _preferences.ShellLayout = layout.Clone();
        }
    }
    private readonly IReadOnlyDictionary<string, (string Contract, string InjectionPoint)> _surfaceMetadata;

    public PluginSurfaceDescriptorTests()
    {
        _surfaceMetadata = LoadSurfaceMetadata();
    }

    [Fact]
    public void TopToolbarDescriptorMatchesManifest()
    {
        AssertSurfaceMatches(ShellPluginSurfaces.TopToolbar);
        var viewModel = CreateTopToolbar();
        Assert.Equal(ShellPluginSurfaces.TopToolbar, viewModel.Descriptor);
    }

    [Fact]
    public void FileMenuDescriptorMatchesManifest()
    {
        AssertSurfaceMatches(ShellPluginSurfaces.FileMenu);
        using var dispatcher = new NoOpShellCommandDispatcher();
        var registry = new PluginRegistry();
        var host = new PluginHost(new NullServiceProvider(), NullLogger<PluginHost>.Instance);
        var menuBar = new ShellMenuBarViewModel(dispatcher, registry, host);
        Assert.Equal(ShellPluginSurfaces.FileMenu, menuBar.FileMenu.Descriptor);
    }

    [Fact]
    public void FieldMenuDescriptorMatchesManifest()
    {
        AssertSurfaceMatches(ShellPluginSurfaces.FieldMenu);
        using var dispatcher = new NoOpShellCommandDispatcher();
        var registry = new PluginRegistry();
        var host = new PluginHost(new NullServiceProvider(), NullLogger<PluginHost>.Instance);
        var menuBar = new ShellMenuBarViewModel(dispatcher, registry, host);
        Assert.Equal(ShellPluginSurfaces.FieldMenu, menuBar.FieldMenu.Descriptor);
    }

    [Fact]
    public void ToolsMenuDescriptorMatchesManifest()
    {
        AssertSurfaceMatches(ShellPluginSurfaces.ToolsMenu);
        using var dispatcher = new NoOpShellCommandDispatcher();
        var registry = new PluginRegistry();
        var host = new PluginHost(new NullServiceProvider(), NullLogger<PluginHost>.Instance);
        var menuBar = new ShellMenuBarViewModel(dispatcher, registry, host);
        Assert.Equal(ShellPluginSurfaces.ToolsMenu, menuBar.ToolsMenu.Descriptor);
    }

    [Fact]
    public void AppShellDescriptorMatchesManifest()
    {
        AssertSurfaceMatches(ShellPluginSurfaces.AppShell);
        using var dispatcher = new NoOpShellCommandDispatcher();
        var catalog = new BlockCatalog(new IBlockProvider[] { new CoreBlockProvider() });
        var preferencesService = new FakePreferencesService(new UiPreferences());
        var layout = CreateBlockLayout(dispatcher);
        var store = new BlockLayoutStore(preferencesService, catalog);
        var blockLayoutVM = new BlockLayoutViewModel(store, catalog, dispatcher, preferencesService);
        var viewModel = new AppShellViewModel(blockLayoutVM);
        Assert.Equal(ShellPluginSurfaces.AppShell, viewModel.SurfaceDescriptor);
    }

    private void AssertSurfaceMatches(PluginSurfaceDescriptor descriptor)
    {
        Assert.True(_surfaceMetadata.TryGetValue(descriptor.SurfaceId, out var metadata));
        Assert.Equal(metadata.Contract, descriptor.Contract);
        Assert.Equal(metadata.InjectionPoint, descriptor.InjectionPoint);
    }

    private static TopToolbarViewModel CreateTopToolbar()
    {
        var dispatcher = new NoOpShellCommandDispatcher();
        return new TopToolbarViewModel(dispatcher);
    }

    private static ShellMenuBarViewModel CreateShellMenuBar(IShellCommandDispatcher dispatcher)
    {
        var registry = new PluginRegistry();
        var host = new PluginHost(new NullServiceProvider(), NullLogger<PluginHost>.Instance);
        return new ShellMenuBarViewModel(dispatcher, registry, host);
    }

    private static BlockLayoutViewModel CreateBlockLayout(IShellCommandDispatcher dispatcher)
    {
        var preferences = new StubUiPreferencesService();
        var catalog = new BlockCatalog(Array.Empty<IBlockProvider>());
        var store = new StubBlockLayoutStore();
        return new BlockLayoutViewModel(store, catalog, dispatcher, preferences);
    }

    private static IReadOnlyDictionary<string, (string Contract, string InjectionPoint)> LoadSurfaceMetadata()
    {
        var root = LocateRepositoryRoot();
        var file = Path.Combine(root, "artifacts", "ui-to-plugin.yaml");
        var metadata = new Dictionary<string, (string Contract, string InjectionPoint)>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(file);
        string? line;
        string? currentId = null;
        string? currentContract = null;
        string? currentInjectionPoint = null;

        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("- id:", StringComparison.Ordinal))
            {
                Commit();
                currentId = trimmed.Split(':', 2)[1].Trim();
                currentContract = null;
                currentInjectionPoint = null;
            }
            else if (currentId is not null && trimmed.StartsWith("contract:", StringComparison.Ordinal))
            {
                currentContract = trimmed.Split(':', 2)[1].Trim();
            }
            else if (currentId is not null && trimmed.StartsWith("inject_point:", StringComparison.Ordinal))
            {
                currentInjectionPoint = trimmed.Split(':', 2)[1].Trim();
            }
        }

        Commit();
        return metadata;

        void Commit()
        {
            if (currentId is null)
            {
                return;
            }

            if (currentContract is null || currentInjectionPoint is null)
            {
                throw new InvalidOperationException($"Incomplete metadata for surface '{currentId}'.");
            }

            metadata[currentId] = (currentContract, currentInjectionPoint);
            currentId = null;
            currentContract = null;
            currentInjectionPoint = null;
        }
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.Exists)
        {
            var candidate = Path.Combine(directory.FullName, "artifacts", "ui-to-plugin.yaml");
            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root containing artifacts/ui-to-plugin.yaml.");
    }

    private sealed class NoOpShellCommandDispatcher : IShellCommandDispatcher, IDisposable
    {
        public ValueTask<bool> DispatchAsync(string injectionPoint, string commandId, System.Threading.CancellationToken cancellationToken = default)
        {
            return new ValueTask<bool>(true);
        }

        public void Dispose()
        {
        }
    }

    private sealed class StubBlockLayoutStore : IBlockLayoutStore
    {
        public IReadOnlyList<BlockInstance> Load() => Array.Empty<BlockInstance>();

        public void Save(IEnumerable<BlockInstance> instances)
        {
        }
    }

    private sealed class StubUiPreferencesService : IUiPreferencesService
    {
        private UiPreferences _preferences = new();

        public UiPreferences GetPreferences() => _preferences;

        public void UpdateTheme(UiTheme theme)
        {
            _preferences.Theme = theme;
        }

        public void UpdateWindowPlacement(WindowPlacement placement)
        {
            _preferences.Window = placement ?? new WindowPlacement();
        }

        public void UpdateTelemetryOptIn(bool isOptedIn)
        {
            _preferences.TelemetryOptIn = isOptedIn;
        }

        public void UpdateRunMode(AvaloniaRunMode mode)
        {
            _preferences.RunMode = mode;
        }

        public void UpdateShellLayout(ShellLayoutPreferences layout)
        {
            _preferences.ShellLayout = layout ?? new ShellLayoutPreferences();
        }
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
