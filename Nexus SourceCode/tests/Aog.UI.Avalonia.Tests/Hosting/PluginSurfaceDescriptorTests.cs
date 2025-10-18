using System;
using System.Collections.Generic;
using System.IO;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.ViewModels.Shell;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Hosting;

public sealed class PluginSurfaceDescriptorTests
{
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
        var menuBar = new ShellMenuBarViewModel(dispatcher);
        Assert.Equal(ShellPluginSurfaces.FileMenu, menuBar.FileMenu.Descriptor);
    }

    [Fact]
    public void FieldMenuDescriptorMatchesManifest()
    {
        AssertSurfaceMatches(ShellPluginSurfaces.FieldMenu);
        using var dispatcher = new NoOpShellCommandDispatcher();
        var menuBar = new ShellMenuBarViewModel(dispatcher);
        Assert.Equal(ShellPluginSurfaces.FieldMenu, menuBar.FieldMenu.Descriptor);
    }

    [Fact]
    public void ToolsMenuDescriptorMatchesManifest()
    {
        AssertSurfaceMatches(ShellPluginSurfaces.ToolsMenu);
        using var dispatcher = new NoOpShellCommandDispatcher();
        var menuBar = new ShellMenuBarViewModel(dispatcher);
        Assert.Equal(ShellPluginSurfaces.ToolsMenu, menuBar.ToolsMenu.Descriptor);
    }

    [Fact]
    public void AppShellDescriptorMatchesManifest()
    {
        AssertSurfaceMatches(ShellPluginSurfaces.AppShell);
        var viewModel = new AppShellViewModel();
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
}
