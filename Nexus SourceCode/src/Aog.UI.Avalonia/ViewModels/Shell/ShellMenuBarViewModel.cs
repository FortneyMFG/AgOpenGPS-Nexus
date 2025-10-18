using System;
using System.Collections.Generic;
using Aog.UI.Avalonia.Hosting;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Aggregates the top-level menus exposed by the shell.
/// </summary>
public sealed class ShellMenuBarViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShellMenuBarViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">Dispatcher used to route commands.</param>
    public ShellMenuBarViewModel(IShellCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

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
                ShellMenuItemViewModel.CreateContainer(
                    "Offset",
                    ShellMenuItemViewModel.CreateCommand(
                        "Shift Position",
                        "tools.offset",
                        "core.shift_position_dialog.open",
                        dispatcher,
                        description: "Adjust the current vehicle position offsets.")),
            });
    }

    /// <summary>Gets the File menu group.</summary>
    public ShellMenuGroupViewModel FileMenu { get; }

    /// <summary>Gets the Field menu group.</summary>
    public ShellMenuGroupViewModel FieldMenu { get; }

    /// <summary>Gets the Tools menu group.</summary>
    public ShellMenuGroupViewModel ToolsMenu { get; }

    /// <summary>Enumerates all menu groups.</summary>
    public IEnumerable<ShellMenuGroupViewModel> AllMenus
    {
        get
        {
            yield return FileMenu;
            yield return FieldMenu;
            yield return ToolsMenu;
        }
    }
}
