namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Coordinates launching field operation dialogs such as boundary and flag managers.
/// </summary>
public interface IFieldOperationsDialogHost
{
    /// <summary>Opens the boundary editor dialog.</summary>
    void OpenBoundaryEditor();

    /// <summary>Opens the flag manager dialog.</summary>
    void OpenFlagManager();

    /// <summary>Displays the headland planner notice dialog.</summary>
    void ShowHeadlandPlannerNotice();
}
