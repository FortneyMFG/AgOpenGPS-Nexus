using System;
using System.Collections.Generic;
using System.Linq;
using Aog.UI.Avalonia.Hosting;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Aggregates the top-level menus exposed by the shell.
/// </summary>
public sealed class ShellMenuBarViewModel
{
    private readonly Plugins.PluginRegistry _registry;
    private readonly Plugins.PluginHost _host;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellMenuBarViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">Dispatcher used to route commands.</param>
    public ShellMenuBarViewModel(
        IShellCommandDispatcher dispatcher,
        Plugins.PluginRegistry registry,
        Plugins.PluginHost host)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _host = host ?? throw new ArgumentNullException(nameof(host));

        FileMenu = new ShellMenuGroupViewModel(
            ShellPluginSurfaces.FileMenu,
            "File",
            new[]
            {
                ShellMenuItemViewModel.CreateCommand(
                    "Profile…",
                    "menu.file",
                    "core.profile.manage",
                    dispatcher,
                    description: "Open the profile manager to switch between saved machine presets."),
                ShellMenuItemViewModel.CreateCommand(
                    "Simulator On",
                    "menu.file",
                    "core.simulation.toggle",
                    dispatcher,
                    description: "Toggle the embedded simulator routes."),
                ShellMenuItemViewModel.CreateCommand(
                    "About",
                    "menu.file",
                    "core.shell.about",
                    dispatcher,
                    description: "Show build information, licensing, and release notes."),
            });

        FieldMenu = new ShellMenuGroupViewModel(
            ShellPluginSurfaces.FieldMenu,
            "Field",
            new[]
            {
                ShellMenuItemViewModel.CreateCommand(
                    "Boundary tool…",
                    "dialog.boundary",
                    "core.boundary_tool_dialog.open",
                    dispatcher,
                    description: "Launch the boundary editor for the active field."),
                ShellMenuItemViewModel.CreateCommand(
                    "Headland",
                    "menu.field",
                    "core.field.headland",
                    dispatcher,
                    description: "Configure headland passes and tramlines."),
                ShellMenuItemViewModel.CreateCommand(
                    "Delete Applied",
                    "menu.field",
                    "core.field.clearApplied",
                    dispatcher,
                    description: "Clear recorded coverage for the current job."),
            });

        ToolsMenu = new ShellMenuGroupViewModel(
            ShellPluginSurfaces.ToolsMenu,
            "Tools",
            new[]
            {
                ShellMenuItemViewModel.CreateContainer(
                    "Wizards",
                    ShellMenuItemViewModel.CreateCommand(
                        "Steer Wizard",
                        "menu.tools",
                        "core.tools.steerWizard",
                        dispatcher,
                        description: "Launch the steering calibration wizard.")),
                ShellMenuItemViewModel.CreateContainer(
                    "Charts",
                    ShellMenuItemViewModel.CreateCommand(
                        "Steer Chart",
                        "menu.tools",
                        "core.tools.steerChart",
                        dispatcher,
                        description: "Open live steering telemetry charts.")),
                ShellMenuItemViewModel.CreateContainer(
                    "Flags",
                    ShellMenuItemViewModel.CreateCommand(
                        "Flag Manager",
                        "dialog.flags",
                        "core.flag_manager_dialog.open",
                        dispatcher,
                        description: "Review and manage field flags.")),
                ShellMenuItemViewModel.CreateCommand(
                    "Log Viewer",
                    "menu.tools",
                    "core.tools.logViewer",
                    dispatcher,
                    description: "Review recent log entries and event history."),
                ShellMenuItemViewModel.CreateCommand(
                    "System Summary…",
                    "menu.tools",
                    "core.shell.systemSummary",
                    dispatcher,
                    description: "Show the current system summary, layers, and theme selections."),
                ShellMenuItemViewModel.CreateContainer(
                    "Offset",
                    ShellMenuItemViewModel.CreateCommand(
                        "Shift Position",
                        "tools.offset",
                        "core.shift_position_dialog.open",
                        dispatcher,
                    description: "Adjust the current vehicle position offsets.")),
            });

