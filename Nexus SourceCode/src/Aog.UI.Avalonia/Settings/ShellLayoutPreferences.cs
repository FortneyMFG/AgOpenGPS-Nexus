namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Stores persisted layout preferences for the Nexus shell.
/// </summary>
public sealed class ShellLayoutPreferences
{
    /// <summary>Gets or sets whether the top toolbar is visible.</summary>
    public bool IsTopToolbarVisible { get; set; } = true;

    /// <summary>Gets or sets whether the right sidebar panels are visible.</summary>
    public bool IsRightSidebarVisible { get; set; } = true;

    /// <summary>Gets or sets the last active workspace identifier.</summary>
    public string ActiveWorkspaceId { get; set; } = "workspace.main";

    /// <summary>Creates a deep copy of the layout preferences.</summary>
    public ShellLayoutPreferences Clone() => new()
    {
        IsTopToolbarVisible = IsTopToolbarVisible,
        IsRightSidebarVisible = IsRightSidebarVisible,
        ActiveWorkspaceId = ActiveWorkspaceId,
    };
}
