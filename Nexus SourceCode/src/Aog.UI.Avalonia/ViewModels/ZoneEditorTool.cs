namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Editing affordances supported by the shared zone editor toolbar.
/// </summary>
public enum ZoneEditorTool
{
    /// <summary>Draw a free-form polygon by placing vertices.</summary>
    Polygon,

    /// <summary>Create axis-aligned rectangles sized to management areas.</summary>
    Rectangle,

    /// <summary>Brush adjustments onto an existing geometry.</summary>
    Brush,

    /// <summary>Remove geometry or carve gaps within a zone.</summary>
    Eraser,
}