        SettingsMenu = new ShellMenuGroupViewModel(
            ShellPluginSurfaces.SettingsMenu,
            "Settings",
            new[]
            {
                ShellMenuItemViewModel.CreateContainer(
                    "Equipment",
                    ShellMenuItemViewModel.CreateCommand(
                        "Section Settings",
                        "menu.settings",
                        "core.settings.sections",
                        dispatcher,
                        description: "Configure section widths, counts, and control delays."),
                    ShellMenuItemViewModel.CreateCommand(
                        "Autosteer Settings",
                        "menu.settings",
                        "core.settings.autosteer",
                        dispatcher,
                        description: "Adjust autosteer controller and vehicle geometry parameters.")),
                ShellMenuItemViewModel.CreateContainer(
                    "Display",
                    ShellMenuItemViewModel.CreateCommand(
                        "Colors",
                        "menu.settings",
                        "core.settings.display",
                        dispatcher,
                        description: "Customize display color palettes and map styling.")),
                ShellMenuItemViewModel.CreateContainer(
                    "Input",
                    ShellMenuItemViewModel.CreateCommand(
                        "Hotkeys",
                        "menu.settings",
                        "core.settings.hotkeys",
                        dispatcher,
                        description: "Review and edit keyboard shortcuts.")),
            });

        ServicesMenu = new ShellMenuGroupViewModel(
            ShellPluginSurfaces.ServicesMenu,
            "Services",
            new[]
            {
                ShellMenuItemViewModel.CreateCommand(
                    "Start Core Host",
                    "services.backend",
                    "services.core.start",
                    dispatcher,
                    description: "Launch the background core host the UI communicates with."),
                ShellMenuItemViewModel.CreateCommand(
                    "Stop Core Host",
                    "services.backend",
                    "services.core.stop",
                    dispatcher,
                    description: "Terminate the running core host process."),
                ShellMenuItemViewModel.CreateCommand(
                    "Start AgIO (Windows)",
                    "services.backend",
                    "services.agio.windows.start",
                    dispatcher,
                    description: "Start the Windows AgIO bridge.") ,
                ShellMenuItemViewModel.CreateCommand(
                    "Stop AgIO (Windows)",
                    "services.backend",
                    "services.agio.windows.stop",
                    dispatcher,
                    description: "Stop the Windows AgIO bridge."),
                ShellMenuItemViewModel.CreateCommand(
                    "Start AgIO Simulator",
                    "services.backend",
                    "services.agio.sim.start",
                    dispatcher,
                    description: "Launch the AgIO simulator backend."),
                ShellMenuItemViewModel.CreateCommand(
                    "Stop AgIO Simulator",
                    "services.backend",
                    "services.agio.sim.stop",
                    dispatcher,
                    description: "Terminate the AgIO simulator backend."),
            });

        PluginsMenu = new ShellMenuGroupViewModel(
            ShellPluginSurfaces.PluginsMenu,
            "Plugins",
            BuildPluginMenuItems(dispatcher));
    }

    /// <summary>Gets the File menu group.</summary>
    public ShellMenuGroupViewModel FileMenu { get; }

    /// <summary>Gets the Field menu group.</summary>
    public ShellMenuGroupViewModel FieldMenu { get; }

    /// <summary>Gets the Tools menu group.</summary>
    public ShellMenuGroupViewModel ToolsMenu { get; }

    /// <summary>Gets the Settings menu group.</summary>
    public ShellMenuGroupViewModel SettingsMenu { get; }

    /// <summary>Gets the Services menu group.</summary>
    public ShellMenuGroupViewModel ServicesMenu { get; }

    /// <summary>Gets the Plugins menu group.</summary>
    public ShellMenuGroupViewModel PluginsMenu { get; }

    /// <summary>Enumerates all menu groups.</summary>
    public IEnumerable<ShellMenuGroupViewModel> AllMenus
    {
        get
        {
            yield return FileMenu;
            yield return FieldMenu;
            yield return ToolsMenu;
            yield return SettingsMenu;
            yield return ServicesMenu;
            yield return PluginsMenu;
        }
    }

    private IList<ShellMenuItemViewModel> BuildPluginMenuItems(IShellCommandDispatcher dispatcher)
    {
        var items = new List<ShellMenuItemViewModel>();
        foreach (var descriptor in _registry.GetDescriptors()
                     .OrderBy(d => d.Manifest.Name ?? d.Id, StringComparer.OrdinalIgnoreCase))
        {
            var status = _host.GetLoadedPlugins().Any(p => string.Equals(p.Descriptor.Id, descriptor.Id, StringComparison.OrdinalIgnoreCase))
                ? "Loaded"
                : "Installed";
            var source = descriptor.RootPath.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase)
                ? "Bundled"
                : "User";
            var header = FormattableString.Invariant($"{descriptor.Manifest.Name} ({status} - {source})");
            items.Add(ShellMenuItemViewModel.CreateContainer(header));
        }

        if (items.Count > 0)
        {
            items.Add(ShellMenuItemViewModel.CreateContainer("-"));
        }

        items.Add(ShellMenuItemViewModel.CreateCommand(
            "Plugin Manager...",
            "menu.plugins",
            "core.plugins.manager",
            dispatcher,
            description: "Open the plugin manager to view installed plugins, status, and updates."));

        return items;
    }
}



