using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model surfacing the legacy-inspired map tools menu.
/// </summary>
public sealed class MapToolsDialogViewModel
{
    private readonly ReadOnlyObservableCollection<MapToolOptionViewModel> _tools;

    /// <summary>Initializes a new instance of the <see cref="MapToolsDialogViewModel"/> class.</summary>
    /// <param name="host">The coordinator responsible for launching field operation dialogs.</param>
    /// <param name="closeAction">Action invoked to close the owning dialog.</param>
    public MapToolsDialogViewModel(IFieldOperationsDialogHost host, Action closeAction)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(closeAction);

        var options = new[]
        {
            new MapToolOptionViewModel(
                title: "Boundary Editor",
                description: "Edit field boundaries, drive-throughs, and guidance fences.",
                command: new DelegateCommand(_ =>
                {
                    closeAction();
                    host.OpenBoundaryEditor();
                })),
            new MapToolOptionViewModel(
                title: "Flag Manager",
                description: "Review, add, and remove field flags.",
                command: new DelegateCommand(_ =>
                {
                    closeAction();
                    host.OpenFlagManager();
                })),
            new MapToolOptionViewModel(
                title: "Headland Planner",
                description: "Configure headland passes and smoothing (coming soon).",
                command: new DelegateCommand(_ =>
                {
                    closeAction();
                    host.ShowHeadlandPlannerNotice();
                })),
        };

        _tools = new ReadOnlyObservableCollection<MapToolOptionViewModel>(new ObservableCollection<MapToolOptionViewModel>(options));
    }

    /// <summary>Gets the collection of map tool options rendered in the dialog.</summary>
    public ReadOnlyObservableCollection<MapToolOptionViewModel> Tools => _tools;

    /// <summary>
    /// Creates a design-time sample view-model instance.
    /// </summary>
    public static MapToolsDialogViewModel CreateDesignSample() => new(new DesignHost(), () => { });

    private sealed class DesignHost : IFieldOperationsDialogHost
    {
        public void OpenBoundaryEditor()
        {
        }

        public void OpenFlagManager()
        {
        }

        public void ShowHeadlandPlannerNotice()
        {
        }
    }
}

/// <summary>
/// Represents a single map tool option rendered in the map tools dialog.
/// </summary>
public sealed class MapToolOptionViewModel
{
    /// <summary>Initializes a new instance of the <see cref="MapToolOptionViewModel"/> class.</summary>
    /// <param name="title">Display title for the option.</param>
    /// <param name="description">Descriptive text explaining the option.</param>
    /// <param name="command">Command invoked when the option is selected.</param>
    public MapToolOptionViewModel(string title, string description, ICommand command)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Command = command ?? throw new ArgumentNullException(nameof(command));
    }

    /// <summary>Gets the display title for the map tool option.</summary>
    public string Title { get; }

    /// <summary>Gets the descriptive text for the option.</summary>
    public string Description { get; }

    /// <summary>Gets the command invoked when the option is selected.</summary>
    public ICommand Command { get; }
}
