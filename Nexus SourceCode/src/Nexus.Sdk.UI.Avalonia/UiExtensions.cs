using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace Nexus.Sdk.UI;

/// <summary>Provides UI-specific host services.</summary>
public interface IUiHostServices
{
    IBlockRegistry BlockRegistry { get; }
    IWindowRegistry WindowRegistry { get; }
    IToolRegistry ToolRegistry { get; }
    ILayerRegistry LayerRegistry { get; }
    IAssetLocator Assets { get; }
}

/// <summary>Locates plugin assets embedded in zips or assemblies.</summary>
public interface IAssetLocator
{
    Uri? TryResolveAssetUri(string assetKey);
}

/// <summary>Registry used to publish shell blocks.</summary>
public interface IBlockRegistry
{
    void Register(IBlockProvider provider);
    void Unregister(IBlockProvider provider);
}

/// <summary>Registry used to publish additional windows or dialogs.</summary>
public interface IWindowRegistry
{
    void Register(IWindowProvider provider);
    void Unregister(IWindowProvider provider);
}

/// <summary>Registry used to publish interactive tools.</summary>
public interface IToolRegistry
{
    void Register(IToolProvider provider);
    void Unregister(IToolProvider provider);
}

/// <summary>Registry used to publish map layers.</summary>
public interface ILayerRegistry
{
    void Register(ILayerProvider provider);
    void Unregister(ILayerProvider provider);
}

/// <summary>Provides block descriptors to the shell.</summary>
public interface IBlockProvider
{
    IEnumerable<BlockDescriptor> GetBlocks(IServiceProvider serviceProvider);
}

/// <summary>Provides window descriptors to the shell.</summary>
public interface IWindowProvider
{
    IReadOnlyList<WindowDescriptor> GetWindows(IServiceProvider serviceProvider);
}

/// <summary>Provides tool descriptors to the shell.</summary>
public interface IToolProvider
{
    IEnumerable<ToolDescriptor> GetTools(IServiceProvider serviceProvider);
}

/// <summary>Provides layer descriptors to the map host.</summary>
public interface ILayerProvider
{
    IEnumerable<LayerDescriptor> GetLayers(IServiceProvider serviceProvider);
}

/// <summary>Describes a UI block contributed by a plugin.</summary>
/// <param name="Id">Stable identifier for persistence.</param>
/// <param name="DisplayName">Label rendered in the shell.</param>
/// <param name="Placement">Logical placement hint.</param>
/// <param name="Factory">Factory callback that builds the block content.</param>
public sealed record BlockDescriptor(
    string Id,
    string DisplayName,
    BlockPlacement Placement,
    Func<IServiceProvider, Control> Factory);

/// <summary>Describes a window or dialog contributed by a plugin.</summary>
/// <param name="Id">Stable identifier.</param>
/// <param name="DisplayName">Presentation label.</param>
/// <param name="IsSingleton">Indicates whether the same instance should be reused.</param>
/// <param name="Factory">Factory used to construct the window when invoked.</param>
public sealed record WindowDescriptor(
    string Id,
    string DisplayName,
    bool IsSingleton,
    Func<IServiceProvider, Window> Factory);

/// <summary>Describes an interactive tool.</summary>
/// <param name="Id">Stable identifier.</param>
/// <param name="DisplayName">Presentation label.</param>
/// <param name="Activation">Callback invoked when the tool is activated.</param>
public sealed record ToolDescriptor(
    string Id,
    string DisplayName,
    Func<IServiceProvider, IToolInstance> Activation);

/// <summary>Describes a map layer factory.</summary>
/// <param name="Id">Stable identifier.</param>
/// <param name="DisplayName">Presentation label.</param>
/// <param name="Factory">Factory invoked when the layer is requested.</param>
public sealed record LayerDescriptor(
    string Id,
    string DisplayName,
    Func<IServiceProvider, ILayerInstance> Factory);

/// <summary>
/// Runtime instance returned when a tool is activated.
/// </summary>
public interface IToolInstance : IDisposable
{
    void OnDeactivated();
}

/// <summary>
/// Runtime instance returned when a layer is realised.
/// </summary>
public interface ILayerInstance : IDisposable
{
    void Invalidate();
}

/// <summary>Hints the shell uses when placing blocks.</summary>
public enum BlockPlacement
{
    Unknown = 0,
    Left,
    Right,
    Bottom,
    Top,
    Floating
}

