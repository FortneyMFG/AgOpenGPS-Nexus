using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Describes a UI surface and the plugin contract metadata that powers it.
/// </summary>
public sealed record PluginSurfaceDescriptor
{
    public PluginSurfaceDescriptor(string surfaceId, string contract, string injectionPoint)
    {
        SurfaceId = surfaceId ?? throw new ArgumentNullException(nameof(surfaceId));
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        InjectionPoint = injectionPoint ?? throw new ArgumentNullException(nameof(injectionPoint));
    }

    /// <summary>Gets the inventory identifier for the surface.</summary>
    public string SurfaceId { get; }

    /// <summary>Gets the plugin contract declared for the surface.</summary>
    public string Contract { get; }

    /// <summary>Gets the injection point used when dispatching commands to the surface.</summary>
    public string InjectionPoint { get; }
}

/// <summary>
/// Provides strongly typed access to the shell-related surfaces defined in <c>artifacts/ui-to-plugin.yaml</c>.
/// </summary>
public static class ShellPluginSurfaces
{
    public static PluginSurfaceDescriptor AppShell { get; } = new("app_shell", "core.app_shell.v1", "shell.main");

    public static PluginSurfaceDescriptor MapCanvas { get; } = new("map_canvas", "core.map_canvas.v1", "shell.map");

    public static PluginSurfaceDescriptor TopToolbar { get; } = new("top_toolbar", "core.top_toolbar.v1", "toolbar.top");

    public static PluginSurfaceDescriptor FileMenu { get; } = new("file_menu", "core.file_menu.v1", "menu.file");

    public static PluginSurfaceDescriptor FieldMenu { get; } = new("field_menu", "core.field_menu.v1", "menu.field");

    public static PluginSurfaceDescriptor ToolsMenu { get; } = new("tools_menu", "core.tools_menu.v1", "menu.tools");

    public static PluginSurfaceDescriptor SettingsMenu { get; } = new("settings_menu", "core.settings_menu.v1", "menu.settings");

    public static PluginSurfaceDescriptor ServicesMenu { get; } = new("services_menu", "core.services_menu.v1", "services.backend");
}
