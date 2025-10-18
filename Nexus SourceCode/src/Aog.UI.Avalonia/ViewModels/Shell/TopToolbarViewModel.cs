using System;
using System.Collections.ObjectModel;
using Aog.UI.Avalonia.Hosting;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model for the top command toolbar.
/// </summary>
public sealed class TopToolbarViewModel
{
    private readonly ReadOnlyObservableCollection<TopToolbarItemViewModel> _items;

    /// <summary>Gets the plugin surface descriptor describing the toolbar.</summary>
    public PluginSurfaceDescriptor Descriptor => ShellPluginSurfaces.TopToolbar;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopToolbarViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">Command dispatcher used by toolbar buttons.</param>
    public TopToolbarViewModel(IShellCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        var collection = new ObservableCollection<TopToolbarItemViewModel>(new[]
        {
            new TopToolbarItemViewModel(
                id: "toolbar.autosteer",
                displayName: "Auto Steer",
                injectionPoint: "toolbar.top",
                commandId: "core.toolbar.autoSteer",
                dispatcher: dispatcher,
                isToggle: true,
                initialState: true,
                description: "Engage or disengage autosteer."),
            new TopToolbarItemViewModel(
                id: "toolbar.sections",
                displayName: "Sections",
                injectionPoint: "toolbar.top",
                commandId: "core.toolbar.sections",
                dispatcher: dispatcher,
                isToggle: true,
                initialState: true,
                description: "Toggle master section control."),
            new TopToolbarItemViewModel(
                id: "toolbar.youturn",
                displayName: "YouTurn",
                injectionPoint: "toolbar.top",
                commandId: "core.toolbar.youTurn",
                dispatcher: dispatcher,
                isToggle: true,
                initialState: false,
                description: "Arm automated headland turns."),
            new TopToolbarItemViewModel(
                id: "toolbar.autotrack",
                displayName: "AutoTrack",
                injectionPoint: "toolbar.top",
                commandId: "core.toolbar.autoTrack",
                dispatcher: dispatcher,
                isToggle: true,
                initialState: true,
                description: "Maintain alignment with the active guidance track."),
            new TopToolbarItemViewModel(
                id: "toolbar.steerSettings",
                displayName: "Steer Config",
                injectionPoint: "toolbar.top",
                commandId: "core.toolbar.steerConfig",
                dispatcher: dispatcher,
                isToggle: false,
                initialState: false,
                description: "Open steering configuration."),
            new TopToolbarItemViewModel(
                id: "toolbar.sectionConfig",
                displayName: "Section Config",
                injectionPoint: "toolbar.top",
                commandId: "core.toolbar.sectionConfig",
                dispatcher: dispatcher,
                isToggle: false,
                initialState: false,
                description: "Open section control configuration."),
            new TopToolbarItemViewModel(
                id: "toolbar.offsetFix",
                displayName: "Offset Fix",
                injectionPoint: "tools.offset",
                commandId: "core.shift_position_dialog.open",
                dispatcher: dispatcher,
                isToggle: false,
                initialState: false,
                description: "Open the shift position dialog."),
        });

        _items = new ReadOnlyObservableCollection<TopToolbarItemViewModel>(collection);
    }

    /// <summary>Gets the toolbar items rendered in the UI.</summary>
    public ReadOnlyObservableCollection<TopToolbarItemViewModel> Items => _items;
}
